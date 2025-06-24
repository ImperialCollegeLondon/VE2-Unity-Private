using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VE2.NonCore.FileSystem.API;

using FileDetails = VE2.NonCore.FileSystem.API;

namespace VE2.NonCore.FileSystem.Internal
{
    internal class FileHandlerExample : MonoBehaviour
    {
        [EditorButton(nameof(HandleRefreshFilesButtonClicked), "Refresh Files", activityType: ButtonActivityType.OnPlayMode, Order = -1)]
        [SerializeField] private string folderToSearch = "/";

        [SerializeField] private GameObject _loadingPanel;
        [SerializeField] private GameObject _fileObjectHorizontalGroupPrefab;
        [SerializeField] private VerticalLayoutGroup _fileObjectVerticalGroup;
        [SerializeField] private GameObject _fileUIObjectPrefab;

        [SerializeField] private GameObject _fileSystemGameObject;
        private IV_FileSystem _pluginFileSystem => _fileSystemGameObject.GetComponent<IV_FileSystem>();

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

        private void HandleFileSystemReady() 
        {
            _pluginFileSystem.OnFileSystemReady -= HandleFileSystemReady;
            StartSearch();
        }

        private void StartSearch() 
        {
            IRemoteFileSearchInfo task = _pluginFileSystem.GetRemoteFilesAtPath(folderToSearch);
            task.OnSearchComplete += OnGetRemoteFiles;
        }

        private void OnGetRemoteFiles(IRemoteFileSearchInfo search)
        {
            _loadingPanel.SetActive(false);

            Dictionary<string, RemoteFileDetails> remoteFiles = search.FilesFound;
            Dictionary<string, LocalFileDetails> localFiles = _pluginFileSystem.GetLocalFilesAtPath(folderToSearch);

            Dictionary<string, API.FileDetails> allFiles = new Dictionary<string, API.FileDetails>();
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

        public void HandleRefreshFilesButtonClicked()
        {
            foreach (var group in _fileObjectHorizontalGroups)
                    Destroy(group.Item1.gameObject);

            _fileObjectHorizontalGroups.Clear();

            StartSearch();
        }
    }
}
