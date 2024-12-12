using UnityEngine;
using UnityEngine.SceneManagement;

public class V_PluginFileStorage : MonoBehaviour
{
    [SerializeField] private FTPNetworkSettings ftpNetworkSettings;


    private FileStorageService _fileStorageService;

    private void OnEnable()
    {
        _fileStorageService = FileStorageServiceFactory.CreateFileStorageService(ftpNetworkSettings, $"PluginFiles/{SceneManager.GetActiveScene().name}");
        _fileStorageService.OnFileStorageServiceReady += HandleFileStorageServiceReady;
    }

    private void HandleFileStorageServiceReady()
    {
        _fileStorageService.OnFileStorageServiceReady -= HandleFileStorageServiceReady;

        Debug.Log("FOUND REMOTE FILES... " + _fileStorageService.remoteFiles.Count);
        foreach (var file in _fileStorageService.remoteFiles)
        {
            Debug.Log("Remote file: " + file.Key + " - " + file.Value.fileName + " - " + file.Value.fileSize);
        }
    }

    private void OnDisable() => _fileStorageService.TearDown();
}
