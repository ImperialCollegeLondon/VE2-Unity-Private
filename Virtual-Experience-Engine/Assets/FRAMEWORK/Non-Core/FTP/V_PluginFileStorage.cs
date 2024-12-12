using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class V_PluginFileStorage : MonoBehaviour
{
    [SerializeField] private FTPNetworkSettings ftpNetworkSettings;
    [SerializeField, Disable] private List<string> _remoteFilesDebug = new();

    //Need to show things in queue, and things in progress

    private FileStorageService _fileStorageService;

    private void OnEnable()
    {
        _fileStorageService = FileStorageServiceFactory.CreateFileStorageService(ftpNetworkSettings, $"VE2/PluginFiles/{SceneManager.GetActiveScene().name}");
        _fileStorageService.OnFileStorageServiceReady += HandleFileStorageServiceReady;
    }

    private void HandleFileStorageServiceReady()
    {
        _fileStorageService.OnFileStorageServiceReady -= HandleFileStorageServiceReady;

        Debug.Log("FOUND REMOTE FILES... " + _fileStorageService.RemoteFiles.Count);
        foreach (var file in _fileStorageService.RemoteFiles)
        {
            Debug.Log("Remote file: " + file.Key + " - " + file.Value.fileName + " - " + file.Value.fileSize);
            _remoteFilesDebug.Add(file.Key);
        }
    }

    private void OnDisable() => _fileStorageService.TearDown();
}
