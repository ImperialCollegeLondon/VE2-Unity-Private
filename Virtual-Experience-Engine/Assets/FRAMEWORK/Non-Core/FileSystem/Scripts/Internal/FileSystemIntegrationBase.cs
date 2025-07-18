using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using VE2.NonCore.FileSystem.API;
using VE2.NonCore.Platform.API;
using static VE2.NonCore.Platform.API.PlatformPublicSerializables;
using Debug = UnityEngine.Debug;

namespace VE2.NonCore.FileSystem.Internal
{
    internal abstract class FileSystemIntegrationBase : MonoBehaviour
    {
        // [SerializeField, IgnoreParent, BeginGroup("Remote File Tasks")] private List<RemoteFileTaskInfo> _queuedTasks = new(); 
        // [EditorButton(nameof(CancelAllTasks), "Cancel All Tasks", activityType: ButtonActivityType.OnPlayMode, Order = -1)]
        // [SerializeField, IgnoreParent, EndGroup] private List<RemoteFileTaskInfo> _completedTasks = new();

        #region Interfaces 
        public bool IsFileSystemReady {get; private set;} = false;
        public event Action OnFileSystemReady;
        public abstract string RemoteWorkingPath { get; }
        public string LocalAbsoluteWorkingPath => Application.persistentDataPath + "/files/" + RemoteWorkingPath;

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

        public void Update() => _FileStorageService?.Update();

        public void OpenLocalWorkingFolder()
        {
            string path = LocalAbsoluteWorkingPath;
            path = path.Replace("/", "\\"); //Systen.IO works with backslashes
            UnityEngine.Debug.Log("Try open " + path);
            try
            {
                if (!System.IO.Directory.Exists(path))
                    System.IO.Directory.CreateDirectory(path);

                Process.Start("explorer.exe", path);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"An error occurred: {ex.Message}");
            }
        }

        protected void CreateFileSystem(ServerConnectionSettings serverSettings)
        {
            if (string.IsNullOrEmpty(serverSettings.Username) || string.IsNullOrEmpty(serverSettings.Password) || string.IsNullOrEmpty(serverSettings.ServerAddress))
            {
                Debug.LogError("Can't boot file system, invalid server settings.");
                return;
            }

            _FileStorageService = FileSystemServiceFactory.CreateFileStorageService(serverSettings, RemoteWorkingPath, LocalAbsoluteWorkingPath);

            IsFileSystemReady = true;
            try
            {
                OnFileSystemReady?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogError($"Error invoking OnFileSystemReady: {e.Message}");
            }
        }

        private void OnDisable()
        {
            IsFileSystemReady = false;
            _FileStorageService?.TearDown();
            // _queuedTasks.Clear();
            // _completedTasks.Clear();
        }

        private void CancelAllTasks()
        {
            // List<IRemoteFileTaskInfo> tasks = new(_queuedTasks);
            // foreach (RemoteFileTaskInfo task in tasks)
            //     task.CancelRemoteFileTask();
        }
    }
}