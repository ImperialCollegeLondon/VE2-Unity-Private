using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
using VE2_NonCore_FileSystem_Interfaces_Common;
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

    private IPluginFileSystem _fileSystem;
    private FileDetails _fileDetails;

    public void Setup(IPluginFileSystem fileSystem, FileDetails fileDetails)
    {
        _fileSystem = fileSystem;

        _fileDetails = fileDetails;

        string worldFolderName = fileDetails.NameAndPath.Substring(fileDetails.NameAndPath.IndexOf('/') + 1);
        _categoryText.text = worldFolderName.Substring(0, worldFolderName.IndexOf('-'));
        _fileNameText.text = worldFolderName.Substring(fileDetails.NameAndPath.LastIndexOf('-') + 1);

        _taskPanel.SetActive(false); //No current tasks!
        _cancelTaskButton.gameObject.SetActive(false);

        _loadingText.SetActive(true);
        _loadedPanel.SetActive(false);

        IRemoteFolderSearchInfo searchInfo = _fileSystem.GetRemoteFoldersAtPath(fileDetails.NameAndPath);
        searchInfo.OnSearchComplete += HandleSearchComplete;
    }

    private void HandleSearchComplete(IRemoteFolderSearchInfo info)
    {
        info.OnSearchComplete -= HandleSearchComplete;

        int highestFoundRemoteVersion = GetHighestVersionNumberFromFoldersList(info.FoldersFound);
        
        List<string> localWorldVersions = _fileSystem.GetLocalFoldersAtPath(_fileDetails.NameAndPath);
        int _highestLocalVersionFound = GetHighestVersionNumberFromFoldersList(localWorldVersions);

        bool isAvailableLocally = _highestLocalVersionFound >= highestFoundRemoteVersion;

        _downloadRemoteButton.gameObject.SetActive(!isAvailableLocally);
        _playButton.gameObject.SetActive(isAvailableLocally);

        _versionNumberText.text = $"V{math.max(highestFoundRemoteVersion, _highestLocalVersionFound)}";
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
        _currentRemoteTask = _fileSystem.DownloadFile(_fileDetails.NameAndPath);
        _currentRemoteTask.OnTaskCompleted += HandleDownloadRemoteFileComplete;
        _taskPanel.SetActive(true);
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
}
