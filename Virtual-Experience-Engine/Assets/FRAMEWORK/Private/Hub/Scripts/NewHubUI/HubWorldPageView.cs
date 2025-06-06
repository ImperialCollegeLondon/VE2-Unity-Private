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
    [SerializeField] private Button _enterInstanceCodeButton;
    [SerializeField] private VerticalLayoutGroup _instancesVerticalGroup;
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

    [SerializeField] private GameObject instanceButtonPrefab;

    public event Action OnBackClicked;

    public event Action OnAutoSelectInstanceClicked;
    public event Action OnEnterInstanceCodeClicked;

    //TODO - we still need to figure out what we're doing with version numbers here. 
    public event Action<InstanceCode> OnInstanceCodeSelected;

    public event Action OnDownloadWorldClicked;
    public event Action OnCancelDownloadClicked;
    public event Action OnInstallWorldClicked;
    public event Action OnEnterWorldClicked;

    private Dictionary<PlatformInstanceInfo, HubInstanceView> _instanceViews = new();

    //private HubWorldDetails _worldDetails;

    //TODO - also pass in some world state to tell us if we need to download, or install?
    //We probably shouldn't be using the WorldDetails object... maybe LocalWorldDetails, that includes the local state of the world?
    //TODO - also need to pass in instance details
    //TODO - also also need to pass in current play mode (2D/VR) and whether we can switch modes
    public void SetupView(HubWorldDetails worldDetails, List<PlatformInstanceInfo> instances)
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
        _needToSelectInstancePanel.SetActive(false);
        _enterWorldButton.gameObject.SetActive(false);

        List<PlatformInstanceInfo> instancesToRemove = _instanceViews.Keys.ToList();

        foreach (PlatformInstanceInfo instanceToRemove in instancesToRemove)
            RemoveInstanceView(instanceToRemove);

        UpdateInstances(instances);
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

    /// <summary>
    /// Kind of the second half of Setup.
    /// </summary>
    public void ShowSelectedVersion(int version, bool needsDownload, bool downloadedButNotInstalled, bool IsExperimental, bool isInstanceSelected)
    {
        _confirmingVersionsPanel.SetActive(false);
        _downloadWorldButton.gameObject.SetActive(needsDownload);
        _installWorldButton.gameObject.SetActive(downloadedButNotInstalled);
        _needToSelectInstancePanel.SetActive(!needsDownload && !downloadedButNotInstalled &&!isInstanceSelected);
        _enterWorldButton.gameObject.SetActive(!needsDownload && !downloadedButNotInstalled && isInstanceSelected);

        _selectedVersionNumber.text = $"V{version} {(IsExperimental ? "<i>(Experimental)</i>" : "")}";
    }

    public void ShowStartDownloadWorldButton()
    {
        _downloadWorldButton.gameObject.SetActive(true);
        _downloadingWorldPanel.SetActive(false);
        _installWorldButton.gameObject.SetActive(false);
        _needToSelectInstancePanel.SetActive(false);
        _enterWorldButton.gameObject.SetActive(false);
    }

    public void ShowDownloadingWorldPanel()
    {
        _downloadingWorldProgressBar.SetValue(0);

        _downloadWorldButton.gameObject.SetActive(false);
        _downloadingWorldPanel.SetActive(true);
        _installWorldButton.gameObject.SetActive(false);
        _needToSelectInstancePanel.SetActive(false);
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
        _needToSelectInstancePanel.SetActive(false);
        _enterWorldButton.gameObject.SetActive(false);
    }

    public void ShowNeedToSelectInstancePanel()
    {
        _downloadWorldButton.gameObject.SetActive(false);
        _downloadingWorldPanel.SetActive(false);
        _installWorldButton.gameObject.SetActive(false);
        _needToSelectInstancePanel.SetActive(true);
        _enterWorldButton.gameObject.SetActive(false);
    }

    public void ShowEnterWorldButton()
    {
        _downloadWorldButton.gameObject.SetActive(false);
        _downloadingWorldPanel.SetActive(false);
        _installWorldButton.gameObject.SetActive(false);
        _enterWorldButton.gameObject.SetActive(true);
    }

    public void SetSelectedInstance(InstanceCode selectedInstanceCode)
    {
        Debug.Log($"Setting selected instance code on view: {selectedInstanceCode} existing codes...");
        _noInstancesToShowPanel.SetActive(false);

        _selectedInstanceCodeText.text = "Instance #" + selectedInstanceCode.InstanceSuffix;

        bool foundInstance = false;
        foreach (KeyValuePair<PlatformInstanceInfo, HubInstanceView> kvp in _instanceViews)
        {
            if (selectedInstanceCode.Equals(kvp.Key.InstanceCode))
            {
                kvp.Value.IsSelected = true;
                foundInstance = true;
            }
            else
            {
                if (kvp.Key.ClientInfos.Count == 0)
                {
                    RemoveInstanceView(kvp.Key);
                }
                else
                {
                    kvp.Value.IsSelected = false;
                }
            }
        }

        if (!foundInstance)
        {
            // If the instance view is not found, we need to create a new one
            PlatformInstanceInfo instanceInfo = new(selectedInstanceCode, new Dictionary<ushort, PlatformClientInfo>());
            AddInstanceView(instanceInfo);
            _instanceViews[instanceInfo].IsSelected = true;
        }
    }

    public void UpdateInstances(List<PlatformInstanceInfo> instancesFromServer)
    {
        //Remove old instances 
        List<PlatformInstanceInfo> instancesToRemove = new();
        foreach (KeyValuePair<PlatformInstanceInfo, HubInstanceView> kvp in _instanceViews)
        {
            if (!instancesFromServer.Contains(kvp.Key) && !kvp.Value.IsSelected)
                instancesToRemove.Add(kvp.Key);
        }
        foreach (PlatformInstanceInfo instanceInfo in instancesToRemove)
            RemoveInstanceView(instanceInfo);

        //Add new instances
        foreach (PlatformInstanceInfo instanceInfo in instancesFromServer)
        {
            if (!_instanceViews.ContainsKey(instanceInfo))
            {
                AddInstanceView(instanceInfo);
            }
            else
            {
                _instanceViews[instanceInfo].UpdateInstanceInfo(instanceInfo);
            }
        }

        _noInstancesToShowPanel.SetActive(_instanceViews.Count == 0);
    }

    private void AddInstanceView(PlatformInstanceInfo instanceInfo)
    {
        GameObject instanceButtonObject = Instantiate(instanceButtonPrefab, _instancesVerticalGroup.transform);
        HubInstanceView instanceView = instanceButtonObject.GetComponent<HubInstanceView>();
        instanceView.SetupView(instanceInfo);
        instanceView.OnSelectInstance += (instanceCode) => HandleInstanceButtonClicked(instanceInfo);
        _instanceViews.Add(instanceInfo, instanceView);
    }

    private void RemoveInstanceView(PlatformInstanceInfo instanceInfo)
    {
        if (_instanceViews.TryGetValue(instanceInfo, out HubInstanceView instanceView))
        {
            instanceView.OnSelectInstance -= (instanceCode) => OnEnterWorldClicked?.Invoke();
            Destroy(instanceView.gameObject);
            _instanceViews.Remove(instanceInfo);
        }
    }

    private void HandleInstanceButtonClicked(PlatformInstanceInfo instanceInfo)
    {
        OnInstanceCodeSelected?.Invoke(instanceInfo.InstanceCode);
    }

}
