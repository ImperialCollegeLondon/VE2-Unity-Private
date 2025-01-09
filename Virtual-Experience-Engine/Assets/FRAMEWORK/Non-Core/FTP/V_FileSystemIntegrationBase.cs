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

        [Title("Play mode debug")]
        [Help("Enter play mode to view local and remote files")]

        [EditorButton(nameof(OpenLocalWorkingFolder), "Open Local Working Folder", activityType: ButtonActivityType.Everything, Order = 2)]
        [EditorButton(nameof(RefreshLocalFiles), "Refresh Local Files", activityType: ButtonActivityType.OnPlayMode, Order = -1)]
        [EditorButton(nameof(UploadAllFiles), "Upload All Files", activityType: ButtonActivityType.OnPlayMode, Order = -1)]
        [EditorButton(nameof(DeleteAllLocalFiles), "Delete All Local Files", activityType: ButtonActivityType.OnPlayMode, Order = -1)]
        [SerializeField, Disable, BeginGroup("Local Files"), EndGroup, SpaceArea(spaceBefore: 10)] private List<LocalFileDetails> _localFilesAvailable = new();


        [EditorButton(nameof(RefreshRemoteFiles), "Refresh Remote Files", activityType: ButtonActivityType.OnPlayMode, Order = -1)]
        [EditorButton(nameof(DownloadAllFiles), "Download all Files", activityType: ButtonActivityType.OnPlayMode, Order = -1)]
        [EditorButton(nameof(DeleteAllRemoteFiles), "Delete All Remote Files", activityType: ButtonActivityType.OnPlayMode, Order = -1)]
        [SerializeField, Disable, BeginGroup("Remote Files"), EndGroup, SpaceArea(spaceBefore: 10)] private List<RemoteFileDetails> _remoteFilesAvailable = new();

        [SerializeField, IgnoreParent, BeginGroup("Remote File Tasks"), SpaceArea(spaceBefore: 10)] private List<RemoteFileTaskInfo> _queuedTasks = new();
        [EditorButton(nameof(CancelAllTasks), "Cancel All Tasks", activityType: ButtonActivityType.OnPlayMode, Order = -1)]
        [SerializeField, IgnoreParent, EndGroup] private List<RemoteFileTaskInfo> _completedTasks = new();


        #region Interfaces 
        public void RefreshLocalFiles() => _fileStorageService.RefreshLocalFiles();
        public void RefreshRemoteFiles() => _fileStorageService.RefreshRemoteFiles();
        public Dictionary<string, LocalFileDetails> GetLocalFiles() 
        {
            Dictionary<string, LocalFileDetails> localFiles = new();
            _localFilesAvailable.ForEach(file => localFiles.Add(file.NameAndPath, file));
            return localFiles;
        }
        public Dictionary<string, RemoteFileDetails> GetRemoteFiles()
        {
            Dictionary<string, RemoteFileDetails> remoteFiles = new();
            _remoteFilesAvailable.ForEach(file => remoteFiles.Add(file.NameAndPath, file));
            return remoteFiles;
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
        public void DeleteLocalFile(string nameAndPath) => _fileStorageService.DeleteLocalFile(nameAndPath);
        public Dictionary<string, IRemoteFileTaskInfo> GetQueuedTasks()
        {
            Dictionary<string, IRemoteFileTaskInfo> queuedTasks = new();
            _queuedTasks.ForEach(task => queuedTasks.Add(task.NameAndPath, task));
            return queuedTasks;
        }
        public Dictionary<string, IRemoteFileTaskInfo> GetCompletedTasks() 
        {
            Dictionary<string, IRemoteFileTaskInfo> completedTasks = new();
            _completedTasks.ForEach(task => completedTasks.Add(task.NameAndPath, task));
            return completedTasks;
        }
        #endregion

        private FileSystemService _fileStorageService;
        protected abstract string _LocalWorkingFilePath { get; }

        private void OnEnable()
        {
            _fileStorageService = FileSystemServiceFactory.CreateFileStorageService(ftpNetworkSettings, _LocalWorkingFilePath);
            _fileStorageService.OnFileStorageServiceReady += HandleFileStorageServiceReady;
            _fileStorageService.OnRemoteFilesRefreshed += HandleRemoteFilesRefreshed;
            _fileStorageService.OnLocalFilesRefreshed += HandleLocalFilesRefreshed;
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

        private void HandleFileStorageServiceReady()
        {
            _fileStorageService.OnFileStorageServiceReady -= HandleFileStorageServiceReady;
            HandleLocalFilesRefreshed(); //Happens immediately when service is created 
        }

        public void OpenLocalWorkingFolder()
        {
            string path = Application.persistentDataPath + "/files/" + _LocalWorkingFilePath;
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

        private void HandleLocalFilesRefreshed()
        {
            _localFilesAvailable.Clear();
            foreach (var file in _fileStorageService.localFiles)
                _localFilesAvailable.Add(new LocalFileDetails(file.Key, file.Value.fileSize, Path.Combine(Application.persistentDataPath, _LocalWorkingFilePath, file.Key).Replace("/", "\\")));
        }

        private void HandleRemoteFilesRefreshed()
        {
            _remoteFilesAvailable.Clear();
            foreach (var file in _fileStorageService.RemoteFiles)
                _remoteFilesAvailable.Add(new RemoteFileDetails(file.Key, file.Value.fileSize));
        }

        private void OnDisable()
        {
            _fileStorageService.TearDown();
            _queuedTasks.Clear();
            _completedTasks.Clear();
        }

        #region TO-REMOVE-DEBUG //TODO:

        private void DownloadAllFiles()
        {
            foreach (string fileNameAndPath in _fileStorageService.RemoteFiles.Keys)
                DownloadFile(fileNameAndPath);
        }

        private void UploadAllFiles()
        {
            foreach (string fileNameAndPath in _fileStorageService.localFiles.Keys)
                UploadFile(fileNameAndPath);
        }

        private void DeleteAllLocalFiles()
        {
            List<string> localFileNames = new List<string>(_fileStorageService.localFiles.Keys);
            foreach (string fileNameAndPath in localFileNames)
                DeleteLocalFile(fileNameAndPath);
        }

        private void DeleteAllRemoteFiles()
        {
            List<string> remoteFileNames = new List<string>(_fileStorageService.RemoteFiles.Keys);
            foreach (string fileNameAndPath in remoteFileNames)
                DeleteRemoteFile(fileNameAndPath);
        }

        private void CancelAllTasks()
        {
            List<IRemoteFileTaskInfo> tasks = new(_queuedTasks);
            foreach (RemoteFileTaskInfo task in tasks)
                task.CancelRemoteFileTask();
        }
        #endregion
    }
}