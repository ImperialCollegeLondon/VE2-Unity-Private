using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VE2.PlatformNetworking;
using VE2_NonCore_FileSystem_Interfaces_Common;
using VE2_NonCore_FileSystem_Interfaces_Internal;
using VE2_NonCore_FileSystem_Interfaces_Plugin;

public class HubFileHandlerExample : MonoBehaviour
{
    [EditorButton(nameof(HandleRefreshFilesButtonClicked), "Refresh Files", activityType: ButtonActivityType.OnPlayMode, Order = -1)]
    [SerializeField] private string folderToSearch = "/";

    [SerializeField] private GameObject _fileObjectHorizontalGroupPrefab;
    [SerializeField] private VerticalLayoutGroup _fileObjectVerticalGroup;
    [SerializeField] private GameObject _fileUIObjectPrefab;

    [SerializeField] private GameObject _fileSystemGameObject;
    private IInternalFileSystem _fileSystem => _fileSystemGameObject.GetComponent<IInternalFileSystem>();

    // private List<VerticalLayoutGroup> _fileUIObjectVerticalGroups = new List<VerticalLayoutGroup>();
    // private List<FileUIObjectExample> _fileUIObjects = new List<FileUIObjectExample>();
    private List<(HorizontalLayoutGroup, List<HubFileUIObjectExample>)> _fileObjectHorizontalGroups = new();

    void OnEnable()
    {
        if (_fileSystem.IsFileSystemReady)
            HandleFileSystemReady();
        else
            _fileSystem.OnFileSystemReady += HandleFileSystemReady;
    }

    private void HandleFileSystemReady() 
    {
        _fileSystem.OnFileSystemReady -= HandleFileSystemReady;
        StartSearch();
    }

    private void StartSearch() 
    {
        IRemoteFolderSearchInfo task = _fileSystem.GetRemoteFoldersAtPath(folderToSearch);
        task.OnSearchComplete += HandleGetRemoteFolders;
    }

    private void HandleGetRemoteFolders(IRemoteFolderSearchInfo search)
    {
        //Debug.Log("Got remote folders! " + search.CompletionCode + " - " + search.FoldersFound.Count);

        V_PlatformIntegration platformIntegration = GameObject.FindObjectOfType<V_PlatformIntegration>();

        /*
            Get list of worlds from server. Foreach world, make a UI for it, that's it!
        */
        List<string> localWorlds = _fileSystem.GetLocalFoldersAtPath(folderToSearch);

        foreach (string remoteWorldFolder in search.FoldersFound)
        {
            //TODO: FOR VE2===================================================================
            //================================================================================

            //We can look here to check if we have something locally. 
            //If we know the world folder isn't there, we can tell the ui object, and save it from
            //Making a request to the server to check if it's there so it does the first run faster 
            bool isAvailableLocally = localWorlds.Contains(remoteWorldFolder);

            //================================================================================
            //TODO: FOR VE2===================================================================

            GameObject FileUIObjectGO = Instantiate(_fileUIObjectPrefab, null);
            HubFileUIObjectExample hubFileUIObject = FileUIObjectGO.GetComponent<HubFileUIObjectExample>();
            hubFileUIObject.Setup(platformIntegration, _fileSystem, remoteWorldFolder);

            (HorizontalLayoutGroup, List<HubFileUIObjectExample>) lastHorizontalGroup = _fileObjectHorizontalGroups.Count > 0 ? _fileObjectHorizontalGroups[_fileObjectHorizontalGroups.Count - 1] : (null, null);

            if (lastHorizontalGroup.Item1 == null || lastHorizontalGroup.Item2.Count == 3) //We need a new horizontal group!
            {
                //Create new horizontal group and add it to the vertical
                GameObject fileObjectHorizontalGroupGO = Instantiate(_fileObjectHorizontalGroupPrefab, transform);
                fileObjectHorizontalGroupGO.transform.SetParent(_fileObjectVerticalGroup.transform, false);

                //Add the file UI object to the new horizontal group
                HorizontalLayoutGroup horizontalLayoutGroup = fileObjectHorizontalGroupGO.GetComponent<HorizontalLayoutGroup>();
                FileUIObjectGO.transform.SetParent(horizontalLayoutGroup.transform, false);

                _fileObjectHorizontalGroups.Add((horizontalLayoutGroup, new List<HubFileUIObjectExample> { hubFileUIObject }));
            }
            else
            {
                FileUIObjectGO.transform.SetParent(lastHorizontalGroup.Item1.transform, false);
                lastHorizontalGroup.Item2.Add(hubFileUIObject);
            }

            hubFileUIObject.transform.localPosition = Vector3.zero;
            hubFileUIObject.transform.localRotation = Quaternion.identity;
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
