using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VE2.NonCore.FileSystem.API;
using VE2.NonCore.Platform.API;
using static VE2.NonCore.Platform.API.PlatformPublicSerializables;

internal class HubController : MonoBehaviour
{
    //TODO: username and password should come from arguments - or probably in a different scene?
    [SerializeField] private ServerConnectionSettings _platformServerConnectionSettings = new("devName", "devPassword", "127.0.0.1", 4298);

    [SerializeField] private GameObject _fileSystemGameObject;
    private IFileSystemInternal _fileSystem => _fileSystemGameObject.GetComponent<IFileSystemInternal>();
    [EditorButton(nameof(HandleRefreshFilesButtonClicked), "Refresh Files", activityType: ButtonActivityType.OnPlayMode, Order = -1)]
    [SerializeField] private string folderToSearch = "/";

    [SerializeField] private HubHomePageView _hubHomePageView;
    [SerializeField] private HubCategoryPageView _hubCategoryPageView;
    [SerializeField] private HubWorldPageView _hubWorldPageView;

    private IPlatformServiceInternal _platformService;

    private void Awake()
    {
        _hubHomePageView.OnWorldClicked += HandleWorldClicked;
        _hubCategoryPageView.OnWorldClicked += HandleWorldClicked;
        _hubHomePageView.OnCategoryClicked += HandleCategoryClicked;
        _hubCategoryPageView.OnBackClicked += HandleBackClicked;

        _hubWorldPageView.OnBackClicked += HandleBackClicked;
        _hubWorldPageView.OnDownloadWorldClicked += HandleStartDownloadClicked;
        _hubWorldPageView.OnCancelDownloadClicked += HandleCancelDownloadClicked;
        _hubWorldPageView.OnInstallWorldClicked += HandleInstallWorldClicked;
        _hubWorldPageView.OnInstanceCodeSelected += HandleInstanceSelected;
        _hubWorldPageView.OnAutoSelectInstanceClicked += HandleChooseInstanceForMeSelected;
        _hubWorldPageView.OnEnterWorldClicked += HandleEnterWorldClicked;
    }

    private void OnEnable()
    {
        Debug.Log("Connecting to hub instance");
        _platformService = (IPlatformServiceInternal)PlatformAPI.PlatformService;
        InstanceCode hubInstanceCode = new InstanceCode("Hub", "Solo", 0);
        _platformService.UpdateSettings(_platformServerConnectionSettings, hubInstanceCode);
        _platformService.ConnectToPlatform();

        Application.focusChanged += OnFocusChanged;

        if (_fileSystem.IsFileSystemReady)
            HandleFileSystemReady();
        else
            _fileSystem.OnFileSystemReady += HandleFileSystemReady;

        _platformService.OnInstanceInfosChanged += HandleInstanceInfosChanged;
    }

    private void HandleInstanceInfosChanged(Dictionary<InstanceCode, PlatformInstanceInfo> instanceInfos)
    {
        if (_viewingWorldDetails != null && _hubWorldPageView.gameObject.activeSelf)
            _hubWorldPageView.UpdateInstances(GetInstancesForWorldName(_viewingWorldDetails.Name));
    }

    private void Update()
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
    } 

    private void HandleFileSystemReady()
    {
        _fileSystem.OnFileSystemReady -= HandleFileSystemReady;
        StartSearch();
    }

    private void StartSearch()
    {
        IRemoteFolderSearchInfo task = _fileSystem.GetRemoteFoldersAtPath(folderToSearch);
        task.OnSearchComplete += HandleGetRemoteFolders;
    }

    private void HandleGetRemoteFolders(IRemoteFolderSearchInfo search)
    {
        List<string> localWorlds = _fileSystem.GetLocalFoldersAtPath(folderToSearch);

        Dictionary<string, int> activeWorldsAndVersions = _platformService.ActiveWorldsNamesAndVersions.ToDictionary(w => w.Item1, w => w.Item2);
        // Debug.Log("Active worlds from platform service: " + activeWorldsAndVersions.Count);
        // foreach (var world in activeWorldsAndVersions)
        // {
        //     Debug.Log($"Active world: {world.Key}, Version: {world.Value}");
        // }

        List<HubWorldDetails> availableWorlds = new();
        Dictionary<string, WorldCategory> worldCategories = new();

        foreach (string remoteWorldFolder in search.FoldersFound)
        {
            HubWorldDetails worldDetails = new();
            worldDetails.Name = remoteWorldFolder;

            string category;

            if (activeWorldsAndVersions.TryGetValue(remoteWorldFolder, out int version))
            {
                category = "Default"; // Placeholder
                worldDetails.IsExperimental = false;
                worldDetails.LiveVersionNumber = version;
                worldDetails.Description = "A sample description"; // Placeholder
                worldDetails.Author = "Author Name"; // Placeholder
                worldDetails.DateOfPublish = DateTime.Now; // Placeholder
            }
            else
            {
                category = "Experimental";
                worldDetails.IsExperimental = true;
            }

            worldDetails.Category = category;
            WorldCategory worldCategory;
            if (worldCategories.TryGetValue(category, out worldCategory))
            {
                worldCategory.Worlds.Add(worldDetails);
            }
            else
            {
                worldCategory = new WorldCategory(category, new List<HubWorldDetails> { worldDetails });
                worldCategories.Add(category, worldCategory);
            }

            availableWorlds.Add(worldDetails);
        }

        List<HubWorldDetails> suggestedWorldDetails = availableWorlds
            .Take(5)
            .ToList();  

        _hubHomePageView.SetupView(suggestedWorldDetails, worldCategories.Values.ToList());
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

        public void MarkComplete() {AmountDownloaded = FileSize;}
    }
    private List<HubFileDownloadInfo> _filesToDownload;
    private int _curentFileDownloadIndex = -1;
    private IRemoteFileTaskInfo _currentDownloadTask;

    private void HandleStartDownloadClicked()
    {
        Debug.Log("Download world clicked: " + _viewingWorldDetails.Name);
        _hubWorldPageView.ShowStartDownloadWorldButton();

        _hubWorldPageView.UpdateDownloadingWorldProgress(0);

        IRemoteFileSearchInfo searchInfo = _fileSystem.GetRemoteFilesAtPath($"{_viewingWorldDetails.Name}/{_selectedWorldVersion.ToString("D3")}");
        searchInfo.OnSearchComplete += HandleWorldFilesSearchComplete;
    }

    private void HandleWorldFilesSearchComplete(IRemoteFileSearchInfo searchInfo)
    {
        if (searchInfo.FilesFound.Count == 0)
        {
            Debug.LogError("No files found for world: " + _viewingWorldDetails.Name);
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
        //Start polling for successful instal of package
    }

    private void HandleInstanceSelected(InstanceCode instanceCode)
    {
        _selectedInstanceCode = instanceCode;
        _hubWorldPageView.SetSelectedInstance(instanceCode);

        if (_viewingWorldDetails.IsVersionDownloaded(_selectedWorldVersion) && _viewingWorldDetails.IsVersionInstalled(_selectedWorldVersion))
            _hubWorldPageView.ShowEnterWorldButton();
    }

    private void HandleChooseInstanceForMeSelected()
    {
        //TODO - version number may change. need to think about how we're handling versions overall really 
        //Maybe it should just be a bool for "live version" or "experimental version" 
        InstanceCode instanceCode = new(_viewingWorldDetails.Name, "00", (ushort)_selectedWorldVersion);

        HandleInstanceSelected(instanceCode);
    }

    private void HandleEnterWorldClicked()
    {
        Debug.Log("Enter world clicked: " + _viewingWorldDetails.Name + " Version: " + _selectedWorldVersion);

        if (_selectedWorldVersion == -1)
        {
            Debug.LogError("No version selected for world: " + _viewingWorldDetails.Name);
            return;
        }

        _platformService.RequestInstanceAllocation(new InstanceCode(_viewingWorldDetails.Name, "00", (ushort)_selectedWorldVersion));
    }

    private HubWorldDetails _viewingWorldDetails;
    private int _selectedWorldVersion = -1;
    private InstanceCode _selectedInstanceCode = null;

    private void HandleWorldClicked(HubWorldDetails worldDetails)
    {
        Debug.Log("World clicked: " + worldDetails.Name);
        _viewingWorldDetails = worldDetails;

        _hubWorldPageView.SetupView(worldDetails, GetInstancesForWorldName(worldDetails.Name));
        _hubHomePageView.gameObject.SetActive(false);
        _hubCategoryPageView.gameObject.SetActive(false);
        _hubWorldPageView.gameObject.SetActive(true);

        //First, we have to search for the versions of that world
        IRemoteFolderSearchInfo searchInfo = _fileSystem.GetRemoteFoldersAtPath($"{worldDetails.Name}");
        searchInfo.OnSearchComplete += HandleWorldVersionSearchComplete;
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
        _viewingWorldDetails.VersionsAvailableRemotely = searchInfo.FoldersFound
            .Select(s => int.TryParse(s, out var v) ? (int?)v : null)
            .Where(v => v.HasValue)
            .Select(v => v.Value)
            .ToList();

        //TODO: This returns DevBlue/002, while the above just returns 002

        //The question is, which do we want??
        //Maybe it makes sense for there to be an ILocalFolderSearchInfo?? Idk
        _viewingWorldDetails.VersionsAvailableLocally = _fileSystem.GetLocalFoldersAtPath($"{_viewingWorldDetails.Name}")
            .Select(s => int.TryParse(s, out var v) ? (int?)v : null)
            .Where(v => v.HasValue)
            .Select(v => v.Value)
            .ToList();

        List<string> localWorlds = _fileSystem.GetLocalFoldersAtPath($"{_viewingWorldDetails.Name}");
        Debug.LogWarning("Local world strings found: " + localWorlds.Count);
        foreach (string localWorld in localWorlds)
        {
            Debug.Log("Found local world: " + localWorld);
        }

        Debug.Log("Searched for local versions at " + $"{_viewingWorldDetails.Name}, found " + _viewingWorldDetails.VersionsAvailableLocally.Count + " local versions");
        foreach (int version in _viewingWorldDetails.VersionsAvailableLocally)
        {
            Debug.Log("Found local version: " + version);
        }

        int targetVersion;

        if (_viewingWorldDetails.IsExperimental)
        {
            targetVersion = _viewingWorldDetails.VersionsAvailableRemotely.Max();
        }
        else
        {
            if (_viewingWorldDetails.VersionsAvailableRemotely.Contains(_viewingWorldDetails.LiveVersionNumber))
            {
                targetVersion = _viewingWorldDetails.LiveVersionNumber;
            }
            else
            {
                Debug.LogError($"Live version {_viewingWorldDetails.LiveVersionNumber} not found for world {_viewingWorldDetails.Name}");
                targetVersion = -1; //TODO - show some error on UI  
            }
        }

        //TODO: don't show non-live versions if we haven't ticked "show experimental worlds" on the UI somewhere
        _hubWorldPageView.ShowAvailableVersions(_viewingWorldDetails.VersionsAvailableRemotely);

        bool needsDownload = !_viewingWorldDetails.VersionsAvailableLocally.Contains(targetVersion);
        bool downloadedButNotInstalled;
        if (Application.platform == RuntimePlatform.Android)
        {
            downloadedButNotInstalled = false; // TODO: Check if world and version is installed 
        }
        else
        {
            downloadedButNotInstalled = false; //On windows, no need to install
        }
        bool isVersionExperimental = targetVersion != _viewingWorldDetails.LiveVersionNumber;

        Debug.LogWarning("####################");
        Debug.Log($"Showing selected version: {targetVersion}, Needs Download: {needsDownload}, Downloaded But Not Installed: {downloadedButNotInstalled}, Is Experimental: {isVersionExperimental}");

        _hubWorldPageView.ShowSelectedVersion(targetVersion, needsDownload, downloadedButNotInstalled, isVersionExperimental, _selectedInstanceCode != null);
        _selectedWorldVersion = targetVersion;

        //_hubWorldPageView.SupplyVersions
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

    private void HandleCategoryClicked(WorldCategory category)
    {
        Debug.Log("Category clicked: " + category.CategoryName);

        _hubCategoryPageView.SetupView(category);
        _hubHomePageView.gameObject.SetActive(false);
        _hubCategoryPageView.gameObject.SetActive(true);
        _hubWorldPageView.gameObject.SetActive(false);
    }

    private void HandleBackClicked()
    {
        Debug.Log("Back clicked");

        _hubHomePageView.gameObject.SetActive(true);
        _hubCategoryPageView.gameObject.SetActive(false);
        _hubWorldPageView.gameObject.SetActive(false);
    }


    public void HandleRefreshFilesButtonClicked()
    {
        StartSearch();
    }

    private void OnDisable()
    {
        Application.focusChanged -= OnFocusChanged;

        //on android, we need to disconnect here, as the plugin we're going to will connect to the server itself
        //if we're in windows, this will happen automatically
        if (Application.platform == RuntimePlatform.Android)
        {
            Debug.Log("Disconnecting from hub instance");
            IPlatformServiceInternal platformService = (IPlatformServiceInternal)PlatformAPI.PlatformService;
            platformService.TearDown();
        }
    }

    private void OnFocusChanged(bool hasFocus)
    {
        //Debug.Log("Focus changed: " + hasFocus);
        if (Application.platform == RuntimePlatform.Android)
        {
            if (hasFocus)
            {
                OnEnable();
            }
            else
            {
                //This happens when we go to a plugin
                //We need to disconnect from the server
                OnDisable();
            }
        }
    }
}

//At this point, we don't know version. It's only when we select the world should we ask the FTP server what versions there are
internal class HubWorldDetails
{
    public string Name;
    public string Category;

    public bool IsExperimental;

    //These fields will be unpopulated if the world is experimental=========
    public int LiveVersionNumber = -1; // -1 indicates no live version
    public string Description;
    public string Author;
    public DateTime DateOfPublish;
    //======================================================================

    public List<int> VersionsAvailableLocally;
    public List<int> VersionsAvailableRemotely;

    /*
        So we open the world page, one that happens we search for remote versions, 
        Controller then tells the view what the versions are, and which one to be targeting 
    */

    public bool IsVersionDownloaded(int version)
    {
        return VersionsAvailableLocally.Contains(version);
    }

    public bool IsVersionInstalled(int version)
    {
        if (Application.platform == RuntimePlatform.Android)
        {
            return true; //TODO!
        }
        else
        {
            //On windows, we don't need to install, so if it's downloaded, it's installed
            return IsVersionDownloaded(version);
        }
    }
}

/*
    We start off with a list of strings for folders. 
    We split these into categories, use that to create category buttons, 
    and also use that to create world buttons.

    World buttons will need to be setup with world name, and the category they relate to


    How are we handling controllers and views? 
    Probably don't want this HubController script to control everything 
    So, hub controller should get this list of cats and worlds, and pass that to HubHomePageController
    HubHomePageController then... idk... screw it, let's bung it all in here for now, it's a prototype



    We should also pass down if a world is experimental or not, 


    ===============================
    does the FTP server even need to kno about categories? 
    Maybe not?? 
    We can get all worlds from FTP 
    We can match those against what we get from the the platform, who can tell us what category those worlds go into 
    If a world doesn't have a category, it'll fall under "experimental" or "unknown" 
    If a world DOES have a category, we show it under that category, but we can still change version numbers on the UI 

    When we click on a worldbutton, the view populates with the world name, doesn't even need the category 
    When the view opens, if it's experimental, we scan that world folder for versions 
    If it's NOT experimental... we scan for versions anyway, and confirm the one indicated by the platform is actually there 
    When we click download, we download that world, at that version. No categories needed.
    ===============================

    Is there any benefit to having cats in FTP? It means we can categorise worlds without the platform telling us 
    THAT means, if we're running offline, we can no longer categorise worlds, since the cat isn't encoded in the world folder name...
    That might ruin it all then... hmmn

    So, if we're offline, and we don't have the list of active worlds from the server... then what? 
    We just show the most recent version of everything that we have locally?

    Hold on though, aren't we going to need to store metadata about the world anyway? Things beyond just what comes from FTP?
    E.G image? 

    We could just say "if offline, show a different view entirely"
    Or just "every time we get info from the platform, store that in a local file, if offline, fall back to that file"
    Let's just do that I think. 

*/