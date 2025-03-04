using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEngine;
using VE2.NonCore.FileSystem.API;
using static VE2.NonCore.Platform.API.PlatformPublicSerializables;

namespace VE2.NonCore.FileSystem.Internal
{
    internal abstract class V_FileSystemIntegrationBase : MonoBehaviour
    {
        [SerializeField, SpaceArea(spaceAfter: 10)] private ServerConnectionSettings ftpNetworkSettings;


        [SerializeField, IgnoreParent, BeginGroup("Remote File Tasks")] private List<RemoteFileTaskInfo> _queuedTasks = new(); 
        [EditorButton(nameof(CancelAllTasks), "Cancel All Tasks", activityType: ButtonActivityType.OnPlayMode, Order = -1)]
        [EditorButton(nameof(OpenLocalWorkingFolder), "Open Local Working Folder", activityType: ButtonActivityType.Everything, Order = -2)]
        [SerializeField, IgnoreParent, EndGroup] private List<RemoteFileTaskInfo> _completedTasks = new();


        #region Interfaces 
        public bool IsFileSystemReady {get; private set;} = false;
        public event Action OnFileSystemReady;
        public abstract string LocalWorkingPath { get; }

        public Dictionary<string, LocalFileDetails> GetLocalFilesAtPath(string path) => _FileStorageService.GetLocalFilesAtPath(path);
        public List<string> GetLocalFoldersAtPath(string path) => _FileStorageService.GetLocalFoldersAtPath(path);

        public IRemoteFileSearchInfo GetRemoteFilesAtPath(string path) => _FileStorageService.GetRemoteFilesAtPath(path);
        public IRemoteFolderSearchInfo GetRemoteFoldersAtPath(string path) => _FileStorageService.GetRemoteFoldersAtPath(path);

        public IRemoteFileTaskInfo DownloadFile(string nameAndPath) => _FileStorageService.DownloadFile(nameAndPath);
        public IRemoteFileTaskInfo UploadFile(string nameAndPath) => _FileStorageService.UploadFile(nameAndPath);

        public IRemoteFileTaskInfo DeleteRemoteFile(string nameAndPath) => _FileStorageService.DeleteRemoteFile(nameAndPath);
        public bool DeleteLocalFile(string nameAndPath) => _FileStorageService.DeleteLocalFile(nameAndPath);

        public List<IRemoteFileTaskInfo> GetQueuedFileTasks() => _FileStorageService.GetQueuedFileTasks();
        public List<IRemoteFileTaskInfo> GetCompletedFileTasks() => _FileStorageService.GetCompletedFileTasks();
        #endregion

        internal FileSystemService _FileStorageService; 

        private void OnEnable()
        {
            _FileStorageService = FileSystemServiceFactory.CreateFileStorageService(ftpNetworkSettings, LocalWorkingPath);

            IsFileSystemReady = true;
            try
            {
                OnFileSystemReady?.Invoke();
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"Error invoking OnFileSystemReady: {e.Message}");
            }
        }

        public void Update() => _FileStorageService.Update();

        public void OpenLocalWorkingFolder()
        {
            string path = Application.persistentDataPath + "/files/" + LocalWorkingPath;
            path = path.Replace("/", "\\"); //Systen.IO works with backslashes
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
                    UnityEngine.Debug.LogError("The specified path does not exist.");
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"An error occurred: {ex.Message}");
            }
        }

        private void OnDisable()
        {
            IsFileSystemReady = false;
            _FileStorageService.TearDown();
            _queuedTasks.Clear();
            _completedTasks.Clear();
        }

        private void CancelAllTasks()
        {
            List<IRemoteFileTaskInfo> tasks = new(_queuedTasks);
            foreach (RemoteFileTaskInfo task in tasks)
                task.CancelRemoteFileTask();
        }
    }
}