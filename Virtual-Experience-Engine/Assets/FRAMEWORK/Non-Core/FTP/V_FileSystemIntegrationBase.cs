using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEngine;
using VE2_NonCore_FileSystem_Interfaces_Common;

namespace VE2_NonCore_FileSystem
{
    public abstract class V_FileSystemIntegrationBase : MonoBehaviour
    {
        [SerializeField, SpaceArea(spaceAfter: 10)] private FTPNetworkSettings ftpNetworkSettings;

        [Title("Play mode debug"), SpaceArea(spaceBefore: 10)]
        [SerializeField, IgnoreParent, BeginGroup("Remote File Tasks")] private List<RemoteFileTaskInfo> _queuedTasks = new();
        [EditorButton(nameof(CancelAllTasks), "Cancel All Tasks", activityType: ButtonActivityType.OnPlayMode, Order = -1)]
        [SerializeField, IgnoreParent, EndGroup] private List<RemoteFileTaskInfo> _completedTasks = new();


        #region Interfaces 

        public bool IsFileSystemReady {get; private set;} = false;
        public event Action OnFileSystemReady;

        public Dictionary<string, LocalFileDetails> GetLocalFilesAtPath(string path)
        {
            Dictionary<string, LocalFileDetails> localFiles = new();

            foreach (var file in _fileStorageService.GetAllLocalFilesAtPath(path))
                localFiles.Add(file.Key, new LocalFileDetails(file.Key, file.Value.fileSize, Path.Combine(Application.persistentDataPath, LocalWorkingPath, file.Key).Replace("/", "\\")));

            return localFiles;
        }

        public List<string> GetLocalFoldersAtPath(string path)
        {
            return _fileStorageService.GetAllLocalFoldersAtPath(path);
        }

        public IRemoteFileSearchInfo GetRemoteFilesAtPath(string path)
        {
            FTPRemoteFileListTask task = _fileStorageService.GetRemoteFilesAtPath(path);
            RemoteFileSearchInfo taskInfo = new(task, path);
            return taskInfo;
        }

        public IRemoteFolderSearchInfo GetRemoteFoldersAtPath(string path)
        {
            FTPRemoteFolderListTask task = _fileStorageService.GetRemoteFoldersAtPath(path);
            RemoteFolderSearchInfo taskInfo = new(task, path);
            return taskInfo;
        }

        public IRemoteFileTaskInfo DownloadFile(string nameAndPath)
        {
            FTPDownloadTask task = _fileStorageService.DownloadFile(nameAndPath);
            RemoteFileTaskInfo taskInfo = new(task, RemoteTaskType.Download, 0, nameAndPath);
            _queuedTasks.Add(taskInfo);
            return taskInfo;
        }
        public IRemoteFileTaskInfo UploadFile(string nameAndPath)
        {
            FTPUploadTask task = _fileStorageService.UploadFile(nameAndPath);
            RemoteFileTaskInfo taskInfo = new(task, RemoteTaskType.Upload, 0, nameAndPath);
            _queuedTasks.Add(taskInfo);
            return taskInfo;
        }
        public IRemoteFileTaskInfo DeleteRemoteFile(string nameAndPath)
        {
            FTPDeleteTask task = _fileStorageService.DeleteRemoteFile(nameAndPath);
            RemoteFileTaskInfo taskInfo = new(task, RemoteTaskType.Delete, 0, nameAndPath);
            _queuedTasks.Add(taskInfo);
            return taskInfo;
        }
        public bool DeleteLocalFile(string nameAndPath) => _fileStorageService.DeleteLocalFile(nameAndPath);
        public Dictionary<string, IRemoteFileTaskInfo> GetQueuedFileTasks()
        {
            Dictionary<string, IRemoteFileTaskInfo> queuedTasks = new();
            _queuedTasks.ForEach(task => queuedTasks.Add(task.NameAndPath, task));
            return queuedTasks;
        }
        public Dictionary<string, IRemoteFileTaskInfo> GetCompletedFileTasks() 
        {
            Dictionary<string, IRemoteFileTaskInfo> completedTasks = new();
            _completedTasks.ForEach(task => completedTasks.Add(task.NameAndPath, task));
            return completedTasks;
        }
        #endregion

        private FileSystemService _fileStorageService;
        public abstract string LocalWorkingPath { get; }

        private void OnEnable()
        {
            _fileStorageService = FileSystemServiceFactory.CreateFileStorageService(ftpNetworkSettings, LocalWorkingPath);

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

        private void Update()
        {
            List<RemoteFileTaskInfo> tasksToMoveToCompleted = new();

            foreach (RemoteFileTaskInfo task in _queuedTasks)
            {
                task.Update();
                if (task.Status == RemoteFileTaskStatus.Succeeded || task.Status == RemoteFileTaskStatus.Failed || task.Status == RemoteFileTaskStatus.Cancelled)
                    tasksToMoveToCompleted.Add(task);
            }

            foreach (RemoteFileTaskInfo task in tasksToMoveToCompleted)
            {
                _queuedTasks.Remove(task);
                _completedTasks.Add(task);
            }
        }

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
            _fileStorageService.TearDown();
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