using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.SceneManagement;

[Serializable]
public class RemoteFileTaskDetails //TODO: May have to move
{
    [BeginHorizontal(ControlFieldWidth = false), SerializeField] public string Type;
    [SerializeField, LabelWidth(110f)] public string NameAndPath; //Relative to working path
    [EndHorizontal, SerializeField] public float Progress;

    public RemoteFileTaskDetails(string type, float progress, string name, string path, string workingPath)
    {
        Type = type;
        NameAndPath = (path + name).Replace($"{workingPath}/", ""); //TODO: Isn't quite right
        Progress = progress;
    }
}

public class V_PluginFileStorage : MonoBehaviour
{
    [SerializeField, SpaceArea(spaceAfter: 10)] private FTPNetworkSettings ftpNetworkSettings;

    [Title("Play mode debug")]
    [Help("Enter play mode to view local and remote files")]

    [EditorButton(nameof(OpenLocalWorkingFolder), "Open Local Working Folder", activityType: ButtonActivityType.Everything, Order = 2)]
    [EditorButton(nameof(RefreshLocalFiles), "Refresh Local Files", activityType: ButtonActivityType.OnPlayMode, Order = -1)]
    [EditorButton(nameof(UploadAllFiles), "Upload All Files", activityType: ButtonActivityType.OnPlayMode, Order = -1)]
    [EditorButton(nameof(DeleteAllLocalFiles), "Delete All Local Files", activityType: ButtonActivityType.OnPlayMode, Order = -1)]
    [SerializeField, Disable, BeginGroup("Local Files"), EndGroup, SpaceArea(spaceBefore: 10)] private List<string> _localFilesAvailable = new(); //TODO don't show full local path


    [EditorButton(nameof(RefreshRemoteFiles), "Refresh Remote Files", activityType: ButtonActivityType.OnPlayMode, Order = -1)]
    [EditorButton(nameof(DownloadAllFiles), "Download all Files", activityType: ButtonActivityType.OnPlayMode, Order = -1)]
    [EditorButton(nameof(DeleteAllRemoteFiles), "Delete All Remote Files", activityType: ButtonActivityType.OnPlayMode, Order = -1)]
    [SerializeField, Disable, BeginGroup("Remote Files"), EndGroup, SpaceArea(spaceBefore: 10)] private List<string> _remoteFilesAvailable = new();


    [SerializeField, IgnoreParent, BeginGroup("Remote File Tasks"), EndGroup, SpaceArea(spaceBefore: 10)] private List<RemoteFileTaskDetails> _queuedTaskDetails = new();


    #region interface stuff 
    public void RefreshLocalFiles() => _fileStorageService.RefreshLocalFiles();
    public void RefreshRemoteFiles() => _fileStorageService.RefreshRemoteFiles();
    #endregion

    private FileStorageService _fileStorageService;
    private string _localWorkingFilePath => $"VE2/PluginFiles/{SceneManager.GetActiveScene().name}";

    private void OnGUI() 
    {
        if (_fileStorageService != null)
        {
            _queuedTaskDetails.Clear();
            _queuedTaskDetails = _fileStorageService.GetAllUpcomingFileTransferDetails();
        }
    }

    private void OnEnable()
    {
        _fileStorageService = FileStorageServiceFactory.CreateFileStorageService(ftpNetworkSettings, _localWorkingFilePath);
        _fileStorageService.OnFileStorageServiceReady += HandleFileStorageServiceReady;
        _fileStorageService.OnRemoteFilesRefreshed += HandleRemoteFilesRefreshed;
        _fileStorageService.OnLocalFilesRefreshed += HandleLocalFilesRefreshed;
    }

    private void HandleFileStorageServiceReady()
    {
        _fileStorageService.OnFileStorageServiceReady -= HandleFileStorageServiceReady;
        HandleLocalFilesRefreshed(); //Happens immediately when service is created 
    }

    public void OpenLocalWorkingFolder()
    {
        string path = (Application.persistentDataPath + "/files/" + _localWorkingFilePath).Replace("/", "\\");
        UnityEngine.Debug.Log("Try open " + path);
        try
        {
            // Check if the file or directory exists
            if (System.IO.Directory.Exists(path))
            {
                // Open Windows Explorer with the specified path
                Process.Start("explorer.exe", path);
            }
            else
            {
                Console.WriteLine("The specified path does not exist.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
    }

    private void HandleLocalFilesRefreshed() 
    {
        _localFilesAvailable.Clear();
        foreach (var file in _fileStorageService.localFiles)
            _localFilesAvailable.Add(file.Key);
    }

    private void HandleRemoteFilesRefreshed() 
    {
        _remoteFilesAvailable.Clear();
        foreach (var file in _fileStorageService.RemoteFiles)
            _remoteFilesAvailable.Add(file.Key);
    }

    private void OnDisable() 
    {
        _fileStorageService.TearDown();
        _queuedTaskDetails.Clear();
    }

    #region TO-REMOVE-DEBUG //TODO:

    private void DownloadAllFiles()
    {
        foreach (string fileNameAndPath in _fileStorageService.RemoteFiles.Keys)
            _fileStorageService.DownloadFile(fileNameAndPath);

    }

    private void UploadAllFiles()
    {
        foreach (string fileNameAndPath in _fileStorageService.localFiles.Keys)
            _fileStorageService.UploadFile(fileNameAndPath);
    }

    private void DeleteAllLocalFiles() 
    {
        List<string> localFileNames = new List<string>(_fileStorageService.localFiles.Keys);
        foreach (string fileNameAndPath in localFileNames)
            _fileStorageService.DeleteLocalFile(fileNameAndPath);
    }

    private void DeleteAllRemoteFiles() 
    {
        List<string> remoteFileNames = new List<string>(_fileStorageService.RemoteFiles.Keys);
        foreach (string fileNameAndPath in remoteFileNames)
            _fileStorageService.DeleteRemoteFile(fileNameAndPath);
    }

    #endregion
}
