using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VE2_NonCore_FileSystem_Interfaces_Common;
using VE2_NonCore_FileSystem_Interfaces_Plugin;

public class FileHandlerExample : MonoBehaviour
{
    [SerializeField] private GameObject _fileSystemGameObject;
    private IPluginFileSystem _pluginFileSystem => _fileSystemGameObject.GetComponent<IPluginFileSystem>();

    void OnEnable()
    {
        if (_pluginFileSystem.IsFileSystemReady)
            HandleFileSystemReady();
        else
            _pluginFileSystem.OnFileSystemReady += HandleFileSystemReady;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void HandleFileSystemReady() 
    {
        _pluginFileSystem.OnFileSystemReady -= HandleFileSystemReady;
    }
}
