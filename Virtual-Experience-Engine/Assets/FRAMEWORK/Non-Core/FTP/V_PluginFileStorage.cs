using UnityEngine;

public class V_PluginFileStorage : MonoBehaviour
{
    [SerializeField] private FTPNetworkSettings ftpNetworkSettings;


    private FileStorageService _fileStorageService;

    private void OnEnable()
    {
        _fileStorageService = FileStorageServiceFactory.CreateFileStorageService(ftpNetworkSettings, "/WorldFiles/Seismics");
        _fileStorageService.OnFileStorageServiceReady += HandleFileStorageServiceReady;
    }

    private void OnDisable() => _fileStorageService.TearDown();

    private void HandleFileStorageServiceReady()
    {
        _fileStorageService.OnFileStorageServiceReady -= HandleFileStorageServiceReady;

        Debug.Log("FOUND REMOTE FILES... " + _fileStorageService.remoteFiles.Count);
        foreach (var file in _fileStorageService.remoteFiles)
        {
            Debug.Log("Remote file: " + file.Key + " - " + file.Value.fileName + " - " + file.Value.fileSize);
        }
    }
}
