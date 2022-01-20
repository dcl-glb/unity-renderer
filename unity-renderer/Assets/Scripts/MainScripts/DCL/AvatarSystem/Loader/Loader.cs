using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using DCL;
using DCL.Helpers;
using UnityEngine;

namespace AvatarSystem
{
    public class Loader : ILoader
    {
        public GameObject bodyshapeContainer => bodyshapeLoader?.rendereable?.container;
        public SkinnedMeshRenderer combinedRenderer { get; private set; }
        public Renderer[] facialFeaturesRenderers { get; private set; }
        public ILoader.Status status { get; private set; } = ILoader.Status.Idle;

        private readonly IWearableLoaderFactory wearableLoaderFactory;
        private readonly GameObject container;

        internal IBodyshapeLoader bodyshapeLoader;
        internal readonly Dictionary<string, IWearableLoader> loaders = new Dictionary<string, IWearableLoader>();
        private readonly IAvatarMeshCombinerHelper avatarMeshCombiner;

        public Loader(IWearableLoaderFactory wearableLoaderFactory, GameObject container, IAvatarMeshCombinerHelper avatarMeshCombiner)
        {
            this.wearableLoaderFactory = wearableLoaderFactory;
            this.container = container;

            this.avatarMeshCombiner = avatarMeshCombiner;
            avatarMeshCombiner.prepareMeshForGpuSkinning = true;
            avatarMeshCombiner.uploadMeshToGpu = true;
        }

        public async UniTask Load(WearableItem bodyshape, WearableItem eyes, WearableItem eyebrows, WearableItem mouth, List<WearableItem> wearables, AvatarSettings settings, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            List<IWearableLoader> toCleanUp = new List<IWearableLoader>();

            void DisposeCleanUpLoaders()
            {
                for (int i = 0; i < toCleanUp.Count; i++)
                {
                    if (toCleanUp[i] == null)
                        continue;
                    toCleanUp[i].Dispose();
                }
            }

            try
            {
                status = ILoader.Status.Loading;

                if (bodyshapeLoader == null || bodyshapeLoader.wearable.id != bodyshape.id || bodyshapeLoader.eyes.id != eyes.id || bodyshapeLoader.eyebrows.id != eyebrows.id || bodyshapeLoader.mouth.id != mouth.id)
                {
                    toCleanUp.Add(bodyshapeLoader);
                    bodyshapeLoader = wearableLoaderFactory.GetBodyshapeLoader(bodyshape, eyes, eyebrows, mouth);
                }

                await bodyshapeLoader.Load(container, settings, ct);

                if (bodyshapeLoader.status == IWearableLoader.Status.Failed)
                {
                    status = ILoader.Status.Failed_Mayor;
                    throw new Exception($"Couldnt load bodyshape");
                }

                (List<IWearableLoader> notReusableLoaders, List<IWearableLoader> newLoaders) = GetNewLoaders(wearables, loaders, wearableLoaderFactory);
                toCleanUp.AddRange(notReusableLoaders);
                loaders.Clear();
                for (int i = 0; i < newLoaders.Count; i++)
                {
                    IWearableLoader loader = newLoaders[i];
                    loaders.Add(loader.wearable.data.category, loader);
                }

                await UniTask.WhenAll(loaders.Values.Select(x => x.Load(container, settings, ct)));

                // Update Status accordingly
                status = ComposeStatus(loaders);
                if (status == ILoader.Status.Failed_Mayor)
                {
                    List<string> failedWearables = loaders.Values
                                                          .Where(x => x.status == IWearableLoader.Status.Failed && AvatarSystemUtils.IsCategoryRequired(x.wearable.data.category))
                                                          .Select(x => x.wearable.id)
                                                          .ToList();

                    throw new Exception($"Couldnt load (nor fallback) wearables with required category: {string.Join(", ", failedWearables)}");
                }
                //TODO move this to the DI
                BonesRetargeter retargeter = new BonesRetargeter();
                retargeter.Retarget(loaders.Values.SelectMany(x => x.rendereable.renderers).OfType<SkinnedMeshRenderer>(), bodyshapeLoader.upperBodyRenderer);

                (bool headVisible, bool upperBodyVisible, bool lowerBodyVisible, bool feetVisible) = AvatarSystemUtils.GetActiveBodyParts(bodyshape.id, wearables);

                var activeBodyParts = AvatarSystemUtils.GetActiveBodyPartsRenderers(bodyshapeLoader, headVisible, upperBodyVisible, lowerBodyVisible, feetVisible);

                // AvatarMeshCombiner is a bit buggy when performing the combine of the same meshes on the same frame,
                // once that's fixed we can remove this wait
                // AttachExternalCancellation is needed because cancellation will take a wait to trigger
                await UniTask.WaitForEndOfFrame(ct).AttachExternalCancellation(ct);

                if (!MergeAvatar(activeBodyParts.Union(loaders.Values.SelectMany(x => x.rendereable.renderers.OfType<SkinnedMeshRenderer>())), out SkinnedMeshRenderer combinedRenderer))
                {
                    status = ILoader.Status.Failed_Mayor;
                    throw new Exception("Couldnt merge avatar");
                }

                this.combinedRenderer = combinedRenderer;
                if (headVisible)
                    facialFeaturesRenderers = new Renderer[] { bodyshapeLoader.eyesRenderer, bodyshapeLoader.eyebrowsRenderer, bodyshapeLoader.mouthRenderer };
                else
                    //Loader is not in charge of visibility, since everything loaded has the renderer disabled
                    //we can just leave this field nulled 
                    facialFeaturesRenderers = null; 
            }
            catch (OperationCanceledException)
            {
                Dispose();
                throw;
            }
            catch
            {
                Dispose();
                Debug.Log("Failed Loading avatar");
                throw;
            }
            finally
            {
                DisposeCleanUpLoaders();
            }
        }

        internal static (List<IWearableLoader> notReusableLoaders, List<IWearableLoader> newLoaders) GetNewLoaders(List<WearableItem> wearables, Dictionary<string, IWearableLoader> currentLoaders, IWearableLoaderFactory wearableLoaderFactory)
        {
            // Initialize with all loaders and remove from cleaning-up the ones that can be reused
            List<IWearableLoader> notReusableLoaders = new List<IWearableLoader>(currentLoaders.Values);
            List<IWearableLoader> newLoaders = new List<IWearableLoader>();

            for (int i = 0; i < wearables.Count; i++)
            {
                WearableItem wearable = wearables[i];

                if (currentLoaders.TryGetValue(wearable.data.category, out IWearableLoader loader))
                {
                    //We can reuse this loader
                    if (loader.wearable.id == wearable.id)
                    {
                        newLoaders.Add(loader);
                        notReusableLoaders.Remove(loader);
                        continue;
                    }
                }
                newLoaders.Add(wearableLoaderFactory.GetWearableLoader(wearable));
            }

            return (notReusableLoaders, newLoaders);
        }

        public Transform[] GetBones() { return bodyshapeLoader?.upperBodyRenderer?.bones; }

        private bool MergeAvatar(IEnumerable<SkinnedMeshRenderer> allRenderers, out SkinnedMeshRenderer renderer)
        {
            renderer = null;
            var featureFlags = DataStore.i.featureFlags.flags.Get();
            avatarMeshCombiner.useCullOpaqueHeuristic = featureFlags.IsFeatureEnabled("cull-opaque-heuristic");
            avatarMeshCombiner.enableCombinedMesh = false;

            bool success = avatarMeshCombiner.Combine(bodyshapeLoader.upperBodyRenderer, allRenderers.ToArray());
            if (!success)
                return false;

            avatarMeshCombiner.container.transform.SetParent(container.transform, true);
            avatarMeshCombiner.container.transform.localPosition = Vector3.zero;

            renderer = avatarMeshCombiner.renderer;
            return true;
        }

        internal static ILoader.Status ComposeStatus(Dictionary<string, IWearableLoader> loaders)
        {
            ILoader.Status composedStatus = ILoader.Status.Succeeded;
            foreach ((string category, IWearableLoader loader) in loaders)
            {
                if (loader.status == IWearableLoader.Status.Defaulted)
                    composedStatus = ILoader.Status.Failed_Minor;
                else if (loader.status == IWearableLoader.Status.Failed)
                {
                    if (AvatarSystemUtils.IsCategoryRequired(category))
                        return ILoader.Status.Failed_Mayor;
                    composedStatus = ILoader.Status.Failed_Minor;
                }
            }
            return composedStatus;
        }

        private void ClearLoaders()
        {
            bodyshapeLoader?.Dispose();
            foreach (IWearableLoader wearableLoader in loaders.Values)
            {
                wearableLoader.Dispose();
            }
            loaders.Clear();
        }

        public void Dispose()
        {
            avatarMeshCombiner.Dispose();
            status = ILoader.Status.Idle;
            ClearLoaders();
        }
    }
}