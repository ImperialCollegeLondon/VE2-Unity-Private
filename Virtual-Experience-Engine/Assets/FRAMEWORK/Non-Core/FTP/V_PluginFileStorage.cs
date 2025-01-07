using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class V_PluginFileStorage : MonoBehaviour
{
    [SerializeField] private FTPNetworkSettings ftpNetworkSettings;


    [Help("Enter play mode to see local and remote files")]
    [DynamicHelp(nameof(_localPathDebug), UnityMessageType.Info, ApplyCondition = true)]

    [EditorButton(nameof(RefreshLocalFiles), "Refresh Local Files", activityType: ButtonActivityType.OnPlayMode, Order = -1)]
    [SerializeField, Disable] private List<string> _localFilesDebug = new(); //TODO don't show full local path

    [EditorButton(nameof(RefreshRemoteFiles), "Refresh Remote Files", activityType: ButtonActivityType.OnPlayMode, Order = -1)]
    [SerializeField, Disable] private List<string> _remoteFilesDebug = new();

    private string _localPathDebug = "Local files stored at: Unknown";

    //Need to show things in queue, and things in progress

    #region interface stuff 
    public void RefreshLocalFiles() => _fileStorageService.RefreshLocalFiles();
    public void RefreshRemoteFiles() => _fileStorageService.RefreshRemoteFiles();
    #endregion

    private FileStorageService _fileStorageService;

    private void OnEnable()
    {
        _fileStorageService = FileStorageServiceFactory.CreateFileStorageService(ftpNetworkSettings, $"VE2/PluginFiles/{SceneManager.GetActiveScene().name}");
        _fileStorageService.OnFileStorageServiceReady += HandleFileStorageServiceReady;
        _fileStorageService.OnRemoteFilesRefreshed += HandleRemoteFilesRefreshed;
        _fileStorageService.OnLocalFilesRefreshed += HandleLocalFilesRefreshed;

        _localPathDebug = $"Local files stored at: {_fileStorageService.LocalWorkingPath}";
    }

    private void HandleFileStorageServiceReady()
    {
        _fileStorageService.OnFileStorageServiceReady -= HandleFileStorageServiceReady;
        HandleLocalFilesRefreshed(); //Happens immediately when service is created 

        //DownloadAllFiles();
        //UploadAllFiles();
    }

    private void DownloadAllFiles() 
    {
        //_fileStorageService.DownloadFile("DevTestRoot1.txt");
        //_fileStorageService.DownloadFile("SubFolder/DevTest2.txt");
        //_fileStorageService.DownloadFile("SubFolder/SubSubFolder/DevTest3.txt");

        foreach (string fileNameAndPath in _fileStorageService.RemoteFiles.Keys)
            _fileStorageService.DownloadFile(fileNameAndPath);

    }

    private void UploadAllFiles() 
    {
        foreach (string fileNameAndPath in _fileStorageService.localFiles.Keys)
            _fileStorageService.UploadFile(fileNameAndPath);
    }


    private void HandleLocalFilesRefreshed() 
    {
        _localFilesDebug.Clear();
        foreach (var file in _fileStorageService.localFiles)
            _localFilesDebug.Add(file.Key);
    }

    private void HandleRemoteFilesRefreshed() 
    {
        _remoteFilesDebug.Clear();
        foreach (var file in _fileStorageService.RemoteFiles)
            _remoteFilesDebug.Add(file.Key);
    }

    private void OnDisable() => _fileStorageService.TearDown();
}
