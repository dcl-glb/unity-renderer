using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class SkyboxCameraAdjustment : MonoBehaviour
{
    public int skyboxLayer;
    public Camera skyboxCamera;

    bool initialized = false;

    // Update is called once per frame
    void Update()
    {
        if (initialized || Camera.main == null)
        {
            return;
        }
        AdjustMainCamera();
    }

    void AdjustMainCamera()
    {
        Camera mainCam = Camera.main;
        var cameraData = mainCam.GetUniversalAdditionalCameraData();
        cameraData.renderType = CameraRenderType.Overlay;

        var skyBoxCamData = skyboxCamera.GetUniversalAdditionalCameraData();
        skyBoxCamData.cameraStack.Add(mainCam);

        //mainCam.cullingMask = mainCam.cullingMask & ~(1 << skyboxLayer);
        //mainCam.clearFlags = CameraClearFlags.Nothing;
        initialized = true;
    }
}
