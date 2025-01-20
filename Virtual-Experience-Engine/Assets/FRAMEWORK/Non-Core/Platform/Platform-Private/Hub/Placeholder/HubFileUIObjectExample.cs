using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
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

    private IInternalFileSystem _fileSystem;
    private string _worldFolder;

    public void Setup(IInternalFileSystem fileSystem, string worldFolder)
    {
        _fileSystem = fileSystem;

        _worldFolder = worldFolder;

        string worldFolderName = _worldFolder.Substring(_worldFolder.IndexOf('/') + 1);
        _categoryText.text = worldFolderName.Substring(0, worldFolderName.IndexOf('-'));
        _fileNameText.text = worldFolderName.Substring(_worldFolder.LastIndexOf('-') + 1);

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

        Debug.Log("FOUND WORLD FILES");
        foreach (string file in _filesToDownload)
        {
            Debug.Log(file);
        }

        if (_filesToDownload.Count < 3)
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
                _downloadRemoteButton.gameObject.SetActive(false);
                _playButton.gameObject.SetActive(true);
            }
        }
        else 
        {
            Debug.LogError("Failed to download file!");
        }
    }

    public void HandleDownloadRemoteFileComplete(IRemoteFileTaskInfo task)
    {
        _currentRemoteTask.OnTaskCompleted -= HandleDownloadRemoteFileComplete;

        if (task.Status == RemoteFileTaskStatus.Succeeded)
        {
            _downloadRemoteButton.gameObject.SetActive(false);
            _playButton.gameObject.SetActive(true);
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

            Debug.Log(folderName);

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
        Debug.Log("Play button pressed!");
    }
}
