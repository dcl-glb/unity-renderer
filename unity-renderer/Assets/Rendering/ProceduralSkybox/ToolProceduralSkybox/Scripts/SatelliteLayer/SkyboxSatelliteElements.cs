using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DCL.Skybox
{
    public class SkyboxSatelliteElements
    {
        const string satelliteParentResourcePath = "SkyboxPrefabs/Satellite Parent";

        private GameObject skyboxElements;
        private GameObject satelliteElements;
        private GameObject satelliteParentPrefab;

        Dictionary<GameObject, Queue<SatelliteReferences>> satelliteReferences = new Dictionary<GameObject, Queue<SatelliteReferences>>();
        List<SatelliteReferences> usedSatellites = new List<SatelliteReferences>();

        public SkyboxSatelliteElements(GameObject skyboxElements)
        {
            this.skyboxElements = skyboxElements;
            // Get or instantiate Skybox elements GameObject
            GetOrInstantiatePlanarElements();
        }

        private void GetOrInstantiatePlanarElements()
        {
            Transform satelliteParentTransform = skyboxElements.transform.Find("Satellite Elements");

            if (satelliteParentTransform != null)
            {
                Object.DestroyImmediate(satelliteParentTransform.gameObject);
            }

            satelliteElements = new GameObject("Satellite Elements");
            satelliteElements.layer = LayerMask.NameToLayer("Skybox");
            satelliteElements.transform.parent = skyboxElements.transform;

            FollowBehavior followObj = satelliteElements.AddComponent<FollowBehavior>();
            followObj.followPos = true;
            followObj.ignoreYAxis = true;
        }

        internal void ApplySatelliteConfigurations(SkyboxConfiguration config, float dayTime, float normalizedDayTime, Light directionalLightGO, float cycleTime)
        {
            ResetObjects();
            List<SatelliteReferences> satelliteRefs = GetSatelliteAllActiveSatelliteRefs(config.satelliteLayers);

            if (satelliteRefs.Count != config.satelliteLayers.Count)
            {
                Debug.LogError("Satellite not working!, cause of difference of count in config and 3D pool");
                return;
            }

            for (int i = 0; i < satelliteRefs.Count; i++)
            {
                // If satellite is disabled, disable the 3D object too.
                if (!config.satelliteLayers[i].enabled)
                {
                    satelliteRefs[i].satelliteParent.SetActive(false);
                    satelliteRefs[i].satelliteBehavior.ChangeRenderType(LayerRenderType.NotRendering);
                    continue;
                }

                satelliteRefs[i].satelliteBehavior.AssignValues(config.satelliteLayers[i], dayTime, cycleTime);
            }
        }

        private void ResetObjects()
        {
            if (usedSatellites != null)
            {
                for (int i = 0; i < usedSatellites.Count; i++)
                {
                    SatelliteReferences sat = usedSatellites[i];
                    sat.satelliteParent.SetActive(false);
                    satelliteReferences[sat.satellitePrefab].Enqueue(sat);
                }
                usedSatellites.Clear();
            }
        }

        private List<SatelliteReferences> GetSatelliteAllActiveSatelliteRefs(List<Config3DSatellite> satelliteLayers)
        {
            for (int i = 0; i < satelliteLayers.Count; i++)
            {
                GetSatelliteObject(satelliteLayers[i]);
            }
            return usedSatellites;
        }

        private SatelliteReferences GetSatelliteObject(Config3DSatellite config)
        {
            SatelliteReferences tempSatellite = null;

            if (config.satellite == null)
            {
                return tempSatellite;
            }

            // Check if GO for this prefab is already in scene, else create new
            if (satelliteReferences.ContainsKey(config.satellite))
            {
                // Check if there is any unused GO for the given prefab
                if (satelliteReferences[config.satellite].Count > 0)
                {
                    tempSatellite = satelliteReferences[config.satellite].Dequeue();
                }
                else
                {
                    tempSatellite = InstantiateNewSatelliteReference(config);
                }
            }
            else
            {
                satelliteReferences.Add(config.satellite, new Queue<SatelliteReferences>());
                tempSatellite = InstantiateNewSatelliteReference(config);
            }

            usedSatellites.Add(tempSatellite);
            tempSatellite.satelliteParent.SetActive(true);

            return tempSatellite;
        }

        private SatelliteReferences InstantiateNewSatelliteReference(Config3DSatellite config)
        {
            if (satelliteParentPrefab == null)
            {
                satelliteParentPrefab = Resources.Load<GameObject>(satelliteParentResourcePath);
            }

            GameObject obj = GameObject.Instantiate<GameObject>(satelliteParentPrefab);
            obj.layer = LayerMask.NameToLayer("Skybox");
            obj.name = "Satellite Parent";
            obj.transform.parent = satelliteElements.transform;
            obj.transform.localPosition = Vector3.zero;

            GameObject orbit = obj.transform.GetChild(0).gameObject;
            GameObject satelliteObj = GameObject.Instantiate(config.satellite);
            satelliteObj.transform.parent = orbit.transform;

            // Get satellite behavior and assign satellite 
            SatelliteLayerBehavior satelliteBehavior = obj.GetComponent<SatelliteLayerBehavior>();
            satelliteBehavior.satellite = satelliteObj;

            SatelliteReferences satellite = new SatelliteReferences();
            satellite.satelliteParent = obj;
            satellite.orbitGO = orbit;
            satellite.satelliteGO = satelliteObj;
            satellite.satellitePrefab = config.satellite;
            satellite.satelliteBehavior = satelliteBehavior;

            return satellite;
        }

        public void ResolveCameraDependency(Transform currentTransform)
        {
            if (satelliteElements != null)
            {
                satelliteElements.GetComponent<FollowBehavior>().target = currentTransform.gameObject;
            }
        }
    }
}