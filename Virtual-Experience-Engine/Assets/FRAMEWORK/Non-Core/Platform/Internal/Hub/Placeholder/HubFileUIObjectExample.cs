using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
using VE2.NonCore.Platform.Private;
using VE2_NonCore_FileSystem_Interfaces_Common;
using VE2_NonCore_FileSystem_Interfaces_Internal;
using VE2_NonCore_FileSystem_Interfaces_Plugin;

public class HubFileUIObjectExample : MonoBehaviour
{
    [SerializeField] private TMP_Text _fileNameText;
    [SerializeField] private TMP_Text _categoryText;
    [SerializeField] private TMP_Text _versionNumberText;


    [SerializeField] private GameObject _taskPanel;
    [SerializeField] private TMP_Text _currentTaskTypeText;
    [SerializeField] private TMP_Text _currentTaskProgressText;
    [SerializeField] private TMP_Text _currentTaskStatusText;
    [SerializeField] private Button _cancelTaskButton;

    [SerializeField] private GameObject _loadingText;
    [SerializeField] private GameObject _loadedPanel;

    
    [SerializeField] private Button _downloadRemoteButton;
    [SerializeField] private Button _playButton;

    private IRemoteFileTaskInfo _currentRemoteTask;
    private int _activeRemoteVersion;

    private IPlatformService _platformService;
    private IInternalFileSystem _fileSystem;
    private string _worldFolder;

    public void Setup(IPlatformService platformService, IInternalFileSystem fileSystem, string worldFolder)
    {
        _platformService = platformService;
        _fileSystem = fileSystem;

        _worldFolder = worldFolder;

        string worldFolderName = _worldFolder.Substring(_worldFolder.IndexOf('/') + 1);
        _categoryText.text = worldFolderName.Substring(0, worldFolderName.IndexOf('_'));
        _fileNameText.text = worldFolderName.Substring(_worldFolder.IndexOf('_') + 1);

        _taskPanel.SetActive(false); //No current tasks!
        _cancelTaskButton.gameObject.SetActive(false);

        _loadingText.SetActive(true);
        _loadedPanel.SetActive(false);
        _downloadRemoteButton.gameObject.SetActive(false);
        _playButton.gameObject.SetActive(false);

        IRemoteFolderSearchInfo searchInfo = _fileSystem.GetRemoteFoldersAtPath(_worldFolder);
        searchInfo.OnSearchComplete += HandleVersionSearchComplete;
    }

    private void HandleVersionSearchComplete(IRemoteFolderSearchInfo info)
    {
        info.OnSearchComplete -= HandleVersionSearchComplete;

        _activeRemoteVersion = GetHighestVersionNumberFromFoldersList(info.FoldersFound);
        
        List<string> localWorldVersions = _fileSystem.GetLocalFoldersAtPath(_worldFolder);
        int _highestLocalVersionFound = GetHighestVersionNumberFromFoldersList(localWorldVersions);

        bool isAvailableLocally = _highestLocalVersionFound >= _activeRemoteVersion;

        _loadingText.SetActive(false);
        _loadedPanel.SetActive(true);
        _downloadRemoteButton.gameObject.SetActive(!isAvailableLocally);
        _playButton.gameObject.SetActive(isAvailableLocally);

        _versionNumberText.text = $"V{_activeRemoteVersion}";
    }

    private void Update()
    {
        if (_currentRemoteTask == null)
            return;

        _currentTaskTypeText.text = _currentRemoteTask.Type.ToString();
        _currentTaskStatusText.text = _currentRemoteTask.Status.ToString();

        _currentTaskProgressText.gameObject.SetActive(_currentRemoteTask.Status == RemoteFileTaskStatus.InProgress);
        _currentTaskProgressText.text = $"{Mathf.FloorToInt(_currentRemoteTask.Progress * 100f)}%";

        _cancelTaskButton.gameObject.SetActive(_currentRemoteTask.IsCancellable);
    }

    public void DownloadRemoteFile()
    {
        IRemoteFileSearchInfo searchInfo = _fileSystem.GetRemoteFilesAtPath($"{_worldFolder}/{_activeRemoteVersion.ToString("D3")}");
        searchInfo.OnSearchComplete += HandleWorldFilesSearchComplete;
    }

    List<string> _filesToDownload;
    int _curentFileDownloadIndex;

    private void HandleWorldFilesSearchComplete(IRemoteFileSearchInfo info)
    {
        info.OnSearchComplete -= HandleWorldFilesSearchComplete;
        _filesToDownload = new List<string>(info.FilesFound.Keys);

        int minNumFiles = Application.platform == RuntimePlatform.Android ? 2 : 3;

        if (_filesToDownload.Count < minNumFiles)
        {
            Debug.LogError("Couldn't find remote files...");
            return;
        }

        _taskPanel.SetActive(true);

        _curentFileDownloadIndex = 0;
        _currentRemoteTask = _fileSystem.DownloadFile(_filesToDownload[_curentFileDownloadIndex]);
        _currentRemoteTask.OnTaskCompleted += HandleDownloadWorldFileComplete;
    }

    private void HandleDownloadWorldFileComplete(IRemoteFileTaskInfo task)
    {
        _currentRemoteTask.OnTaskCompleted -= HandleDownloadWorldFileComplete;

        if (task.Status == RemoteFileTaskStatus.Succeeded)
        {
            _curentFileDownloadIndex++;
            if (_curentFileDownloadIndex < _filesToDownload.Count)
            {
                _currentRemoteTask = _fileSystem.DownloadFile(_filesToDownload[_curentFileDownloadIndex]);
                _currentRemoteTask.OnTaskCompleted += HandleDownloadWorldFileComplete;
            }
            else
            {
                if (Application.platform == RuntimePlatform.Android)
                {
                    //TODO - need a proper way to get the file path, should come from filesystem
                    string filepath = $"{Application.persistentDataPath}/files/VE2/Worlds/Android/{_worldFolder}/{_activeRemoteVersion.ToString("D3")}/{_fileNameText.text}.apk";
                    InstallAPK(filepath);
                }

                _downloadRemoteButton.gameObject.SetActive(false);
                _playButton.gameObject.SetActive(true);
            }
        }
        else 
        {
            Debug.LogError("Failed to download file!");
        }
    }

    public void InstallAPK(string filePath)
    {
        Debug.Log("Installing APK: " + filePath);
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
    }

    public void CancelTask()
    {
        _currentRemoteTask.CancelRemoteFileTask();
    }

    private int GetHighestVersionNumberFromFoldersList(List<string> folderList)
    {
        // A regular expression to match strings that consist of 3 digits only
        Regex numericRegex = new Regex(@"^\d{3}$");

        int highestFolderFound = 0;

        foreach (string folderPathAndName in folderList)
        {
            string folderName = folderPathAndName.Contains("/") ? folderPathAndName.Substring(folderPathAndName.LastIndexOf("/") + 1) : folderPathAndName;

            //Debug.Log(folderName);

            // Check if the folder name matches the format
            if (numericRegex.IsMatch(folderName))
            {
                // Parse the numeric part of the folder name
                if (int.TryParse(folderName, out int folderNumber))
                {
                    // Update the highest number if this one is greater
                    if (folderNumber > highestFolderFound)
                        highestFolderFound = folderNumber;
                }
            }
        }

        return highestFolderFound;
    }

    public void HandlePlayButtonPressed() 
    {
        //We should request an allocation to the instance here

        //Should we have to though, for single player plugins?
        //Or even if you're trying to run virse offline 
        //Does feel like the single player option shouldn't even require a connection to the platform server 
        /*
            Basically, the hub needs a way of launching the plugin directly without making a request to the platform server
            Hub can just talk to PluginLoader explicitly?
        */

        //Should we really be supporting a fully offline mode? 
        //So V_PlatformIntegration's DebugPlayerSettings become FallbackPlayerSettings
        //We're getting rid of player settings anyway, what does the platform even provide? Just the IP 

        //Ok, what will inform this decision? Just getting the standalone mode working! 
        //for now, we can just have the hub talk to the plugin loader directly, and the plugin loader can just load the plugin
        //We shouldn't need to think about the server at this stage? If all is working, we should get instance sync anyway
        //Ah actually, no, flow to the hub wont work 
        //Ok, fine, let's start with just making the request via platform service

        //Request allocation to that world, stripping out the category prefix
        _platformService.RequestInstanceAllocation(_fileNameText.text, _activeRemoteVersion.ToString());
    }
}
