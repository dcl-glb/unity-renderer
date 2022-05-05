using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace DCL.Skybox
{

    public class SkyboxCamera
    {
        private GameObject skyboxCameraGO;
        private Camera skyboxCamera;
        private FollowBehavior followBehavior;

        public SkyboxCamera()
        {
            // Make a new camera
            skyboxCameraGO = new GameObject("Skybox Camera");
            skyboxCameraGO.transform.position = Vector3.zero;
            skyboxCameraGO.transform.rotation = Quaternion.identity;

            // Attach camera component
            skyboxCamera = skyboxCameraGO.AddComponent<Camera>();

            var cameraData = skyboxCamera.GetUniversalAdditionalCameraData();
            cameraData.renderShadows = false;
            skyboxCamera.useOcclusionCulling = false;
            skyboxCamera.cullingMask = (1 << LayerMask.NameToLayer("Skybox"));

            // Attach follow script
            followBehavior = skyboxCameraGO.AddComponent<FollowBehavior>();
            followBehavior.followPos = true;
            followBehavior.followRot = true;
        }

        public void AssignTargetCamera(Transform mainCam)
        {
            Camera camComponent = mainCam.GetComponent<Camera>();
            var mainCameraData = camComponent.GetUniversalAdditionalCameraData();
            mainCameraData.renderType = CameraRenderType.Overlay;

            var cameraData = skyboxCamera.GetUniversalAdditionalCameraData();
            cameraData.cameraStack.Add(camComponent);


            followBehavior.target = mainCam.gameObject;
        }

        public void SetCameraEnabledState(bool enabled) { skyboxCamera.enabled = enabled; }
    }
}