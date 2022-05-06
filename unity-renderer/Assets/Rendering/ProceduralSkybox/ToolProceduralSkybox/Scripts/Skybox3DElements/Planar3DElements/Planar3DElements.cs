using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DCL.Skybox
{
    public class PlanarRefs
    {
        public GameObject prefab;
        public GameObject planarObject;
    }

    public class Planar3DElements
    {
        private GameObject skyboxElementsGO;
        private Transform planarElementsGO;
        private Transform planarCameraFollowingElements;
        private Transform planarStaticElements;
        private FollowMainCamera followObj;

        private Dictionary<GameObject, Queue<PlanarRefs>> planarReferences = new Dictionary<GameObject, Queue<PlanarRefs>>();
        private List<PlanarRefs> usedPlanes = new List<PlanarRefs>();

        public Planar3DElements(GameObject skyboxElementsGO)
        {
            this.skyboxElementsGO = skyboxElementsGO;
            // Get or instantiate Skybox elements GameObject
            GetOrInstantiatePlanarElements();
        }

        private void GetOrInstantiatePlanarElements()
        {
            planarElementsGO = skyboxElementsGO.transform.Find("Planar Elements");

            if (planarElementsGO != null)
            {
                UnityEngine.Object.DestroyImmediate(planarElementsGO.gameObject);
            }

            planarElementsGO = new GameObject("Planar Elements").transform;
            planarElementsGO.parent = skyboxElementsGO.transform;
            planarElementsGO.gameObject.layer = LayerMask.NameToLayer("Skybox");

            planarCameraFollowingElements = new GameObject("Camera Following").transform;
            planarCameraFollowingElements.parent = planarElementsGO.transform;
            planarCameraFollowingElements.gameObject.layer = LayerMask.NameToLayer("Skybox");

            planarStaticElements = new GameObject("Static").transform;
            planarStaticElements.parent = planarElementsGO.transform;
            planarStaticElements.gameObject.layer = LayerMask.NameToLayer("Skybox");

            // Add follow script
            followObj = planarCameraFollowingElements.gameObject.AddComponent<FollowMainCamera>();
            followObj.ignoreYPos = true;
            followObj.followPos = true;
        }

        internal void AssignCameraInstance(Transform cameraTransform) { followObj.target = cameraTransform.gameObject; }

        internal void ApplyConfig(List<Config3DPlanar> planarLayers, float timeOfTheDay, float cycleTime, bool isEditor)
        {
            ResetPlanes();
            for (int i = 0; i < planarLayers.Count; i++)
            {
                // if this layer is disabled, continue
                if (!planarLayers[i].enabled)
                {
                    planarLayers[i].renderType = LayerRenderType.NotRendering;
                    continue;
                }

                // Get planar ref for config
                PlanarRefs tempRef = GetPlanarRef(planarLayers[i]);
                // If no prefab is assigned for current config, continue.
                if (tempRef == null)
                {
                    continue;
                }

                ApplyParticleProperties(planarLayers[i], tempRef);

                // Parent with the moving or static parent
                ParentPlanarLayer(planarLayers[i], tempRef);

                // Change layer mask
                ChangeRenderingCamera(planarLayers[i], tempRef);

                // if this layer is not active at present time, disable the object
                if (!IsLayerActiveInCurrentTime(timeOfTheDay, planarLayers[i], cycleTime))
                {
                    planarLayers[i].renderType = LayerRenderType.NotRendering;
                    tempRef.planarObject.SetActive(false);
                    continue;
                }
            }
        }

        private void ChangeRenderingCamera(Config3DPlanar config, PlanarRefs tempRef)
        {
            if (config.renderWithMainCamera)
            {
                tempRef.planarObject.layer = 0;
            }
            else
            {
                tempRef.planarObject.layer = LayerMask.NameToLayer("Skybox");
            }
        }

        private void ParentPlanarLayer(Config3DPlanar config, PlanarRefs tempRef)
        {
            if (config.followCamera)
            {
                tempRef.planarObject.transform.parent = planarCameraFollowingElements;
            }
            else
            {
                tempRef.planarObject.transform.parent = planarStaticElements;
            }
        }

        private void ApplyParticleProperties(Config3DPlanar config, PlanarRefs tempRef)
        {
            // Apply radius
            ParticleSystem particle = tempRef.planarObject.GetComponent<ParticleSystem>();
            var shape = particle.shape;
            shape.radius = config.radius;

            // Apply y-pos
            Vector3 pos = tempRef.planarObject.transform.position;
            pos.y = config.yPos;
            tempRef.planarObject.transform.position = pos;
        }

        public List<PlanarRefs> GetAllEnabledPlanarRefs(List<Config3DPlanar> planarLayers)
        {
            for (int i = 0; i < planarLayers.Count; i++)
            {
                GetPlanarRef(planarLayers[i]);
            }
            return usedPlanes;
        }

        public PlanarRefs GetPlanarRef(Config3DPlanar config)
        {
            PlanarRefs tempPlane = null;

            if (config.prefab == null)
            {
                return tempPlane;
            }

            // Check if GO for this prefab is already in scene, else create new
            if (planarReferences.ContainsKey(config.prefab))
            {
                // Check if there is any unused GO for the given prefab
                if (planarReferences[config.prefab].Count > 0)
                {
                    tempPlane = planarReferences[config.prefab].Dequeue();
                }
                else
                {
                    tempPlane = InstantiateNewSatelliteReference(config);
                }
            }
            else
            {
                planarReferences.Add(config.prefab, new Queue<PlanarRefs>());
                tempPlane = InstantiateNewSatelliteReference(config);
            }

            usedPlanes.Add(tempPlane);
            tempPlane.planarObject.SetActive(true);

            return tempPlane;
        }

        PlanarRefs InstantiateNewSatelliteReference(Config3DPlanar config)
        {
            GameObject obj = GameObject.Instantiate<GameObject>(config.prefab);
            obj.layer = LayerMask.NameToLayer("Skybox");
            obj.name = "Planar Layer";
            obj.transform.parent = planarElementsGO;
            obj.transform.localPosition = Vector3.zero;

            PlanarRefs planar = new PlanarRefs();
            planar.prefab = config.prefab;
            planar.planarObject = obj;

            return planar;
        }

        private void ResetPlanes()
        {
            if (usedPlanes != null)
            {
                for (int i = 0; i < usedPlanes.Count; i++)
                {
                    PlanarRefs plane = usedPlanes[i];
                    plane.planarObject.SetActive(false);
                    planarReferences[plane.prefab].Enqueue(plane);
                }
                usedPlanes.Clear();
            }
        }

        private bool IsLayerActiveInCurrentTime(float timeOfTheDay, Config3DPlanar config, float cycleTime)
        {
            // Calculate edited time for the case of out time less than in time (over the day scenario)
            float outTimeEdited = config.timeSpan_End;
            float timeOfTheDayEdited = timeOfTheDay;

            if (config.timeSpan_End < config.timeSpan_start)
            {
                outTimeEdited += cycleTime;
            }

            if (timeOfTheDay < config.timeSpan_start)
            {
                timeOfTheDayEdited += cycleTime;
            }

            if (timeOfTheDayEdited >= config.timeSpan_start && timeOfTheDayEdited <= outTimeEdited)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

    }
}