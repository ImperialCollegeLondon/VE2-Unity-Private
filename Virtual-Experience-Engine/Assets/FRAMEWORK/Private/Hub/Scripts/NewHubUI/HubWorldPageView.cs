using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VE2.Common.API;
using static VE2.NonCore.Platform.API.PlatformPublicSerializables;

internal class HubWorldPageView : MonoBehaviour
{
    [SerializeField] private Button _backButton;
    [SerializeField] private Image _worldBanner;
    [SerializeField] private TMP_Text _worldTitle;
    [SerializeField] private TMP_Text _worldSubtitle;
    [SerializeField] private TMP_Text _worldExtraInfo;

    [SerializeField] private Button _autoSelectInstanceButton;
    [SerializeField] private Button _enterInstanceCodeButton;
    [SerializeField] private VerticalLayoutGroup _instancesVerticalGroup;

    [SerializeField] private Image _2dEnabledIcon;
    [SerializeField] private Image _2dDisabledIcon;
    [SerializeField] private Image _vrEnabledIcon;
    [SerializeField] private Image _vrDisabledIcon;
    [SerializeField] private Button _switchTo2dButton;
    [SerializeField] private Button _switchToVrButton;

    [SerializeField] private Button _downloadWorldButton;
    [SerializeField] private Button _installWorldButton;
    [SerializeField] private Button _enterWorldButton;

    //TODO - admin button

    [SerializeField] private GameObject instanceButtonPrefab;

    public event Action OnBackClicked;

    public event Action<WorldDetails> OnAutoSelectInstanceClicked;
    public event Action OnEnterInstanceCodeClicked;

    public event Action<WorldDetails> OnDownloadWorldClicked;
    public event Action<WorldDetails> OnInstallWorldClicked;
    public event Action<WorldDetails> OnEnterWorldClicked;

    //TODO - also pass in some world state to tell us if we need to download, or install?
    //We probably shouldn't be using the WorldDetails object... maybe LocalWorldDetails, that includes the local state of the world?
    //TODO - also need to pass in instance details
    //TODO - also also need to pass in current play mode (2D/VR) and whether we can switch modes
    public void SetupView(WorldDetails worldDetails)
    {
        _backButton.onClick.AddListener(() => OnBackClicked?.Invoke());
        _worldTitle.text = worldDetails.Name;

        _autoSelectInstanceButton.onClick.AddListener(() => OnAutoSelectInstanceClicked?.Invoke(worldDetails));
        _enterInstanceCodeButton.onClick.AddListener(() => OnEnterInstanceCodeClicked?.Invoke());

        //TODO=======
        //_worldBanner.sprite = worldDetails.Banner;
        //_worldSubtitle.text = worldDetails.SubTitle; 
        //_worldExtraInfo.text = $"Created by: {worldDetails.CreatorName}";
        _2dEnabledIcon.gameObject.SetActive(true);
        _2dDisabledIcon.gameObject.SetActive(false);
        _vrEnabledIcon.gameObject.SetActive(true);
        _vrDisabledIcon.gameObject.SetActive(false);

        bool isVrMode = VE2API.Player.IsVRMode;
        _switchTo2dButton.gameObject.SetActive(isVrMode);
        _switchToVrButton.gameObject.SetActive(!isVrMode);
        //===========

    }
}
