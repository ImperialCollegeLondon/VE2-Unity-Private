using System;
using System.Collections.Generic;
using System.Linq;
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
    //[SerializeField] private Button _enterInstanceCodeButton;
    //[SerializeField] private V_InputFieldHandler _enterInstanceCodeInputField; //No interface, so this is wired up in the inspector
    [SerializeField] public VerticalLayoutGroup InstancesVerticalGroup;
    [SerializeField] private GameObject _noInstancesToShowPanel;

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
    [SerializeField] private GameObject _needToSelectInstancePanel;
    [SerializeField] private TMP_Text _selectedInstanceCodeText;

    //TODO - admin button

    [SerializeField] public GameObject InstanceButtonPrefab;

    public HubWorldPageUIState CurrentUIState;

    public event Action OnBackClicked;

    public event Action OnAutoSelectInstanceClicked;
    //public event Action OnEnterInstanceCodeClicked;
    public event Action<string> OnInstanceCodeManuallyEntered;

    //TODO - we still need to figure out what we're doing with version numbers here. 
    public event Action<InstanceCode> OnInstanceCodeSelected;

    public event Action OnDownloadWorldClicked;
    public event Action OnCancelDownloadClicked;
    public event Action OnInstallWorldClicked;
    public event Action OnEnterWorldClicked;

    private void Awake()
    {
        _backButton.onClick.AddListener(() => OnBackClicked?.Invoke());

        _autoSelectInstanceButton.onClick.AddListener(() => OnAutoSelectInstanceClicked?.Invoke());
        //_enterInstanceCodeButton.onClick.AddListener(() => OnEnterInstanceCodeClicked?.Invoke());
        //_enterInstanceCodeInputField.onEndEdit.AddListener((value) => OnInstanceCodeManuallyEntered?.Invoke(value));

        _downloadWorldButton.onClick.AddListener(() => OnDownloadWorldClicked?.Invoke());
        _cancelDownloadButton.onClick.AddListener(() => OnCancelDownloadClicked?.Invoke());
        _installWorldButton.onClick.AddListener(() => OnInstallWorldClicked?.Invoke());
        _enterWorldButton.onClick.AddListener(() => OnEnterWorldClicked?.Invoke());
    }

    //private HubWorldDetails _worldDetails;

    //TODO - also pass in some world state to tell us if we need to download, or install?
    //We probably shouldn't be using the WorldDetails object... maybe LocalWorldDetails, that includes the local state of the world?
    //TODO - also need to pass in instance details
    //TODO - also also need to pass in current play mode (2D/VR) and whether we can switch modes
    public void SetupView(HubWorldDetails worldDetails)
    {
        // _worldDetails = worldDetails;
        _worldTitle.text = worldDetails.Name;


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
    }

    public void HandleInstanceCodeManuallyEntered(string instanceCode)
    {
        OnInstanceCodeManuallyEntered?.Invoke(instanceCode);
    }

    public void ShowAvailableVersions(List<int> versions)
    {
        //TODO show versions on UI 
        // Debug.Log($"Showing available versions");
        // foreach (int version in versions)
        // {
        //     Debug.Log($"Available version: {version}");
        // }
    }

    public void UpdateUIState(HubWorldPageUIState newState)
    {
        if (CurrentUIState == newState)
            return; // No change, do nothing

        CurrentUIState = newState;

        _confirmingVersionsPanel.SetActive(newState == HubWorldPageUIState.Loading);
        _downloadWorldButton.gameObject.SetActive(newState == HubWorldPageUIState.NeedToDownloadWorld);
        _downloadingWorldPanel.SetActive(newState == HubWorldPageUIState.DownloadingWorld);
        _installWorldButton.gameObject.SetActive(newState == HubWorldPageUIState.NeedToInstallWorld);
        _needToSelectInstancePanel.SetActive(newState == HubWorldPageUIState.NeedToSelectInstance);
        _enterWorldButton.gameObject.SetActive(newState == HubWorldPageUIState.ReadyToEnterWorld);

        if (newState == HubWorldPageUIState.DownloadingWorld)
        {
            _downloadingWorldProgressBar.SetValue(0);
        }
    }

    public void ShowSelectedVersion(int version, bool IsExperimental) => _selectedVersionNumber.text = $"V{version} {(IsExperimental ? "<i>(Experimental)</i>" : "")}";

    public void SetNoInstancesToShow(bool noInstances)
    {
        _noInstancesToShowPanel.SetActive(noInstances);
    }

    public void SetSelectedInstanceCode(InstanceCode instanceCode) => _selectedInstanceCodeText.text = "Instance #" + instanceCode.InstanceSuffix;

    public void UpdateDownloadingWorldProgress(float progress) => _downloadingWorldProgressBar.SetValue(progress);
}

public enum HubWorldPageUIState
{
    Loading,
    NeedToDownloadWorld,
    DownloadingWorld,
    NeedToInstallWorld,
    NeedToSelectInstance,
    ReadyToEnterWorld
}
