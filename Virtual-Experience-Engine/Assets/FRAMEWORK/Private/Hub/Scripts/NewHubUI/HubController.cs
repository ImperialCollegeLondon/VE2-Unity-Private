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
    }

    private void OnEnable()
    {
        Debug.Log("Connecting to hub instance");
        _platformService = (IPlatformServiceInternal)PlatformAPI.PlatformService;
        _platformService.UpdateSettings(_platformServerConnectionSettings, "Internal-Hub-Solo-NoVersion");
        _platformService.ConnectToPlatform();

        Application.focusChanged += OnFocusChanged;

        if (_fileSystem.IsFileSystemReady)
            HandleFileSystemReady();
        else
            _fileSystem.OnFileSystemReady += HandleFileSystemReady;
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

        Debug.Log("Found local folders: " + localWorlds.Count);
        foreach (string localWorldFolder in localWorlds)
        {
            Debug.Log("Found local world folder: " + localWorldFolder);
        }

        Debug.Log("Found remote folders: " + search.FoldersFound.Count);
        foreach (string remoteWorldFolder in search.FoldersFound)
        {
            Debug.Log("Found remote world folder: " + remoteWorldFolder);
        }

        Dictionary<string, int> activeWorldsAndVersions = _platformService.ActiveWorldsNamesAndVersions.ToDictionary(w => w.Item1, w => w.Item2);
        Debug.Log("Active worlds from platform service: " + activeWorldsAndVersions.Count);
        foreach (var world in activeWorldsAndVersions)
        {
            Debug.Log($"Active world: {world.Key}, Version: {world.Value}");
        }

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


    public void HandleWorldClicked(HubWorldDetails worldDetails)
    {
        Debug.Log("World clicked: " + worldDetails.Name);

        _hubWorldPageView.SetupView(worldDetails);
        _hubHomePageView.gameObject.SetActive(false);
        _hubCategoryPageView.gameObject.SetActive(false);
        _hubWorldPageView.gameObject.SetActive(true);

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
        Debug.Log("Focus changed: " + hasFocus);
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