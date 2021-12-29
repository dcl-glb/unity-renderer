﻿using System;
using DCL.Builder;
using UnityEngine;

public interface ILandPublisherController
{
    event Action<IBuilderScene> OnPublishPressed;

    /// <summary>
    /// Init the controller with the default view
    /// </summary>
    void Initialize();

    /// <summary>
    /// Init the view with an specific view
    /// </summary>
    /// <param name="landPublisherView"></param>
    void Initialize(ILandPublisherView landPublisherView);

    /// <summary>
    /// This will start the flow of the publishing
    /// </summary>
    /// <param name="scene"></param>
    void StartPublishFlow(IBuilderScene scene);
    void Dispose();
}

public class LandPublisherController : ILandPublisherController
{
    public event Action OnCancel;
    public event Action<IBuilderScene> OnPublishPressed;

    internal const string PREFAB_PATH = "Land/LandPublisherView";
    internal const string DEFAULT_SCENE_NAME = "My new place";
    internal const string DEFAULT_SCENE_DESC = "";

    internal ILandPublisherView landPublisherView;
    internal bool isValidated = false;
    internal IBuilderScene sceneToPublish;

    public void Initialize()
    {
        var template = Resources.Load<LandPublisherView>(PREFAB_PATH);
        Initialize(GameObject.Instantiate(template));
    }

    public void Initialize(ILandPublisherView landPublisherView)
    {
        this.landPublisherView = landPublisherView;

        landPublisherView.OnCancel += Cancel;
        landPublisherView.OnPublish += Publish;
        landPublisherView.OnSceneNameChange += ValidatePublicationInfo;

        SetDefaultPublicationInfo();
        ValidatePublicationInfo(landPublisherView.currentSceneName);
    }

    public void Dispose()
    {
        landPublisherView.OnCancel -= Cancel;
        landPublisherView.OnPublish -= Publish;
        landPublisherView.OnSceneNameChange -= ValidatePublicationInfo;
    }

    public void SetActive(bool isActive) { landPublisherView.SetActive(isActive); }

    public void Cancel()
    {
        SetActive(false);
        OnCancel?.Invoke();
    }

    public void StartPublishFlow(IBuilderScene scene)
    {
        sceneToPublish = scene;
        SetCustomPublicationInfo(scene);
        SetActive(true);
    }

    public void Publish()
    {
        if (!isValidated)
            return;

        sceneToPublish.manifest.project.title = landPublisherView.GetSceneName();
        sceneToPublish.manifest.project.description = landPublisherView.GetSceneDescription();

        SetActive(false);
        OnPublishPressed?.Invoke(sceneToPublish);
    }

    public void ValidatePublicationInfo(string sceneName)
    {
        isValidated = sceneName.Length > 0;
        landPublisherView.SetSceneNameValidationActive(!isValidated);
        landPublisherView.SetPublishButtonActive(isValidated);
    }

    public void SetDefaultPublicationInfo()
    {
        landPublisherView.SetSceneName(DEFAULT_SCENE_NAME);
        landPublisherView.SetSceneDescription(DEFAULT_SCENE_DESC);
    }

    public void SetCustomPublicationInfo(IBuilderScene scene)
    {
        landPublisherView.SetSceneName(scene.manifest.project.title);
        landPublisherView.SetSceneDescription(scene.manifest.project.description);
        SetPublicationScreenshot(scene.sceneScreenshotTexture);
    }

    public void SetPublicationScreenshot(Texture2D sceneScreenshot) { landPublisherView.SetPublicationScreenshot(sceneScreenshot); }
}