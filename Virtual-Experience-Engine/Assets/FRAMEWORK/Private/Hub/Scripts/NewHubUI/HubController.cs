using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEditorInternal;
using UnityEngine;
using VE2.Common.API;
using VE2.NonCore.FileSystem.API;
using VE2.NonCore.Platform.API;
using static VE2.NonCore.Platform.API.PlatformPublicSerializables;

//TODO - this should instead be "HubHomePagehandler"
internal class HubController : MonoBehaviour
{
    //TODO: username and password should come from arguments - or probably in a different scene?
    [SerializeField] private ServerConnectionSettings _platformServerConnectionSettings = new("devName", "devPassword", "127.0.0.1", 4298);

    [SerializeField] private GameObject _fileSystemGameObject;
    private IFileSystemInternal _fileSystem => _fileSystemGameObject.GetComponent<IFileSystemInternal>();
    [EditorButton(nameof(HandleRefreshButtonClicked), "Refresh Files", activityType: ButtonActivityType.OnPlayMode, Order = -1)]
    [SerializeField] private string folderToSearch = "/";

    [SerializeField] private HubMasterView _hubMasterView;
    [SerializeField] private HubHomePageView _hubHomePageView;
    [SerializeField] private HubCategoryPageView _hubCategoryPageView;
    [SerializeField] private HubWorldPageView _hubWorldPageView;

    private IPlatformServiceInternal _platformService;
    private HubWorldPageHandler _hubWorldPageHandler;

    private void Awake()
    {
        _hubHomePageView.OnWorldClicked += HandleWorldClicked;
        _hubHomePageView.OnRefreshWorldsClicked += HandleRefreshButtonClicked;
        _hubCategoryPageView.OnWorldClicked += HandleWorldClicked;
        _hubHomePageView.OnCategoryClicked += HandleCategoryClicked;
        _hubCategoryPageView.OnBackClicked += HandleBackClicked;

        _hubWorldPageView.OnBackClicked += HandleBackClicked;

        _hubMasterView.ToggleLoadingScreen(true);
    }

    private void HandleRefreshButtonClicked()
    {
        _hubHomePageView.TearDown();
        _hubMasterView.ToggleLoadingScreen(true);
        StartSearchOfWorldFolders();
    }

    private void OnEnable()
    {
        Debug.Log("Connecting to hub instance");
        _platformService = (IPlatformServiceInternal)VE2API.PlatformService;
        InstanceCode hubInstanceCode = new("Hub", "Solo", 0);
        _platformService.UpdateSettings(_platformServerConnectionSettings, hubInstanceCode);
        _platformService.ConnectToPlatform();

        //Application.focusChanged += OnFocusChanged;

        if (_fileSystem.IsFileSystemReady)
            HandleFileSystemReady();
        else
            _fileSystem.OnFileSystemReady += HandleFileSystemReady;
    }

    private void Update()
    {
        _hubWorldPageHandler?.HandleUpdate();
    }

    private void HandleFileSystemReady()
    {
        //If we're here, means we have connected to the platform successfully
        _fileSystem.OnFileSystemReady -= HandleFileSystemReady;
        StartSearchOfWorldFolders();
    }

    private void StartSearchOfWorldFolders()
    {
        IRemoteFolderSearchInfo task = _fileSystem.GetRemoteFoldersAtPath(folderToSearch);
        task.OnSearchComplete += HandleGetRemoteWorldFolders;
    }

    private void HandleGetRemoteWorldFolders(IRemoteFolderSearchInfo search)
    {
        if (search.CompletionCode.ToUpper().Contains("ERROR"))
        {
            //TODO - show some error on UI
            Debug.LogError("Failed to search for remote folders: " + search.CompletionCode);
            return;
        }

        _hubMasterView.HandleUIReady();
        _hubMasterView.ToggleLoadingScreen(false);

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

    private void HandleWorldClicked(HubWorldDetails worldDetails)
    {
        _hubWorldPageHandler = new HubWorldPageHandler(_hubWorldPageView, worldDetails, _platformService, _fileSystem);

        _hubHomePageView.gameObject.SetActive(false);
        _hubCategoryPageView.gameObject.SetActive(false);
        _hubWorldPageView.gameObject.SetActive(true);
    }

    private void HandleCategoryClicked(WorldCategory category)
    {
        _hubCategoryPageView.SetupView(category);
        _hubHomePageView.gameObject.SetActive(false);
        _hubCategoryPageView.gameObject.SetActive(true);
        _hubWorldPageView.gameObject.SetActive(false);
    }

    private void HandleBackClicked()
    {
        _hubWorldPageHandler?.TearDown();
        _hubWorldPageHandler = null;

        _hubHomePageView.gameObject.SetActive(true);
        _hubCategoryPageView.gameObject.SetActive(false);
        _hubWorldPageView.gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        Application.focusChanged -= OnFocusChanged;

        //on android, we need to disconnect here, as the plugin we're going to will connect to the server itself
        //if we're in windows, this will happen automatically
        if (Application.platform == RuntimePlatform.Android)
        {
            Debug.Log("Disconnecting from hub instance");
            IPlatformServiceInternal platformService = (IPlatformServiceInternal)VE2API.PlatformService;
            platformService.TearDown();
        }
    }

    private void OnFocusChanged(bool hasFocus)
    {
        // //Debug.Log("Focus changed: " + hasFocus);
        // if (Application.platform == RuntimePlatform.Android)
        // {
        //     if (hasFocus)
        //     {
        //         OnEnable();
        //     }
        //     else
        //     {
        //         //This happens when we go to a plugin
        //         //We need to disconnect from the server
        //         OnDisable();
        //     }
        // }
    }

#if UNITY_EDITOR
    private void Start()
    {
        UnityEditor.EditorApplication.pauseStateChanged += state => HandleAppLifecycleChanged(state == UnityEditor.PauseState.Paused);
    }
#else
    void OnApplicationPause(bool pause)
    {
        HandleAppLifecycleChanged(pause);
    }
#endif

    private void HandleAppLifecycleChanged(bool isPaused)
    {
        Debug.Log("Application paused: " + isPaused);

        if (!isPaused)
        {
            //Delay to give us time to figure out we disconnected at all
            DOVirtual.DelayedCall(0.1f, () =>
            {
                if (!_platformService.IsConnectedToServer)
                {
                    Debug.Log("Reconnecting to platform after app resume");
                    _platformService.ConnectToPlatform();
                }
            });
        }
        else
        {
            //This happens when we go to a plugin
            //We need to disconnect from the server
            Debug.Log("Hub paused.... disconnecting from platform");
            _platformService.DisconnectFromPlatform();
        }
    }
}

//At this point, we don't know version. It's only when we select the world should we ask the FTP server what versions there are
internal class HubWorldDetails
{
    public string Name;
    public string Category;

    public string AndroidPackageName => $"com.ImperialCollegeLondon.{Name}";

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
        So we open the world page, once that happens we search for remote versions, 
        Controller then tells the view what the versions are, and which one to be targeting 
    */

    public bool IsVersionDownloaded(int version)
    {
        if (!VersionsAvailableLocally.Contains(version))
        {
            Debug.Log(Name + " Version not downloaded: " + version);
            Debug.Log("Available versions locally: " + string.Join(", ", VersionsAvailableLocally));
            return false;
        }

        return VersionsAvailableLocally.Contains(version);
    }

    public bool IsVersionInstalled(int version)
    {
        if (Application.platform == RuntimePlatform.Android)
        {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            {
                AndroidJavaObject packageManager = currentActivity.Call<AndroidJavaObject>("getPackageManager");
                try
                {
                    AndroidJavaObject packageInfo = packageManager.Call<AndroidJavaObject>("getPackageInfo", AndroidPackageName, 0);
                    long versionCode = packageInfo.Call<long>("getLongVersionCode");

                    if (Time.frameCount % 120 == 0)
                        Debug.Log("Found version code: " + versionCode + " for package: " + AndroidPackageName + " matches version: " + version + "?" + (versionCode == version));

                    return versionCode == version;
                }
                catch (AndroidJavaException e)
                {
                    return false;
                }
            }
        }
        else
        {
            //On windows, we don't need to install, so if it's downloaded, it's installed
            return IsVersionDownloaded(version);
        }
    }
}


/*
    What if we're downloading a world, and then we click on a different world? 
    Ideally, we'd like that download to continue
    Ah, let's not overcomplicate it for now, let's say you have to stay on that page to keep the download going
    We can refactor later by modularizing the download logic into a different component, that can hold some persistent queue/state
*/