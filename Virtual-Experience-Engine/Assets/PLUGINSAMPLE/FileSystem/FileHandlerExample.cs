using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VE2_NonCore_FileSystem_Interfaces_Common;
using VE2_NonCore_FileSystem_Interfaces_Plugin;

public class FileHandlerExample : MonoBehaviour
{
    [SerializeField] private GameObject _fileObjectHorizontalGroupPrefab;
    [SerializeField] private VerticalLayoutGroup _fileObjectVerticalGroup;
    [SerializeField] private GameObject _fileUIObjectPrefab;

    [SerializeField] private GameObject _fileSystemGameObject;
    private IPluginFileSystem _pluginFileSystem => _fileSystemGameObject.GetComponent<IPluginFileSystem>();

    // private List<VerticalLayoutGroup> _fileUIObjectVerticalGroups = new List<VerticalLayoutGroup>();
    // private List<FileUIObjectExample> _fileUIObjects = new List<FileUIObjectExample>();
    private List<(HorizontalLayoutGroup, List<FileUIObjectExample>)> _fileObjectHorizontalGroups = new();

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

        Dictionary<string, LocalFileDetails> localFiles = _pluginFileSystem.GetLocalFiles();
        Dictionary<string, RemoteFileDetails> remoteFiles = _pluginFileSystem.GetRemoteFiles();

        Dictionary<string, FileDetails> allFiles = new Dictionary<string, FileDetails>();
        foreach (var file in localFiles)
            allFiles.Add(file.Key, file.Value);

        foreach (var file in remoteFiles)
            if (!allFiles.ContainsKey(file.Key))
                allFiles.Add(file.Key, file.Value);

        foreach (var file in allFiles)
        {
            bool isAvailableLocally = localFiles.ContainsKey(file.Key);
            bool isAvailableRemotely = remoteFiles.ContainsKey(file.Key);

            GameObject FileUIObjectGO = Instantiate(_fileUIObjectPrefab, null);
            FileUIObjectExample fileUIObject = FileUIObjectGO.GetComponent<FileUIObjectExample>();
            fileUIObject.Setup(_pluginFileSystem, file.Value, isAvailableLocally, isAvailableRemotely);

            (HorizontalLayoutGroup, List<FileUIObjectExample>) lastHorizontalGroup = _fileObjectHorizontalGroups.Count > 0 ? _fileObjectHorizontalGroups[_fileObjectHorizontalGroups.Count - 1] : (null, null);

            if (lastHorizontalGroup.Item1 == null || lastHorizontalGroup.Item2.Count == 3) //We need a new horizontal group!
            {
                //Create new horizontal group and add it to the vertical
                GameObject fileObjectHorizontalGroupGO = Instantiate(_fileObjectHorizontalGroupPrefab, transform);
                fileObjectHorizontalGroupGO.transform.SetParent(_fileObjectVerticalGroup.transform, false);

                //Add the file UI object to the new horizontal group
                HorizontalLayoutGroup horizontalLayoutGroup = fileObjectHorizontalGroupGO.GetComponent<HorizontalLayoutGroup>();
                FileUIObjectGO.transform.SetParent(horizontalLayoutGroup.transform, false);

                _fileObjectHorizontalGroups.Add((horizontalLayoutGroup, new List<FileUIObjectExample> { fileUIObject }));
            }
            else
            {
                FileUIObjectGO.transform.SetParent(lastHorizontalGroup.Item1.transform, false);
                lastHorizontalGroup.Item2.Add(fileUIObject);
            }

            fileUIObject.transform.localPosition = Vector3.zero;
            fileUIObject.transform.localRotation = Quaternion.identity;
        }
    }

    public void RefreshLocalFiles() 
    {
        //TODO:
    }

    public void RefreshRemoteFiles() 
    {
        //TODO: 
    }
}
