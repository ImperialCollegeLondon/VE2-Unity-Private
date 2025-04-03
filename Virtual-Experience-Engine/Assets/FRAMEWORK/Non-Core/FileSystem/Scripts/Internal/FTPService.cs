using System;
using System.Collections.Generic;
using System.IO;
using Renci.SshNet;
using UnityEngine;

namespace VE2.NonCore.FileSystem.Internal
{
    internal static class FTPServiceFactory
    {
        public static FTPService Create(ConnectionInfo connectionInfo, string remoteWorkingPath, string localWorkingPath)
        {
            SftpClient sftpClient = new(connectionInfo);
            FTPCommsHandler ftpCommsHandler = new(sftpClient);
            return new FTPService(ftpCommsHandler, remoteWorkingPath, localWorkingPath);
        }
    }

    internal class FTPService 
    {
        #region Higher-level interfaces 

        /// <summary>
        /// Queue a request for list of files in a given remote path
        /// </summary>
        /// <param name="remotePath">path should use / as separator not \</param>
        /// <returns>Task to monitor completion</returns>
        public FTPRemoteFileListTask GetRemoteFilesAtPath(string remotePath)
        {
            remotePath = $"{_remoteWorkingPath}/{remotePath}";
            FTPRemoteFileListTask task = new(remotePath);
            Enqueue(task);
            return task;
        }

        /// <summary>
        /// Queue a request for list of subfolders in a given remote path
        /// </summary>
        /// <param name="remotePath">path should use / as separator not \</param>
        /// <returns>Task to monitor completion</returns>
        public FTPRemoteFolderListTask GetRemoteFoldersAtPath(string remotePath)
        {
            remotePath = remotePath == "" ? _remoteWorkingPath : $"{_remoteWorkingPath}/{remotePath}";
            FTPRemoteFolderListTask task = new(remotePath);
            Enqueue(task);
            return task;
        }

        /// <summary>
        /// Queue a file for download
        /// </summary>
        /// <param name="relativePath">path should use / as separator not \ if file is at root, should be empty string</param>
        /// <param name="filename"></param>
        /// <returns>Task (for checking status/progress, or for cancelling)</returns>
        public FTPDownloadTask DownloadFile(string relativePath, string filename)
        {
            string remotePath = $"{_remoteWorkingPath}/{relativePath}";
            string localPath = $"{_localWorkingPath}/{relativePath}";

            //Need to create local directory before we can download into it!
            if (!Directory.Exists(localPath))
                Directory.CreateDirectory(localPath);

            FTPDownloadTask task = new(remotePath, localPath, filename);
            task.OnComplete += OnDownloadFileComplete;

            Enqueue(task);
            return task;
        }

        private void OnDownloadFileComplete(FTPTask task)
        {
            FTPFileTransferTask downloadTask = task as FTPFileTransferTask;

            string correctedFileNameAndPath = downloadTask.RemotePath.Replace($"{_remoteWorkingPath}/", "") + "/" + downloadTask.Name;
            if (correctedFileNameAndPath.StartsWith("/")) //If starting relative path was ""
                correctedFileNameAndPath = correctedFileNameAndPath.Substring(1);

            Debug.Log($"Download {correctedFileNameAndPath} finished. Result = {downloadTask.CompletionCode}");
            downloadTask.OnComplete -= OnDownloadFileComplete;
        }

        /// <summary>
        /// Queue a file for upload
        /// </summary>
        /// <param name="relativePath">path should use / as separator not \ if file is at root, should be empty string</param>
        /// <param name="filename"></param>
        /// <returns>Task (for checking status/progress, or for cancelling)</returns>
        public FTPUploadTask UploadFile(string relativePath, string filename) //relative path may be "" or "/" _remoteWorkingPath has "sampleScene" thing in, no slash at the end. 
        {
            string remotePath = relativePath.StartsWith("/")? $"{_remoteWorkingPath}{relativePath}" : $"{_remoteWorkingPath}/{relativePath}";
            string localPath = relativePath.StartsWith("/")? $"{_localWorkingPath}{relativePath}" : $"{_localWorkingPath}/{relativePath}";

            MakeRemoteFolder(remotePath); //Queue a task to make a remote folder - if it exists, nothing will happen

            FTPUploadTask task = new(remotePath, localPath, filename);
            task.OnComplete += OnUploadFileComplete;
            Enqueue(task);
            return task;
        }

        private void OnUploadFileComplete(FTPTask task)
        {
            FTPFileTransferTask uploadTask = task as FTPFileTransferTask;
            string correctedFileNameAndPath = uploadTask.RemotePath.Replace($"{_remoteWorkingPath}/", "") + "/" + uploadTask.Name;
            if (correctedFileNameAndPath.StartsWith("/")) //If starting relative path was ""
                correctedFileNameAndPath = correctedFileNameAndPath.Substring(1);

            Debug.Log($"Upload {correctedFileNameAndPath} finished. Result = {uploadTask.CompletionCode}");

            uploadTask.OnComplete -= OnUploadFileComplete;
        }

        /// <summary>
        /// Queue a request to delete a remote file (does not delete local file)
        /// </summary>
        /// <param name="remotePath">path should use / as separator not \</param>
        /// <param name="filename"></param>
        /// <returns>Task (for checking status/progress, or for cancelling)</returns>
        public FTPDeleteTask DeleteRemoteFileOrEmptyFolder(string remotePath, string filename)
        {
            remotePath = $"{_remoteWorkingPath}/{remotePath}";
            FTPDeleteTask task = new(remotePath, filename);
            task.OnComplete += OnDeleteRemoteFileComplete;
            Enqueue(task);
            return task;
        }

        public void OnDeleteRemoteFileComplete(FTPTask task)
        {
            FTPRemoteTask deleteFileTask = task as FTPRemoteTask;
            deleteFileTask.OnComplete -= OnDeleteRemoteFileComplete;

            string correctedFileNameAndPath = task.RemotePath.Replace($"{_remoteWorkingPath}/", "") + "/" + deleteFileTask.Name;
            if (correctedFileNameAndPath.StartsWith("/")) //If starting relative path was ""
                correctedFileNameAndPath = correctedFileNameAndPath.Substring(1);

            Debug.Log($"Delete {correctedFileNameAndPath} finished. Result = {task.CompletionCode}");
        }

        /// <summary>
        /// Queue a request to create a remote folder (does not create it locally)
        /// </summary>
        /// <param name="remoteFolderPathAndName">path should use / as separator not \</param>
        /// <param name="filename"></param>
        /// <returns>Task (for checking status/progress, or for cancelling)</returns>
        public FTPMakeFolderTask MakeRemoteFolder(string remoteFolderPathAndName)
        {
            string remotePathWithoutTrailingSlash = remoteFolderPathAndName.EndsWith("/") ? remoteFolderPathAndName.Substring(0, remoteFolderPathAndName.Length - 1) : remoteFolderPathAndName;
            string newFolderName = remotePathWithoutTrailingSlash.Substring(remotePathWithoutTrailingSlash.LastIndexOf("/") + 1);
            string newFolderPath = remotePathWithoutTrailingSlash.Substring(0, remotePathWithoutTrailingSlash.LastIndexOf("/")); 

            FTPMakeFolderTask task = new(newFolderPath, newFolderName);
            Enqueue(task);
            return task;
        }
        #endregion

        private FTPTask _currentTask = null;
        private Queue<FTPTask> _taskQueue = new();

        private readonly FTPCommsHandler _commsHandler;
        private readonly string _remoteWorkingPath;
        private readonly string _localWorkingPath;

        public FTPService(FTPCommsHandler commsHandler, string remoteWorkingPath, string localWorkingPath)
        {
            _commsHandler = commsHandler;
            _remoteWorkingPath = remoteWorkingPath;
            _localWorkingPath = localWorkingPath;

            _commsHandler.OnStatusChanged += HandleStatusChanged;
        }

        //gets called on startup, when something is added, and whenever client changes status to ready
        //if this gets buggy it could more robustly but less efficiently sit in Update()
        private void ProcessQueue()
        {
            if (_commsHandler.Status == FTPStatus.Busy)
                return;

            do //keep pulling from queue until we find a task not cancelled, or nothing to do
            {
                _currentTask = null;
                if (_taskQueue.Count == 0)
                    return;
                else
                    _currentTask = _taskQueue.Dequeue();

            } while (_currentTask.IsCancelled);


            switch (_currentTask)
            {
                case FTPDownloadTask downloadTask:
                    _commsHandler.DownloadFile(downloadTask);
                    break;
                case FTPUploadTask uploadTask:
                    _commsHandler.UploadFile(uploadTask);
                    break;
                case FTPDeleteTask deleteTask:
                    _commsHandler.RemoveFileOrEmptyFolder(deleteTask);
                    break;
                case FTPRemoteFileListTask fileListTask:
                    _commsHandler.GetFileList(fileListTask);
                    break;
                case FTPRemoteFolderListTask folderListTask:
                    _commsHandler.GetFolderList(folderListTask);
                    break;
                case FTPMakeFolderTask makeFolderTask:
                    _commsHandler.MakeFolder(makeFolderTask);
                    break;
            }
        }

        private void Enqueue(FTPTask task)
        {
            _taskQueue.Enqueue(task);
            ProcessQueue();
        }

        private void HandleStatusChanged(FTPStatus newStatus)
        {
            if (newStatus == FTPStatus.Ready) //If client is ready
                ProcessQueue();
        }

        public void TearDown()
        {
            if (_currentTask != null)
                _currentTask.Cancel();

            foreach (FTPTask t in _taskQueue)
                _currentTask.Cancel();

            _commsHandler.OnStatusChanged -= HandleStatusChanged;
        }
    }

    internal abstract class FTPTask
    {
        public bool IsCompleted { get; protected set; } = false;
        public bool IsCancelled { get; private set; } = false;
        public bool IsInProgress { get; private set; } = false;
        public readonly string RemotePath;
        public FTPCompletionCode CompletionCode;

        /// <summary>
        /// Includes filename/end folder if applicable. For list operations, will be the same as RemotePath
        /// </summary>
        public abstract string RemotePathAndName { get; }
        public event Action<FTPTask> OnComplete;

        protected FTPTask(string remotePath)
        {
            RemotePath = remotePath;
        }

        public void MarkCompleted(FTPCompletionCode code)
        {
            IsCompleted = true;
            IsInProgress = false;
            CompletionCode = code;
            OnComplete?.Invoke(this);
        }

        public void MarkInProgress() => IsInProgress = true;

        public void Cancel() => IsCancelled = true;
    }

    internal abstract class FTPFileTask : FTPTask
    {
        public readonly string Name;

        public FTPFileTask(string remotePath, string name) : base(remotePath)
        {
            Name = name;
        }
    }

    internal abstract class FTPFileTransferTask : FTPFileTask
    {
        public event Action<float> OnProgressChanged;

        /// <summary>
        /// Get progress of task (as a float 0-100%). Underway tasks will return at most 99%
        /// Complete tasks will return 100%. Queued or cancelled tasks will return 0%
        /// </summary>
        public float CurrentProgress
        {
            get
            {
                if (CompletionCode == FTPCompletionCode.CouldNotConnect || CompletionCode == FTPCompletionCode.LocalFileError || CompletionCode == FTPCompletionCode.RemoteFileError || CompletionCode == FTPCompletionCode.Cancelled)
                    return 0;
                else if (IsCompleted)
                    return 1;
                else if (IsCancelled)
                    return 0;
                else
                    return _dataTransferred / Mathf.Max(TotalFileSizeToTransfer, 1);
            }
        }
        public ulong TotalFileSizeToTransfer;
        private ulong _dataTransferred;
        private ulong _lastDataTransferred;

        public override string RemotePathAndName => RemotePath.EndsWith('/') ? $"{RemotePath}{Name}" : $"{RemotePath}/{Name}";

        public readonly string LocalPath;

        public FTPFileTransferTask(string remotePath, string localPath, string name) : base(remotePath, name)
        {
            LocalPath = localPath;
        }

        /// <summary>
        /// This is called by SSH system frequently (who knows how often - but too often I think) during transfers
        /// This handles cancel mechanism, and sporadic callbacks through interface to framework
        /// </summary>
        public void SetProgress(ulong dataTransferred)
        {
            _dataTransferred = dataTransferred;

            if (IsCancelled)
            {
                //Will go back up the chain to the CommsHandler, which will handle the cancellation
                throw new Exception($"File operation cannot complete - it was cancelled! {ToString()}");
            }
            else if (_lastDataTransferred == 0 || (dataTransferred - _lastDataTransferred) > 5000) //Emit event on first call, and ever 5k afterwards
            {
                _lastDataTransferred = dataTransferred;
                OnProgressChanged?.Invoke(CurrentProgress);
            }
        }

        public override string ToString() => $"Name:{Name}  Rpath:{RemotePath} Lpath:{LocalPath}";
    }

    internal class FTPDownloadTask : FTPFileTransferTask
    {
        public FTPDownloadTask(string remotePath, string localPath, string name) : base(remotePath, localPath, name) { }
    }

    internal class FTPUploadTask : FTPFileTransferTask
    {
        public List<string> FoldersToCheck;
        public FTPUploadTask(string remotePath, string localPath, string name) : base(remotePath, localPath, name) { }
    }

    internal class FTPRemoteFolderListTask : FTPTask
    {
        public override string RemotePathAndName => RemotePath;
        public List<string> FoundFolderNames = new();

        public FTPRemoteFolderListTask(string remotePath) : base(remotePath) { }
    }

    internal class FTPRemoteFileListTask : FTPTask
    {
        public override string RemotePathAndName => RemotePath;
        public List<FileDetails> FoundFilesDetails = new();

        public FTPRemoteFileListTask(string remotePath) : base(remotePath) { }
    }

    internal abstract class FTPRemoteTask : FTPFileTask
    {
        public override string RemotePathAndName => RemotePath.EndsWith('/') ? $"{RemotePath}{Name}" : $"{RemotePath}/{Name}";

        public FTPRemoteTask(string remotePath, string name) : base(remotePath, name) { }

        public override string ToString() => $"Name:{Name}  Rpath:{RemotePath}";
    }

    internal class FTPMakeFolderTask : FTPRemoteTask
    {
        public FTPMakeFolderTask(string remotePath, string name) : base(remotePath, name) { }
    }

    internal class FTPDeleteTask : FTPRemoteTask
    {
        public FTPDeleteTask(string remotePath, string name) : base(remotePath, name) { }
    }
}