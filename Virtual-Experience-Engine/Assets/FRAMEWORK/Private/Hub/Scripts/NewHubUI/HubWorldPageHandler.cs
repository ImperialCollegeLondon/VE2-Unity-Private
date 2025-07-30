using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VE2.Common.Shared;
using VE2.NonCore.FileSystem.API;
using VE2.NonCore.Platform.API;
using static VE2.NonCore.Platform.API.PlatformPublicSerializables;

internal class HubWorldPageHandler
{
    private int _selectedWorldVersion = -1;
    private InstanceCode _selectedInstanceCode = null;

    private readonly HubWorldPageView _hubWorldPageView;
    private readonly Dictionary<string, HubInstanceDisplayHandler> _instanceDisplayHandlers = new();
    private readonly HubWorldDetails _worldDetails;
    private readonly IPlatformServiceInternal _platformService;
    private readonly IFileSystemInternal _fileSystem;

    public HubWorldPageHandler(HubWorldPageView hubWorldPageView, HubWorldDetails worldDetails, IPlatformServiceInternal platformService, IFileSystemInternal fileSystem)
    {
        _hubWorldPageView = hubWorldPageView;
        _worldDetails = worldDetails;
        _platformService = platformService;
        _fileSystem = fileSystem;

        _platformService.OnInstanceInfosChanged += HandleInstanceInfosChanged;

        //We don't want to update the after we've already left the instance, otherwise we'll see a new instance pop up with our avatar 
        //The UI is currently setup to show OTHER players in instances, not the local player themselves
        _platformService.OnLeavingInstance += HandleLeaveInstance;

        //_hubWorldPageView.OnBackClicked += HandleBackClicked;
        _hubWorldPageView.OnDownloadWorldClicked += HandleStartDownloadClicked;
        _hubWorldPageView.OnCancelDownloadClicked += HandleCancelDownloadClicked;
        _hubWorldPageView.OnInstallWorldClicked += HandleInstallWorldClicked;
        _hubWorldPageView.OnInstanceCodeSelected += HandleInstanceSelected;
        _hubWorldPageView.OnAutoSelectInstanceClicked += HandleChooseInstanceForMeSelected;
        _hubWorldPageView.OnEnterWorldClicked += HandleEnterWorldClicked;

        _hubWorldPageView.SetupView(worldDetails);

        RefreshInstanceDisplays();

        //First, we have to search for the versions of that world
        IRemoteFolderSearchInfo searchInfo = _fileSystem.GetRemoteFoldersAtPath($"{worldDetails.Name}");
        searchInfo.OnSearchComplete += HandleWorldVersionSearchComplete;

        Debug.LogError("HubWorldPageHandler set up for world: " + _worldDetails.Name);
    }

    private void HandleWorldVersionSearchComplete(IRemoteFolderSearchInfo searchInfo)
    {
        if (searchInfo.CompletionCode.ToUpper().Contains("ERROR"))
        {
            Debug.LogError("Failed to search for world versions: " + searchInfo.CompletionCode);
            return;
        }

        Debug.Log("World version search complete: " + searchInfo.FoldersFound.Count);
        foreach (string file in searchInfo.FoldersFound)
        {
            Debug.Log("Found version: " + file);
        }

        // Version folder names are strings in the format 000, 001, 002, etc. Convert these to integers and populate the list
        _worldDetails.VersionsAvailableRemotely = searchInfo.FoldersFound
            .Select(s => int.TryParse(s, out var v) ? (int?)v : null)
            .Where(v => v.HasValue)
            .Select(v => v.Value)
            .ToList();

        //TODO: This returns DevBlue/002, while the above just returns 002

        _worldDetails.VersionsAvailableLocally = _fileSystem.GetLocalFoldersAtPath($"{_worldDetails.Name}")
            .Select(s => int.TryParse(s, out var v) ? (int?)v : null)
            .Where(v => v.HasValue)
            .Select(v => v.Value)
            .ToList();

        List<string> localWorlds = _fileSystem.GetLocalFoldersAtPath($"{_worldDetails.Name}");

        int targetVersion;

        if (_worldDetails.IsExperimental)
        {
            targetVersion = _worldDetails.VersionsAvailableRemotely.Max();
        }
        else
        {
            if (_worldDetails.VersionsAvailableRemotely.Contains(_worldDetails.LiveVersionNumber))
            {
                targetVersion = _worldDetails.LiveVersionNumber;
            }
            else
            {
                Debug.LogError($"Live version {_worldDetails.LiveVersionNumber} not found for world {_worldDetails.Name}");
                targetVersion = -1; //TODO - show some error on UI  
            }
        }

        //TODO: don't show non-live versions if we haven't ticked "show experimental worlds" on the UI somewhere
        _hubWorldPageView.ShowAvailableVersions(_worldDetails.VersionsAvailableRemotely);

        bool isVersionExperimental = targetVersion != _worldDetails.LiveVersionNumber;
        _hubWorldPageView.ShowSelectedVersion(targetVersion, isVersionExperimental);
        _selectedWorldVersion = targetVersion;

        RefreshWorldUIState();
    }

    //TODO - maybe move to a different module
    //=======================================================================================================
    #region INSTANCES 

    private void HandleInstanceSelected(InstanceCode instanceCode)
    {
        _selectedInstanceCode = instanceCode;
        _hubWorldPageView.SetSelectedInstanceCode(instanceCode);
        RefreshInstanceDisplays();
        RefreshWorldUIState();
    }

    private void HandleChooseInstanceForMeSelected()
    {
        InstanceCode instanceCode = new(_worldDetails.Name, "00", (ushort)_selectedWorldVersion);
        HandleInstanceSelected(instanceCode);
    }

    private void HandleInstanceInfosChanged(Dictionary<string, PlatformInstanceInfo> instanceInfos) => RefreshInstanceDisplays();

    private void RefreshInstanceDisplays()
    {
        List<InstanceCode> instanceCodesFromServer = _platformService.GetInstanceCodesForWorldName(_worldDetails.Name);
        List<string> instancesFromServer = instanceCodesFromServer.Select(ic => ic.ToString()).ToList();

        //Remove old instances=============================================================================== 
        List<string> instancesToRemove = new();
        foreach (KeyValuePair<string, HubInstanceDisplayHandler> kvp in _instanceDisplayHandlers)
        {
            if (!instancesFromServer.Contains(kvp.Key) && !kvp.Key.Equals(_selectedInstanceCode))
                instancesToRemove.Add(kvp.Key);
        }

        foreach (string instanceCode in instancesToRemove)
            RemoveInstanceDisplay(instanceCode);

        //Add/update instances=============================================================================== 
        foreach (string instanceCode in instancesFromServer)
        {
            PlatformInstanceInfo instanceInfo = _platformService.InstanceInfos[instanceCode.ToString()];
            bool isSelected = _selectedInstanceCode != null && instanceCode.Equals(_selectedInstanceCode.ToString());

            if (!_instanceDisplayHandlers.ContainsKey(instanceCode))
                AddInstanceDisplay(instanceInfo);
            else
                _instanceDisplayHandlers[instanceCode.ToString()].UpdateDisplay(instanceInfo, isSelected);
        }

        if (_selectedInstanceCode != null)
        {
            if (!_instanceDisplayHandlers.ContainsKey(_selectedInstanceCode.ToString()))
            {
                //If the selected instance is not in the list, we need to add it
                PlatformInstanceInfo instanceInfo = new(_selectedInstanceCode, new Dictionary<ushort, PlatformClientInfo>());
                instanceInfo.ClientInfos.Add(_platformService.LocalClientID, new PlatformClientInfo
                {
                    ClientID = _platformService.LocalClientID,
                    PlayerPresentationConfig = _platformService.LocalPlayerPresentationConfig
                });

                AddInstanceDisplay(instanceInfo);
            }
            else if (!instancesFromServer.Contains(_selectedInstanceCode.ToString()))
            {
                _instanceDisplayHandlers[_selectedInstanceCode.ToString()].UpdateDisplay(_platformService.InstanceInfos[_selectedInstanceCode.ToString()], true);
            }
            //Otherwise, it will hae been updated already
        }

        _hubWorldPageView.SetNoInstancesToShow(_instanceDisplayHandlers.Count == 0);
    }

    private void AddInstanceDisplay(PlatformInstanceInfo instanceInfo)
    {
        HubInstanceDisplayHandler newInstanceDisplayHandler = new(instanceInfo, instanceInfo.InstanceCode.Equals(_selectedInstanceCode), _hubWorldPageView.InstanceButtonPrefab, _hubWorldPageView.InstancesVerticalGroup);
        _instanceDisplayHandlers.Add(instanceInfo.InstanceCode.ToString(), newInstanceDisplayHandler);
        newInstanceDisplayHandler.OnInstanceButtonClicked += HandleInstanceSelected;
    }

    private void RemoveInstanceDisplay(string instanceCode)
    {
        _instanceDisplayHandlers[instanceCode].Destroy();
        _instanceDisplayHandlers[instanceCode].OnInstanceButtonClicked -= HandleInstanceSelected;
        _instanceDisplayHandlers.Remove(instanceCode);
    }

    private void HandleLeaveInstance() => _platformService.OnInstanceInfosChanged -= HandleInstanceInfosChanged;

    #endregion //INSTANCES

    //=======================================================================================================
    #region WORLD FILES DOWNLOAD 

    //TODO - maybe this can go into a different module

    private class HubFileDownloadInfo
    {
        public string FileNameAndPath;
        public ulong FileSize;
        public ulong AmountDownloaded;

        public HubFileDownloadInfo(string fileNameAndPath, ulong fileSize, ulong amountDownloaded)
        {
            FileNameAndPath = fileNameAndPath;
            FileSize = fileSize;
            AmountDownloaded = amountDownloaded;
        }

        public void MarkComplete() { AmountDownloaded = FileSize; }
    }
    
    private List<HubFileDownloadInfo> _filesToDownload;
    private int _curentFileDownloadIndex = -1;
    private IRemoteFileTaskInfo _currentDownloadTask;

    private void HandleStartDownloadClicked()
    {
        Debug.Log("Download world clicked: " + _worldDetails.Name);
        _hubWorldPageView.UpdateUIState(HubWorldPageUIState.DownloadingWorld); //TODO, could be handled in UpdateUIState

        _hubWorldPageView.UpdateDownloadingWorldProgress(0);

        IRemoteFileSearchInfo searchInfo = _fileSystem.GetRemoteFilesAtPath($"{_worldDetails.Name}/{_selectedWorldVersion.ToString("D3")}");
        searchInfo.OnSearchComplete += HandleCompletedSearchForWorldFilesToDownload;
    }

    private void HandleCompletedSearchForWorldFilesToDownload(IRemoteFileSearchInfo searchInfo)
    {
        if (searchInfo.FilesFound.Count == 0)
        {
            Debug.LogError("No files found for world: " + _worldDetails.Name);
            return;
        }

        Debug.Log("Found " + searchInfo.FilesFound.Count + " files to download for world: " + _worldDetails.Name);

        //Check for correct number of files TODO
        _filesToDownload = searchInfo.FilesFound
            .Select(f => new HubFileDownloadInfo(f.Key, f.Value.Size, (ulong)0))
            .ToList();
        _curentFileDownloadIndex = 0;

        searchInfo.OnSearchComplete -= HandleCompletedSearchForWorldFilesToDownload;

        BeginDownloadNextFile();
    }

    private void BeginDownloadNextFile()
    {
        string fileNameAndPath = _filesToDownload[_curentFileDownloadIndex].FileNameAndPath;
        Debug.Log("Downloading file: " + fileNameAndPath);

        _currentDownloadTask = _fileSystem.DownloadFile($"{fileNameAndPath}");
        _currentDownloadTask.OnStatusChanged += HandleWorldFileDownloadStatusChanged;
    }

    private void HandleWorldFileDownloadStatusChanged(RemoteFileTaskStatus status)
    {
        if (status == RemoteFileTaskStatus.Failed)
        {
            Debug.LogError($"Failed to download file: {_currentDownloadTask.NameAndPath}");
            // TODO show some error on UI
            _curentFileDownloadIndex = -1;
            return;
        }

        if (status == RemoteFileTaskStatus.Cancelled)
        {
            Debug.Log($"Cancelled download file: {_currentDownloadTask.NameAndPath}");
            _curentFileDownloadIndex = -1;
            return;
        }

        if (status == RemoteFileTaskStatus.Succeeded)
        {
            _currentDownloadTask.OnStatusChanged -= HandleWorldFileDownloadStatusChanged;

            _filesToDownload[_curentFileDownloadIndex].MarkComplete();

            _curentFileDownloadIndex++;
            if (_curentFileDownloadIndex < _filesToDownload.Count)
            {
                Debug.Log($"File downloaded successfully: {_currentDownloadTask.NameAndPath}. Starting next download.");
                BeginDownloadNextFile();
            }
            else
            {
                Debug.Log("All files downloaded successfully");
                HandleAllWorldFilesDownloaded();
            }
        }
    }

    private void HandleCancelDownloadClicked()
    {
        _currentDownloadTask?.CancelRemoteFileTask();
        _hubWorldPageView.UpdateUIState(HubWorldPageUIState.NeedToDownloadWorld);
    }

    private void HandleAllWorldFilesDownloaded()
    {
        _curentFileDownloadIndex = -1;

        RefreshWorldUIState();
    }

    private void HandleInstallWorldClicked()
    {
        string versionString = _selectedWorldVersion.ToString("D3");
        string filePath = $"{_fileSystem.LocalAbsoluteWorkingPath}/{_worldDetails.Name}/{versionString}/{_worldDetails.Name}.apk";
        Debug.Log("Installing package " + _worldDetails.AndroidPackageName + " at path " + filePath);

        using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        using (AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
        using (AndroidJavaObject fileProvider = new AndroidJavaClass("androidx.core.content.FileProvider"))
        {
            string authority = "com.ImperialCollegeLondon.VirtualExperienceEngine.fileprovider";
            using (AndroidJavaObject file = new AndroidJavaObject("java.io.File", filePath))
            using (AndroidJavaObject uri = fileProvider.CallStatic<AndroidJavaObject>("getUriForFile", currentActivity, authority, file))
            using (AndroidJavaObject intent = new AndroidJavaObject("android.content.Intent", "android.intent.action.VIEW"))
            {
                intent.Call<AndroidJavaObject>("setDataAndType", uri, "application/vnd.android.package-archive");
                intent.Call<AndroidJavaObject>("addFlags", 268435456); // FLAG_ACTIVITY_NEW_TASK
                intent.Call<AndroidJavaObject>("addFlags", 1); // FLAG_GRANT_READ_URI_PERMISSION
                intent.Call<AndroidJavaObject>("addFlags", 1073741824); // FLAG_ACTIVITY_NO_HISTORY
                intent.Call<AndroidJavaObject>("addFlags", 8388608); // FLAG_ACTIVITY_EXCLUDE_FROM_RECENTS

                currentActivity.Call("startActivity", intent);
            }
        }

        Debug.Log("Current installing package name set to: " + _worldDetails.AndroidPackageName);
    }

    public void HandleUpdate()
    {
        if (_curentFileDownloadIndex != -1)
        {
            _filesToDownload[_curentFileDownloadIndex].AmountDownloaded = (ulong)(_filesToDownload[_curentFileDownloadIndex].FileSize * _currentDownloadTask.Progress);

            ulong totalDownloaded = 0;
            foreach (HubFileDownloadInfo file in _filesToDownload)
                totalDownloaded += file.AmountDownloaded;

            ulong totalSize = 0;
            foreach (HubFileDownloadInfo file in _filesToDownload)
                totalSize += file.FileSize;

            int progressPercent = Mathf.FloorToInt(totalDownloaded * 100 / totalSize);
            _hubWorldPageView.UpdateDownloadingWorldProgress(progressPercent);
        }

        if (_hubWorldPageView.CurrentUIState == HubWorldPageUIState.NeedToInstallWorld)
        {
            Debug.Log("Checking if APK is installed: " + _worldDetails.AndroidPackageName);
            RefreshWorldUIState(); //Will check if the APK is installed
        }
    }

    #endregion //WORLD FILES DOWNLOAD AND INSTALL

    private void RefreshWorldUIState()
    {
        //TODO: Should also handle whether we're downloading

        HubWorldPageUIState newUIState;
        if (!_worldDetails.IsVersionDownloaded(_selectedWorldVersion))
            newUIState = HubWorldPageUIState.NeedToDownloadWorld;
        else if (!_worldDetails.IsVersionInstalled(_selectedWorldVersion))
            newUIState = HubWorldPageUIState.NeedToInstallWorld;
        else if (_selectedInstanceCode == null)
            newUIState = HubWorldPageUIState.NeedToSelectInstance;
        else
            newUIState = HubWorldPageUIState.ReadyToEnterWorld;

        _hubWorldPageView.UpdateUIState(newUIState);
    }

    private void HandleEnterWorldClicked()
    {
        Debug.Log("Enter world clicked: " + _worldDetails.Name + " Version: " + _selectedWorldVersion);

        if (_selectedWorldVersion == -1)
        {
            Debug.LogError("No version selected for world: " + _worldDetails.Name);
            return;
        }

        _platformService.RequestInstanceAllocation(new InstanceCode(_worldDetails.Name, "00", (ushort)_selectedWorldVersion));
    }

    public void TearDown()
    {
        _currentDownloadTask?.CancelRemoteFileTask();

        //.ToList() is needed to avoid modifying the dictionary while iterating
        foreach (string instanceCode in _instanceDisplayHandlers.Keys.ToList())
            RemoveInstanceDisplay(instanceCode);

        _platformService.OnInstanceInfosChanged -= HandleInstanceInfosChanged;
        _platformService.OnLeavingInstance -= HandleLeaveInstance;

        _hubWorldPageView.OnDownloadWorldClicked -= HandleStartDownloadClicked;
        _hubWorldPageView.OnCancelDownloadClicked -= HandleCancelDownloadClicked;
        _hubWorldPageView.OnInstallWorldClicked -= HandleInstallWorldClicked;
        _hubWorldPageView.OnInstanceCodeSelected -= HandleInstanceSelected;
        _hubWorldPageView.OnAutoSelectInstanceClicked -= HandleChooseInstanceForMeSelected;
        _hubWorldPageView.OnEnterWorldClicked -= HandleEnterWorldClicked;
    }
}
