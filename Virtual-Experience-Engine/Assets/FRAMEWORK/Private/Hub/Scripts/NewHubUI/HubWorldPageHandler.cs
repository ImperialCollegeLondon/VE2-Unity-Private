using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VE2.NonCore.FileSystem.API;
using VE2.NonCore.Platform.API;
using static VE2.NonCore.Platform.API.PlatformPublicSerializables;

internal class HubWorldPageHandler
{
    private int _selectedWorldVersion = -1;
    private bool _isWorldInstalled = false;
    private InstanceCode _selectedInstanceCode = null;
    private int _logNotInstalledCounter = 0;

    //Download bits - should maybe move into a different module
    private List<HubFileDownloadInfo> _filesToDownload;
    private int _curentFileDownloadIndex = -1;
    private IRemoteFileTaskInfo _currentDownloadTask;


    private readonly HubWorldPageView _hubWorldPageView;
    private HubWorldDetails _worldDetails;
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
        _platformService.OnLeavingInstance += () => _platformService.OnInstanceInfosChanged -= HandleInstanceInfosChanged;

        //_hubWorldPageView.OnBackClicked += HandleBackClicked;
        _hubWorldPageView.OnDownloadWorldClicked += HandleStartDownloadClicked;
        _hubWorldPageView.OnCancelDownloadClicked += HandleCancelDownloadClicked;
        _hubWorldPageView.OnInstallWorldClicked += HandleInstallWorldClicked;
        _hubWorldPageView.OnInstanceCodeSelected += HandleInstanceSelected;
        _hubWorldPageView.OnAutoSelectInstanceClicked += HandleChooseInstanceForMeSelected;
        _hubWorldPageView.OnEnterWorldClicked += HandleEnterWorldClicked;

        _hubWorldPageView.SetupView(worldDetails, GetInstancesForWorldName(worldDetails.Name));

        //First, we have to search for the versions of that world
        IRemoteFolderSearchInfo searchInfo = _fileSystem.GetRemoteFoldersAtPath($"{worldDetails.Name}");
        searchInfo.OnSearchComplete += HandleWorldVersionSearchComplete;
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

        //TODO, only do this if we haven't already got the install button showing 
        //TODO - also shoudln't show enter button if instance isn't already selected
        if (Application.platform == RuntimePlatform.Android && _worldDetails != null)
        {
            bool isWorldInstalled = _worldDetails.IsVersionInstalled(_selectedWorldVersion);

            Debug.Log("Checking if APK is installed: " + _worldDetails.AndroidPackageName);

            //TODO: If the install button is currently showing

            if (isWorldInstalled)
            {
                _hubWorldPageView.ShowEnterWorldButton();
            }
            else
            {
                if (_logNotInstalledCounter % 100 == 0)
                {
                    Debug.Log($"APK {_worldDetails.AndroidPackageName} not installed yet.");
                }
                _logNotInstalledCounter++;
            }
        }
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

        bool needsDownload = !_worldDetails.VersionsAvailableLocally.Contains(targetVersion);
        bool downloadedButNotInstalled = !needsDownload && !_worldDetails.IsVersionInstalled(targetVersion);
        bool isVersionExperimental = targetVersion != _worldDetails.LiveVersionNumber;

        _hubWorldPageView.ShowSelectedVersion(targetVersion, needsDownload, downloadedButNotInstalled, isVersionExperimental, _selectedInstanceCode != null);
        _selectedWorldVersion = targetVersion;
    }

    private List<PlatformInstanceInfo> GetInstancesForWorldName(string worldName)
    {
        Dictionary<string, List<PlatformInstanceInfo>> instancesByWorldNames = new();
        foreach (PlatformInstanceInfo instanceInfo in _platformService.InstanceInfos.Values)
        {
            if (!instancesByWorldNames.ContainsKey(instanceInfo.InstanceCode.WorldName))
                instancesByWorldNames[instanceInfo.InstanceCode.WorldName] = new List<PlatformInstanceInfo>();

            instancesByWorldNames[instanceInfo.InstanceCode.WorldName].Add(instanceInfo);
        }

        return instancesByWorldNames.ContainsKey(worldName)
            ? instancesByWorldNames[worldName]
            : new List<PlatformInstanceInfo>();
    }

    private void HandleInstanceInfosChanged(Dictionary<InstanceCode, PlatformInstanceInfo> instanceInfos)
    {
        if (_worldDetails != null && _hubWorldPageView.gameObject.activeSelf)
            _hubWorldPageView.UpdateInstances(GetInstancesForWorldName(_worldDetails.Name));
    }

    private void HandleStartDownloadClicked()
    {
        Debug.Log("Download world clicked: " + _worldDetails.Name);
        _hubWorldPageView.ShowStartDownloadWorldButton();

        _hubWorldPageView.UpdateDownloadingWorldProgress(0);

        IRemoteFileSearchInfo searchInfo = _fileSystem.GetRemoteFilesAtPath($"{_worldDetails.Name}/{_selectedWorldVersion.ToString("D3")}");
        searchInfo.OnSearchComplete += HandleWorldFilesSearchComplete;
    }

    private void HandleWorldFilesSearchComplete(IRemoteFileSearchInfo searchInfo)
    {
        if (searchInfo.FilesFound.Count == 0)
        {
            Debug.LogError("No files found for world: " + _worldDetails.Name);
            return;
        }

        //Check for correct number of files TODO
        _filesToDownload = searchInfo.FilesFound
            .Select(f => new HubFileDownloadInfo(f.Key, f.Value.Size, (ulong)0))
            .ToList();
        _curentFileDownloadIndex = 0;

        searchInfo.OnSearchComplete -= HandleWorldFilesSearchComplete;

        _hubWorldPageView.ShowDownloadingWorldPanel();
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
        _hubWorldPageView.ShowStartDownloadWorldButton();
    }

    private void HandleAllWorldFilesDownloaded()
    {
        _curentFileDownloadIndex = -1;
        if (Application.platform == RuntimePlatform.Android)
        {
            _hubWorldPageView.ShowInstallWorldButton();
        }
        else
        {
            _hubWorldPageView.ShowEnterWorldButton();
        }
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

    private void HandleInstanceSelected(InstanceCode instanceCode)
    {
        _selectedInstanceCode = instanceCode;
        _hubWorldPageView.SetSelectedInstance(instanceCode);

        if (_worldDetails.IsVersionDownloaded(_selectedWorldVersion) && _worldDetails.IsVersionInstalled(_selectedWorldVersion))
            _hubWorldPageView.ShowEnterWorldButton();
    }

    private void HandleChooseInstanceForMeSelected()
    {
        InstanceCode instanceCode = new(_worldDetails.Name, "00", (ushort)_selectedWorldVersion);

        HandleInstanceSelected(instanceCode);
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
        if (_currentDownloadTask != null)
        {
            _currentDownloadTask.CancelRemoteFileTask();
        }
    }
    
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
}
