using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VE2.Common.API;
using VE2.Common.Shared;
using static VE2.NonCore.Platform.API.PlatformPublicSerializables;

internal class HubWorldPageView : MonoBehaviour
{
    [SerializeField] private Button _backButton;
    [SerializeField] private Image _worldBanner;
    [SerializeField] private TMP_Text _worldTitle;
    [SerializeField] private TMP_Text _worldSubtitle;
    [SerializeField] private TMP_Text _worldExtraInfo;
    [SerializeField] private TMP_Text _selectedVersionNumber;

    [SerializeField] private Button _autoSelectInstanceButton;
    [SerializeField] private Button _enterInstanceCodeButton;
    [SerializeField] private VerticalLayoutGroup _instancesVerticalGroup;

    [SerializeField] private Image _2dEnabledIcon;
    [SerializeField] private Image _2dDisabledIcon;
    [SerializeField] private Image _vrEnabledIcon;
    [SerializeField] private Image _vrDisabledIcon;
    [SerializeField] private Button _switchTo2dButton;
    [SerializeField] private Button _switchToVrButton;

    [SerializeField] private GameObject _confirmingVersionsPanel;
    [SerializeField] private Button _downloadWorldButton;
    [SerializeField] private GameObject _downloadingWorldPanel;
    [SerializeField] private Button _cancelDownloadButton;
    [SerializeField] private RadialProgressBar _downloadingWorldProgressBar;
    [SerializeField] private Button _installWorldButton;
    [SerializeField] private Button _enterWorldButton;

    //TODO - admin button

    [SerializeField] private GameObject instanceButtonPrefab;

    public event Action OnBackClicked;

    public event Action OnAutoSelectInstanceClicked;
    public event Action OnEnterInstanceCodeClicked;

    public event Action OnDownloadWorldClicked;
    public event Action OnCancelDownloadClicked;
    public event Action OnInstallWorldClicked;
    public event Action OnEnterWorldClicked;

    //private HubWorldDetails _worldDetails;

    //TODO - also pass in some world state to tell us if we need to download, or install?
    //We probably shouldn't be using the WorldDetails object... maybe LocalWorldDetails, that includes the local state of the world?
    //TODO - also need to pass in instance details
    //TODO - also also need to pass in current play mode (2D/VR) and whether we can switch modes
    public void SetupView(HubWorldDetails worldDetails)
    {
        // _worldDetails = worldDetails;

        _backButton.onClick.AddListener(() => OnBackClicked?.Invoke());
        _worldTitle.text = worldDetails.Name;

        _autoSelectInstanceButton.onClick.AddListener(() => OnAutoSelectInstanceClicked?.Invoke());
        _enterInstanceCodeButton.onClick.AddListener(() => OnEnterInstanceCodeClicked?.Invoke());

        //TODO=======
        //_worldBanner.sprite = _worldDetails.Banner;
        //_worldSubtitle.text = _worldDetails.SubTitle; 
        //_worldExtraInfo.text = $"Created by: {_worldDetails.CreatorName}";
        _2dEnabledIcon.gameObject.SetActive(true);
        _2dDisabledIcon.gameObject.SetActive(false);
        _vrEnabledIcon.gameObject.SetActive(true);
        _vrDisabledIcon.gameObject.SetActive(false);

        bool isVrMode = VE2API.Player.IsVRMode;
        _switchTo2dButton.gameObject.SetActive(isVrMode);
        _switchToVrButton.gameObject.SetActive(!isVrMode);
        //===========

        _downloadWorldButton.onClick.AddListener(() => OnDownloadWorldClicked?.Invoke());
        _cancelDownloadButton.onClick.AddListener(() => OnCancelDownloadClicked?.Invoke());
        _installWorldButton.onClick.AddListener(() => OnInstallWorldClicked?.Invoke());
        _enterWorldButton.onClick.AddListener(() => OnEnterWorldClicked?.Invoke());

        _confirmingVersionsPanel.SetActive(false);
        _downloadWorldButton.gameObject.SetActive(false);
        _downloadingWorldPanel.SetActive(false);
        _installWorldButton.gameObject.SetActive(false);
        _enterWorldButton.gameObject.SetActive(false);
    }

    public void ShowAvailableVersions(List<int> versions)
    {
        Debug.Log($"Showing available versions");
        foreach (int version in versions)
        {
            Debug.Log($"Available version: {version}");
        }
    }

    public void ShowSelectedVersion(int version, bool needsDownload, bool downloadedButNotInstalled, bool IsExperimental)
    {
        Debug.Log($"Selected version: {version}");

        _confirmingVersionsPanel.SetActive(false);
        _downloadWorldButton.gameObject.SetActive(needsDownload);
        _installWorldButton.gameObject.SetActive(downloadedButNotInstalled);
        _enterWorldButton.gameObject.SetActive(!needsDownload && !downloadedButNotInstalled);

        _selectedVersionNumber.text = $"V{version} {(IsExperimental ? "<i>(Experimental)</i>" : "")}";
    }

    public void ShowStartDownloadWorldButton()
    {
        _downloadWorldButton.gameObject.SetActive(true);
        _downloadingWorldPanel.SetActive(false);
        _installWorldButton.gameObject.SetActive(false);
        _enterWorldButton.gameObject.SetActive(false);
    }

    public void ShowDownloadingWorldPanel()
    {
        _downloadingWorldProgressBar.SetValue(0);

        _downloadWorldButton.gameObject.SetActive(false);
        _downloadingWorldPanel.SetActive(true);
        _installWorldButton.gameObject.SetActive(false);
        _enterWorldButton.gameObject.SetActive(false);
    }

    public void UpdateDownloadingWorldProgress(float progress)
    {
        _downloadingWorldProgressBar.SetValue(progress);
    }

    public void ShowInstallWorldButton()
    {
        _downloadWorldButton.gameObject.SetActive(false);
        _downloadingWorldPanel.SetActive(false);
        _installWorldButton.gameObject.SetActive(true);
        _enterWorldButton.gameObject.SetActive(false);
    }

    public void ShowEnterWorldButton()
    {
        _downloadWorldButton.gameObject.SetActive(false);
        _downloadingWorldPanel.SetActive(false);
        _installWorldButton.gameObject.SetActive(false);
        _enterWorldButton.gameObject.SetActive(true);
    }

    /*
        How do we know which version we're asking to download/install/enter?
        We need to show available versions

        When we first open the world view page, we need to search for all versions of that world
    */
}
