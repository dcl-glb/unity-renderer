using DCL;
using DCL.EmotesCustomization;
using DCL.Helpers;
using DCL.Interface;
using DCL.NotificationModel;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Categories = WearableLiterals.Categories;
using Environment = DCL.Environment;
using Random = UnityEngine.Random;
using Type = DCL.NotificationModel.Type;

public class AvatarEditorHUDController : IHUD
{
    private const int LOADING_OWNED_WEARABLES_RETRIES = 3;
    private const string LOADING_OWNED_WEARABLES_ERROR_MESSAGE = "There was a problem loading your wearables";
    private const string URL_MARKET_PLACE = "https://market.decentraland.org/browse?section=wearables";
    private const string URL_GET_A_WALLET = "https://docs.decentraland.org/get-a-wallet";
    private const string URL_SELL_COLLECTIBLE_GENERIC = "https://market.decentraland.org/account";
    private const string URL_SELL_SPECIFIC_COLLECTIBLE = "https://market.decentraland.org/contracts/{collectionId}/tokens/{tokenId}";
    private const string EMOTES_CUSTOMIZATION_FEATURE_FLAG = "emotes_customization";
    private const string THIRD_PARTY_COLLECTIONS_FEATURE_FLAG = "third_party_collections";
    protected static readonly string[] categoriesThatMustHaveSelection = { Categories.BODY_SHAPE, Categories.UPPER_BODY, Categories.LOWER_BODY, Categories.FEET, Categories.EYES, Categories.EYEBROWS, Categories.MOUTH };
    protected static readonly string[] categoriesToRandomize = { Categories.HAIR, Categories.EYES, Categories.EYEBROWS, Categories.MOUTH, Categories.FACIAL, Categories.HAIR, Categories.UPPER_BODY, Categories.LOWER_BODY, Categories.FEET };

    [NonSerialized]
    public bool bypassUpdateAvatarPreview = false;

    internal UserProfile userProfile;
    private BaseDictionary<string, WearableItem> catalog;
    bool renderingEnabled => CommonScriptableObjects.rendererState.Get();
    bool isPlayerRendererLoaded => DataStore.i.common.isPlayerRendererLoaded.Get();
    BaseVariable<bool> avatarEditorVisible => DataStore.i.HUDs.avatarEditorVisible;
    BaseVariable<Transform> configureBackpackInFullscreenMenu => DataStore.i.exploreV2.configureBackpackInFullscreenMenu;
    BaseVariable<bool> exploreV2IsOpen => DataStore.i.exploreV2.isOpen;
    DataStore_EmotesCustomization emotesCustomizationDataStore => DataStore.i.emotesCustomization;
    DataStore_FeatureFlag featureFlagsDataStore => DataStore.i.featureFlags;
    private bool isSkinsFeatureEnabled => DataStore.i.featureFlags.flags.Get().IsFeatureEnabled("avatar_skins");
    
    private readonly DataStore_FeatureFlag featureFlags;

    private readonly Dictionary<string, List<WearableItem>> wearablesByCategory = new Dictionary<string, List<WearableItem>>();
    protected readonly AvatarEditorHUDModel model = new AvatarEditorHUDModel();

    private ColorList skinColorList;
    private ColorList eyeColorList;
    private ColorList hairColorList;
    private bool prevMouseLockState = false;
    private int ownedWearablesRemainingRequests = LOADING_OWNED_WEARABLES_RETRIES;
    private bool ownedWearablesAlreadyLoaded = false;
    private List<Nft> ownedNftCollectionsL1 = new List<Nft>();
    private List<Nft> ownedNftCollectionsL2 = new List<Nft>();
    private bool avatarIsDirty = false;
    private float lastTimeOwnedWearablesChecked = 0;
    internal bool collectionsAlreadyLoaded = false;
    private float prevRenderScale = 1.0f;
    private bool isAvatarPreviewReady;
    private List<string> thirdPartyWearablesLoaded = new List<string>();

    internal IEmotesCustomizationComponentController emotesCustomizationComponentController;

    private bool isThirdPartyCollectionsEnabled => featureFlags.flags.Get().IsFeatureEnabled(THIRD_PARTY_COLLECTIONS_FEATURE_FLAG);

    public AvatarEditorHUDView view;

    public event Action OnOpen;
    public event Action OnClose;

    public AvatarEditorHUDController(DataStore_FeatureFlag featureFlags)
    {
        this.featureFlags = featureFlags;
    }

    public void Initialize(UserProfile userProfile, BaseDictionary<string, WearableItem> catalog, bool bypassUpdateAvatarPreview = false)
    {
        this.userProfile = userProfile;
        this.bypassUpdateAvatarPreview = bypassUpdateAvatarPreview;

        view = AvatarEditorHUDView.Create(this);

        view.skinsFeatureContainer.SetActive(isSkinsFeatureEnabled);
        avatarEditorVisible.OnChange += OnAvatarEditorVisibleChanged;
        OnAvatarEditorVisibleChanged(avatarEditorVisible.Get(), false);

        configureBackpackInFullscreenMenu.OnChange += ConfigureBackpackInFullscreenMenuChanged;
        ConfigureBackpackInFullscreenMenuChanged(configureBackpackInFullscreenMenu.Get(), null);

        exploreV2IsOpen.OnChange += ExploreV2IsOpenChanged;

        skinColorList = Resources.Load<ColorList>("SkinTone");
        hairColorList = Resources.Load<ColorList>("HairColor");
        eyeColorList = Resources.Load<ColorList>("EyeColor");
        view.SetColors(skinColorList.colors, hairColorList.colors, eyeColorList.colors);

        SetCatalog(catalog);

        LoadUserProfile(userProfile, true);
        this.userProfile.OnUpdate += LoadUserProfile;

        view.SetSectionActive(AvatarEditorHUDView.EMOTES_SECTION_INDEX, false);
        featureFlagsDataStore.flags.OnChange += OnFeatureFlagsChanged;
        OnFeatureFlagsChanged(featureFlagsDataStore.flags.Get(), null);
        emotesCustomizationDataStore.isEmotesCustomizationSelected.OnChange += HandleEmotesCostumizationSelection;
        emotesCustomizationDataStore.currentLoadedEmotes.OnAdded += OnNewEmoteAdded;

        if (featureFlagsDataStore.flags.Get().IsFeatureEnabled(EMOTES_CUSTOMIZATION_FEATURE_FLAG))
        {
            emotesCustomizationComponentController.onEmotePreviewed += OnPreviewEmote;
            emotesCustomizationComponentController.onEmoteEquipped += OnEmoteEquipped;
            emotesCustomizationComponentController.onEmoteUnequipped += OnEmoteUnequipped;
            emotesCustomizationComponentController.onEmoteSell += OnRedirectToEmoteSelling;
        }

        DataStore.i.HUDs.isAvatarEditorInitialized.Set(true);

        view.SetThirdPartyCollectionsVisibility(isThirdPartyCollectionsEnabled);
    }

    public void SetCatalog(BaseDictionary<string, WearableItem> catalog)
    {
        if (this.catalog != null)
        {
            this.catalog.OnAdded -= AddWearable;
            this.catalog.OnRemoved -= RemoveWearable;
        }

        this.catalog = catalog;

        ProcessCatalog(this.catalog);
        this.catalog.OnAdded += AddWearable;
        this.catalog.OnRemoved += RemoveWearable;
    }

    private void LoadUserProfile(UserProfile userProfile)
    {
        LoadUserProfile(userProfile, false);
        QueryNftCollections(userProfile.userId);
    }

    private void LoadOwnedWereables(UserProfile userProfile)
    {
        // If there is more than 1 minute that we have checked the owned wearables, we try it again
        // This is done in order to retrieved the wearables after you has claimed them
        if ((Time.realtimeSinceStartup < lastTimeOwnedWearablesChecked + 60 &&
             (ownedWearablesAlreadyLoaded ||
              ownedWearablesRemainingRequests <= 0)) ||
            string.IsNullOrEmpty(userProfile.userId))
            return;

        view.ShowCollectiblesLoadingSpinner(true);
        view.ShowCollectiblesLoadingRetry(false);
        lastTimeOwnedWearablesChecked = Time.realtimeSinceStartup;

        CatalogController.RequestOwnedWearables(userProfile.userId)
                         .Then((ownedWearables) =>
                         {
                             ownedWearablesAlreadyLoaded = true;
                             this.userProfile.SetInventory(ownedWearables.Select(x => x.id).Concat(thirdPartyWearablesLoaded).ToArray());
                             LoadUserProfile(userProfile, true);
                             view.ShowCollectiblesLoadingSpinner(false);
                             view.ShowSkinPopulatedList(ownedWearables.Any(item => item.IsSkin()));
                         })
                         .Catch((error) =>
                         {
                             ownedWearablesRemainingRequests--;
                             if (ownedWearablesRemainingRequests > 0)
                             {
                                 Debug.LogWarning("Retrying owned wereables loading...");
                                 LoadOwnedWereables(userProfile);
                             }
                             else
                             {
                                 NotificationsController.i.ShowNotification(new Model
                                 {
                                     message = LOADING_OWNED_WEARABLES_ERROR_MESSAGE,
                                     type = Type.GENERIC,
                                     timer = 10f,
                                     destroyOnFinish = true
                                 });

                                 view.ShowCollectiblesLoadingSpinner(false);
                                 view.ShowCollectiblesLoadingRetry(true);
                                 Debug.LogError(error);
                             }
                         });
    }

    private void QueryNftCollections(string userId)
    {
        if (string.IsNullOrEmpty(userId))
            return;

        Environment.i.platform.serviceProviders.theGraph.QueryNftCollections(userProfile.userId, NftCollectionsLayer.ETHEREUM)
           .Then((nfts) => ownedNftCollectionsL1 = nfts)
           .Catch((error) => Debug.LogError(error));

        Environment.i.platform.serviceProviders.theGraph.QueryNftCollections(userProfile.userId, NftCollectionsLayer.MATIC)
           .Then((nfts) => ownedNftCollectionsL2 = nfts)
           .Catch((error) => Debug.LogError(error));
    }

    public void RetryLoadOwnedWearables()
    {
        ownedWearablesRemainingRequests = LOADING_OWNED_WEARABLES_RETRIES;
        LoadOwnedWereables(userProfile);
    }

    private void PlayerRendererLoaded(bool current, bool previous)
    {
        if (!current)
            return;

        if (!ownedWearablesAlreadyLoaded)
        {
            List<string> equippedOwnedWearables = new List<string>();
            for (int i = 0; i < userProfile.avatar.wearables.Count; i++)
            {
                if (catalog.TryGetValue(userProfile.avatar.wearables[i], out WearableItem wearable) &&
                    !wearable.data.tags.Contains(WearableLiterals.Tags.BASE_WEARABLE))
                {
                    equippedOwnedWearables.Add(userProfile.avatar.wearables[i]);
                }
            }

            userProfile.SetInventory(equippedOwnedWearables.ToArray());
        }

        LoadUserProfile(userProfile, true);
        DataStore.i.common.isPlayerRendererLoaded.OnChange -= PlayerRendererLoaded;
    }

    public void LoadUserProfile(UserProfile userProfile, bool forceLoading)
    {
        bool avatarEditorNotVisible = renderingEnabled && !view.isOpen;
        bool isPlaying = !Application.isBatchMode;

        if (!forceLoading)
        {
            if (isPlaying && avatarEditorNotVisible)
                return;
        }

        if (userProfile == null)
            return;

        if (userProfile.avatar == null || string.IsNullOrEmpty(userProfile.avatar.bodyShape))
            return;

        /*TODO: this has to be refactored, currently there is no other way of understanding if the user is a regular or a guest
        *       due to the execution order of things. The init cannot be done below because that would mean to do it when the
        *       menu is firstly opened
        */
        view.InitializeNavigationEvents(string.IsNullOrEmpty(userProfile.userName));

        CatalogController.wearableCatalog.TryGetValue(userProfile.avatar.bodyShape, out var bodyShape);

        if (bodyShape == null)
        {
            return;
        }

        view.SetIsWeb3(userProfile.hasConnectedWeb3);

        ProcessCatalog(this.catalog);
        EquipBodyShape(bodyShape);
        EquipSkinColor(userProfile.avatar.skinColor);
        EquipHairColor(userProfile.avatar.hairColor);
        EquipEyesColor(userProfile.avatar.eyeColor);

        model.wearables.Clear();
        view.UnselectAllWearables();

        int wearablesCount = userProfile.avatar.wearables.Count;

        if (isPlayerRendererLoaded)
        {
            for (var i = 0; i < wearablesCount; i++)
            {
                CatalogController.wearableCatalog.TryGetValue(userProfile.avatar.wearables[i], out var wearable);
                if (wearable == null)
                {
                    Debug.LogError($"Couldn't find wearable with ID {userProfile.avatar.wearables[i]}");
                    continue;
                }

                if (wearable.IsEmote())
                    EquipEmote(wearable);
                else
                    EquipWearable(wearable);
            }
        }

        EnsureWearablesCategoriesNotEmpty();

        UpdateAvatarPreview();
        isAvatarPreviewReady = true;
    }

    private void EnsureWearablesCategoriesNotEmpty()
    {
        var categoriesInUse = model.wearables
            .Where(x => !x.IsEmote())
            .Select(x => x.data.category).ToArray();

        for (var i = 0; i < categoriesThatMustHaveSelection.Length; i++)
        {
            var category = categoriesThatMustHaveSelection[i];
            if (category != Categories.BODY_SHAPE && !(categoriesInUse.Contains(category)))
            {
                WearableItem wearable;
                var defaultItemId = WearableLiterals.DefaultWearables.GetDefaultWearable(model.bodyShape.id, category);
                if (defaultItemId != null)
                {
                    CatalogController.wearableCatalog.TryGetValue(defaultItemId, out wearable);
                }
                else
                {
                    wearable = wearablesByCategory[category].FirstOrDefault(x => x.SupportsBodyShape(model.bodyShape.id));
                }

                if (wearable != null)
                {
                    EquipWearable(wearable);
                }
            }
        }
    }

    public void WearableClicked(string wearableId)
    {
        CatalogController.wearableCatalog.TryGetValue(wearableId, out var wearable);
        if (wearable == null) return;

        if (wearable.data.category == Categories.BODY_SHAPE)
        {
            if (wearable.id == model.bodyShape.id)
                return;
            EquipBodyShape(wearable);
        }
        else
        {
            if (model.wearables.Contains(wearable))
            {
                if (!categoriesThatMustHaveSelection.Contains(wearable.data.category))
                {
                    UnequipWearable(wearable);
                }
                else
                {
                    return;
                }
            }
            else
            {
                if (IsTryingToReplaceSkin(wearable))
                    UnequipWearable(model.GetWearable(Categories.SKIN));
                
                var sameCategoryEquipped = model.GetWearable(wearable.data.category);
                if (sameCategoryEquipped != null)
                    UnequipWearable(sameCategoryEquipped);
                
                EquipWearable(wearable);
            }
        }

        UpdateAvatarPreview();
    }

    public void HairColorClicked(Color color)
    {
        EquipHairColor(color);
        view.SelectHairColor(model.hairColor);
        UpdateAvatarPreview();
    }

    public void SkinColorClicked(Color color)
    {
        EquipSkinColor(color);
        view.SelectSkinColor(model.skinColor);
        UpdateAvatarPreview();
    }

    public void EyesColorClicked(Color color)
    {
        EquipEyesColor(color);
        view.SelectEyeColor(model.eyesColor);
        UpdateAvatarPreview();
    }

    protected virtual void UpdateAvatarPreview()
    {
        if (bypassUpdateAvatarPreview)
            return;

        AvatarModel modelToUpdate = model.ToAvatarModel();

        // We always keep the loaded emotes into the Avatar Preview
        foreach (string emoteId in emotesCustomizationDataStore.currentLoadedEmotes.Get())
        {
            if (!modelToUpdate.wearables.Contains(emoteId))
                modelToUpdate.wearables.Add(emoteId);
        }

        view.UpdateAvatarPreview(modelToUpdate);
    }

    private void EquipHairColor(Color color)
    {
        var colorToSet = color;
        if (!hairColorList.colors.Any(x => x.AproxComparison(colorToSet)))
        {
            colorToSet = hairColorList.colors[hairColorList.defaultColor];
        }

        model.hairColor = colorToSet;
        view.SelectHairColor(model.hairColor);
    }

    private void EquipEyesColor(Color color)
    {
        var colorToSet = color;
        if (!eyeColorList.colors.Any(x => x.AproxComparison(color)))
        {
            colorToSet = eyeColorList.colors[eyeColorList.defaultColor];
        }

        model.eyesColor = colorToSet;
        view.SelectEyeColor(model.eyesColor);
    }

    private void EquipSkinColor(Color color)
    {
        var colorToSet = color;
        if (!skinColorList.colors.Any(x => x.AproxComparison(colorToSet)))
        {
            colorToSet = skinColorList.colors[skinColorList.defaultColor];
        }

        model.skinColor = colorToSet;
        view.SelectSkinColor(model.skinColor);
    }

    private void EquipBodyShape(WearableItem bodyShape)
    {
        if (bodyShape.data.category != Categories.BODY_SHAPE)
        {
            Debug.LogError($"Item ({bodyShape.id} is not a body shape");
            return;
        }

        if (model.bodyShape == bodyShape)
            return;

        model.bodyShape = bodyShape;
        view.UpdateSelectedBody(bodyShape);

        int wearablesCount = model.wearables.Count;
        for (var i = wearablesCount - 1; i >= 0; i--)
        {
            UnequipWearable(model.wearables[i]);
        }

        var defaultWearables = WearableLiterals.DefaultWearables.GetDefaultWearables(bodyShape.id);
        for (var i = 0; i < defaultWearables.Length; i++)
        {
            if (catalog.TryGetValue(defaultWearables[i], out var wearable))
                EquipWearable(wearable);
        }
    }

    private void EquipWearable(WearableItem wearable)
    {
        if (wearable.IsEmote())
            return;

        if (!wearablesByCategory.ContainsKey(wearable.data.category))
            return;

        if (wearablesByCategory[wearable.data.category].Contains(wearable) && wearable.SupportsBodyShape(model.bodyShape.id) && !model.wearables.Contains(wearable))
        {
            var toReplace = GetWearablesReplacedBy(wearable);
            toReplace.ForEach(UnequipWearable);
            model.wearables.Add(wearable);
            view.EquipWearable(wearable);
            avatarIsDirty = true;
        }
    }

    private void UnequipWearable(WearableItem wearable)
    {
        if (wearable.IsEmote())
            return;

        if (model.wearables.Contains(wearable))
        {
            model.wearables.Remove(wearable);
            view.UnequipWearable(wearable);
            avatarIsDirty = true;
        }
    }

    private void EquipEmote(WearableItem emote)
    {
        if (!emote.IsEmote())
            return;

        if (!model.wearables.Contains(emote))
        {
            model.wearables.Add(emote);
            avatarIsDirty = true;
        }
    }

    private void UnequipEmote(WearableItem emote)
    {
        if (!emote.IsEmote())
            return;

        if (model.wearables.Contains(emote))
        {
            model.wearables.Remove(emote);
            avatarIsDirty = true;
        }
    }

    public void UnequipAllWearables()
    {
        foreach (var wearable in model.wearables)
        {
            if (!wearable.IsEmote())
                view.UnequipWearable(wearable);
        }

        model.wearables.Clear();
    }

    private void ProcessCatalog(BaseDictionary<string, WearableItem> catalog)
    {
        wearablesByCategory.Clear();
        view.RemoveAllWearables();
        using (var iterator = catalog.Get().GetEnumerator())
        {
            while (iterator.MoveNext())
            {
                if (iterator.Current.Value.IsEmote())
                    continue;

                AddWearable(iterator.Current.Key, iterator.Current.Value);
            }
        }

        view.RefreshSelectorsSize();
    }

    private void AddWearable(string id, WearableItem wearable)
    {
        if (!wearable.data.tags.Contains(WearableLiterals.Tags.BASE_WEARABLE) && userProfile.GetItemAmount(id) == 0)
            return;

        if (!wearablesByCategory.ContainsKey(wearable.data.category))
            wearablesByCategory.Add(wearable.data.category, new List<WearableItem>());

        wearablesByCategory[wearable.data.category].Add(wearable);
        view.AddWearable(wearable, userProfile.GetItemAmount(id),
            ShouldShowHideOtherWearablesToast,
            ShouldShowReplaceOtherWearablesToast);
    }

    private void RemoveWearable(string id, WearableItem wearable)
    {
        if (wearablesByCategory.ContainsKey(wearable.data.category))
        {
            if (wearablesByCategory[wearable.data.category].Remove(wearable))
            {
                if (wearablesByCategory[wearable.data.category].Count == 0)
                {
                    wearablesByCategory.Remove(wearable.data.category);
                }
            }
        }

        view.RemoveWearable(wearable);
    }

    public void RandomizeWearables()
    {
        EquipHairColor(hairColorList.colors[Random.Range(0, hairColorList.colors.Count)]);
        EquipEyesColor(eyeColorList.colors[Random.Range(0, eyeColorList.colors.Count)]);

        List<WearableItem> wearablesToRemove = model.wearables.Where(x => !x.IsEmote()).ToList();
        foreach (var wearable in wearablesToRemove)
        {
            model.wearables.Remove(wearable);
        }

        view.UnselectAllWearables();
        using (var iterator = wearablesByCategory.GetEnumerator())
        {
            while (iterator.MoveNext())
            {
                string category = iterator.Current.Key;
                if (!categoriesToRandomize.Contains(category))
                {
                    continue;
                }

                var supportedWearables = iterator.Current.Value.Where(x => x.SupportsBodyShape(model.bodyShape.id)).ToArray();
                if (supportedWearables.Length == 0)
                {
                    Debug.LogError($"Couldn't get any wearable for category {category} and bodyshape {model.bodyShape.id}");
                }

                var wearable = supportedWearables[Random.Range(0, supportedWearables.Length - 1)];
                EquipWearable(wearable);
            }
        }

        UpdateAvatarPreview();
    }

    private List<WearableItem> GetWearablesReplacedBy(WearableItem wearableItem)
    {
        var wearablesToReplace = new List<WearableItem>();
        var categoriesToReplace = new HashSet<string>(wearableItem.GetReplacesList(model.bodyShape.id) ?? new string[0]);

        int wearableCount = model.wearables.Count;
        for (int i = 0; i < wearableCount; i++)
        {
            var wearable = model.wearables[i];
            if (wearable == null) continue;

            if (categoriesToReplace.Contains(wearable.data.category))
            {
                wearablesToReplace.Add(wearable);
            }
            else
            {
                //For retrocompatibility's sake we check current wearables against new one (compatibility matrix is symmetrical)
                HashSet<string> replacesList = new HashSet<string>(wearable.GetReplacesList(model.bodyShape.id) ?? new string[0]);
                if (replacesList.Contains(wearableItem.data.category))
                {
                    wearablesToReplace.Add(wearable);
                }
            }
        }

        return wearablesToReplace;
    }

    public void SetVisibility(bool visible) { avatarEditorVisible.Set(visible); }

    private void OnAvatarEditorVisibleChanged(bool current, bool previous) { SetVisibility_Internal(current); }

    public void SetVisibility_Internal(bool visible)
    {
        if (!visible && view.isOpen)
        {
            view.ResetPreviewEmote();

            if (DataStore.i.common.isSignUpFlow.Get())
                DataStore.i.virtualAudioMixer.sceneSFXVolume.Set(1f);

            Environment.i.messaging.manager.paused = false;
            DataStore.i.skyboxConfig.avatarMatProfile.Set(AvatarMaterialProfile.InWorld);
            if (prevMouseLockState && DataStore.i.common.isSignUpFlow.Get())
            {
                Utils.LockCursor();
            }

            // NOTE(Brian): SSAO doesn't work correctly with the offseted avatar preview if the renderScale != 1.0
            var asset = GraphicsSettings.renderPipelineAsset as UniversalRenderPipelineAsset;
            asset.renderScale = prevRenderScale;

            if (DataStore.i.common.isSignUpFlow.Get())
                CommonScriptableObjects.isFullscreenHUDOpen.Set(false);

            DataStore.i.common.isPlayerRendererLoaded.OnChange -= PlayerRendererLoaded;

            OnClose?.Invoke();
        }
        else if (visible && !view.isOpen)
        {
            if (DataStore.i.common.isSignUpFlow.Get())
            {
                DataStore.i.virtualAudioMixer.sceneSFXVolume.Set(0f);
                view.sectionSelector.Hide(true);
            }
            else
            {
                view.sectionSelector.Show(true);
            }

            LoadOwnedWereables(userProfile);
            LoadCollections();
            Environment.i.messaging.manager.paused = DataStore.i.common.isSignUpFlow.Get();
            DataStore.i.skyboxConfig.avatarMatProfile.Set(AvatarMaterialProfile.InEditor);

            prevMouseLockState = Utils.IsCursorLocked;

            if (DataStore.i.common.isSignUpFlow.Get() || !DataStore.i.exploreV2.isInitialized.Get())
                Utils.UnlockCursor();

            // NOTE(Brian): SSAO doesn't work correctly with the offseted avatar preview if the renderScale != 1.0
            var asset = GraphicsSettings.renderPipelineAsset as UniversalRenderPipelineAsset;
            prevRenderScale = asset.renderScale;
            asset.renderScale = 1.0f;

            if (DataStore.i.common.isSignUpFlow.Get())
                CommonScriptableObjects.isFullscreenHUDOpen.Set(true);

            DataStore.i.common.isPlayerRendererLoaded.OnChange += PlayerRendererLoaded;

            OnOpen?.Invoke();
        }

        view.SetVisibility(visible);
    }

    public void Dispose()
    {
        avatarEditorVisible.OnChange -= OnAvatarEditorVisibleChanged;
        configureBackpackInFullscreenMenu.OnChange -= ConfigureBackpackInFullscreenMenuChanged;
        DataStore.i.common.isPlayerRendererLoaded.OnChange -= PlayerRendererLoaded;
        exploreV2IsOpen.OnChange -= ExploreV2IsOpenChanged;
        featureFlagsDataStore.flags.OnChange -= OnFeatureFlagsChanged;
        emotesCustomizationDataStore.isEmotesCustomizationSelected.OnChange -= HandleEmotesCostumizationSelection;
        emotesCustomizationDataStore.currentLoadedEmotes.OnAdded -= OnNewEmoteAdded;

        if (featureFlagsDataStore.flags.Get().IsFeatureEnabled(EMOTES_CUSTOMIZATION_FEATURE_FLAG))
        {
            emotesCustomizationComponentController.onEmotePreviewed -= OnPreviewEmote;
            emotesCustomizationComponentController.onEmoteEquipped -= OnEmoteEquipped;
            emotesCustomizationComponentController.onEmoteUnequipped -= OnEmoteUnequipped;
            emotesCustomizationComponentController.onEmoteSell -= OnRedirectToEmoteSelling;
        }

        CleanUp();
    }

    public void CleanUp()
    {
        UnequipAllWearables();

        if (view != null)
            view.CleanUp();

        this.userProfile.OnUpdate -= LoadUserProfile;
        this.catalog.OnAdded -= AddWearable;
        this.catalog.OnRemoved -= RemoveWearable;
        DataStore.i.common.isPlayerRendererLoaded.OnChange -= PlayerRendererLoaded;
    }

    public void SetConfiguration(HUDConfiguration configuration) { SetVisibility(configuration.active); }

    public void SaveAvatar(Texture2D face256Snapshot, Texture2D bodySnapshot)
    {
        var avatarModel = model.ToAvatarModel();

        WebInterface.SendSaveAvatar(avatarModel, face256Snapshot, bodySnapshot, DataStore.i.common.isSignUpFlow.Get());
        userProfile.OverrideAvatar(avatarModel, face256Snapshot);
        if (DataStore.i.common.isSignUpFlow.Get())
            DataStore.i.HUDs.signupVisible.Set(true);

        emotesCustomizationDataStore.equippedEmotes.Set(emotesCustomizationDataStore.unsavedEquippedEmotes.Get());
        avatarIsDirty = false;
        SetVisibility(false);
    }

    public void GoToMarketplaceOrConnectWallet()
    {
        if (userProfile.hasConnectedWeb3)
            WebInterface.OpenURL(URL_MARKET_PLACE);
        else
            WebInterface.OpenURL(URL_GET_A_WALLET);
    }

    public void SellCollectible(string collectibleId)
    {
        var ownedCollectible = ownedNftCollectionsL1.FirstOrDefault(nft => nft.urn == collectibleId);
        if (ownedCollectible == null)
            ownedCollectible = ownedNftCollectionsL2.FirstOrDefault(nft => nft.urn == collectibleId);

        if (ownedCollectible != null)
            WebInterface.OpenURL(URL_SELL_SPECIFIC_COLLECTIBLE.Replace("{collectionId}", ownedCollectible.collectionId).Replace("{tokenId}", ownedCollectible.tokenId));
        else
            WebInterface.OpenURL(URL_SELL_COLLECTIBLE_GENERIC);
    }

    public void ToggleVisibility() { SetVisibility(!view.isOpen); }

    private void ConfigureBackpackInFullscreenMenuChanged(Transform currentParentTransform, Transform previousParentTransform) { view.SetAsFullScreenMenuMode(currentParentTransform); }

    private void ExploreV2IsOpenChanged(bool current, bool previous)
    {
        if (!current && avatarIsDirty)
        {
            LoadUserProfile(userProfile, true);

            if (featureFlagsDataStore.flags.Get().IsFeatureEnabled(EMOTES_CUSTOMIZATION_FEATURE_FLAG))
                emotesCustomizationComponentController.RestoreEmoteSlots();

            avatarIsDirty = false;
        }
    }

    private void LoadCollections()
    {
        if (!isThirdPartyCollectionsEnabled || collectionsAlreadyLoaded)
            return;

        WearablesFetchingHelper.GetThirdPartyCollections()
            .Then((collections) =>
            {
                view.LoadCollectionsDropdown(collections);
                collectionsAlreadyLoaded = true;
                LoadUserThirdPartyWearables();
            })
            .Catch((error) => Debug.LogError(error));
    }

    private void LoadUserThirdPartyWearables()
    {
        List<string> collectionIdsToLoad = new List<string>();
        foreach (string wearableId in userProfile.avatar.wearables)
        {
            CatalogController.wearableCatalog.TryGetValue(wearableId, out var wearable);

            if (wearable != null && wearable.IsFromThirdPartyCollection)
            {
                if (!collectionIdsToLoad.Contains(wearable.ThirdPartyCollectionId))
                    collectionIdsToLoad.Add(wearable.ThirdPartyCollectionId);
            }
        }

        foreach (string collectionId in collectionIdsToLoad)
        {
            view.ToggleThirdPartyCollection(collectionId, true);
        }
    }

    public void ToggleThirdPartyCollection(bool isOn, string collectionId, string collectionName)
    {
        if (isOn)
            FetchAndShowThirdPartyCollection(collectionId, collectionName);
        else
            RemoveThirdPartyCollection(collectionId);
    }

    private void FetchAndShowThirdPartyCollection(string collectionId, string collectionName)
    {
        view.BlockCollectionsDropdown(true);
        CatalogController.RequestThirdPartyWearablesByCollection(userProfile.userId, collectionId)
            .Then(wearables =>
            {
                foreach (var wearable in wearables)
                {
                    if (!userProfile.ContainsInInventory(wearable.id))
                    {
                        userProfile.AddToInventory(wearable.id);
                        
                        if (!thirdPartyWearablesLoaded.Contains(wearable.id))
                            thirdPartyWearablesLoaded.Add(wearable.id);
                    }
                }

                view.BlockCollectionsDropdown(false);
                LoadUserProfile(userProfile, true);
            })
            .Catch((error) =>
            {
                view.BlockCollectionsDropdown(false);
                Debug.LogError(error);
            });
    }

    private void RemoveThirdPartyCollection(string collectionId)
    {
        var wearablesToRemove = CatalogController.i.Wearables.GetValues()
            .Where(wearable => !userProfile.HasEquipped(wearable.id)
                               && wearable.ThirdPartyCollectionId == collectionId)
            .Select(item => item.id)
            .ToList();
        CatalogController.i.Remove(wearablesToRemove);

        foreach (string wearableId in wearablesToRemove)
        {
            userProfile.RemoveFromInventory(wearableId);
            thirdPartyWearablesLoaded.Remove(wearableId);
        }

        LoadUserProfile(userProfile, true);
    }

    private bool ShouldShowHideOtherWearablesToast(WearableItem wearable)
    {
        var isWearingSkinAlready = model.wearables.Any(item => item.IsSkin());
        return wearable.IsSkin() && !isWearingSkinAlready;
    }

    private bool IsTryingToReplaceSkin(WearableItem wearable)
    {
        return model.wearables.Any(skin =>
        {
            return skin.IsSkin()
                   && skin.DoesHide(wearable.data.category, model.bodyShape.id);
        });
    }
    
    private bool ShouldShowReplaceOtherWearablesToast(WearableItem wearable)
    {
        if (IsTryingToReplaceSkin(wearable)) return true;
        var toReplace = GetWearablesReplacedBy(wearable);
        if (wearable == null || toReplace.Count == 0) return false;
        if (model.wearables.Contains(wearable)) return false;
        
        // NOTE: why just 1?
        if (toReplace.Count == 1)
        {
            var w = toReplace[0];
            if (w.data.category == wearable.data.category)
                return false;
        }
        return true;
    }

    private void OnFeatureFlagsChanged(FeatureFlag current, FeatureFlag previous)
    {
        if (!current.IsFeatureEnabled(EMOTES_CUSTOMIZATION_FEATURE_FLAG))
            return;

        featureFlagsDataStore.flags.OnChange -= OnFeatureFlagsChanged;
        emotesCustomizationComponentController = CreateEmotesController();
        IEmotesCustomizationComponentView emotesSectionView = emotesCustomizationComponentController.Initialize(
            DataStore.i.emotesCustomization,
            DataStore.i.emotes,
            DataStore.i.exploreV2,
            DataStore.i.HUDs,
            userProfile, 
            catalog);
        emotesSectionView.viewTransform.SetParent(view.emotesSection.transform, false);

        view.SetSectionActive(AvatarEditorHUDView.EMOTES_SECTION_INDEX, true);
    }

    private void HandleEmotesCostumizationSelection(bool current, bool previous)
    {
        if (!current)
            return;

        view.sectionSelector.GetSection(AvatarEditorHUDView.EMOTES_SECTION_INDEX).SelectToggle();
    }

    private void OnNewEmoteAdded(string emoteId)
    {
        if (!isAvatarPreviewReady)
            return;

        UpdateAvatarPreview();
    }

    private void OnPreviewEmote(string emoteId) { view.PlayPreviewEmote(emoteId); }

    private void OnEmoteEquipped(string emoteId)
    {
        catalog.TryGetValue(emoteId, out WearableItem equippedEmote);

        if (equippedEmote != null)
            EquipEmote(equippedEmote);
    }

    private void OnEmoteUnequipped(string emoteId)
    {
        catalog.TryGetValue(emoteId, out WearableItem unequippedEmote);

        if (unequippedEmote != null)
            UnequipEmote(unequippedEmote);
    }

    private void OnRedirectToEmoteSelling(string emoteId) { SellCollectible(emoteId); }

    internal virtual IEmotesCustomizationComponentController CreateEmotesController() => new EmotesCustomizationComponentController();
}