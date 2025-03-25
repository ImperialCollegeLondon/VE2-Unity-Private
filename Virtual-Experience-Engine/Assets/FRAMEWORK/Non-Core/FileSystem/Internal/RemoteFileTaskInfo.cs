using System;
using System.Collections.Generic;
using UnityEngine;
using VE2.NonCore.FileSystem.API;


namespace VE2.NonCore.FileSystem.Internal
{
    [Serializable]
    internal class RemoteFileTaskInfo : IRemoteFileTaskInfo
    {
        [BeginHorizontal(ControlFieldWidth = false), SerializeField, LabelWidth(50), Disable] private RemoteTaskType _type;
        public RemoteTaskType Type => _type;

        [SerializeField, LabelWidth(110f), Disable] private string _nameAndPath; //Relative to working path
        public string NameAndPath => _nameAndPath;

        [SerializeField, LabelWidth(75), Disable] private float _progress;
        public float Progress => _progress;

        [EndHorizontal, SerializeField, LabelWidth(55), Disable] private RemoteFileTaskStatus _status;
        public RemoteFileTaskStatus Status => _status;

        public event Action<RemoteFileTaskStatus> OnStatusChanged;
        public event Action<IRemoteFileTaskInfo> OnTaskCompleted;

        //Once underway, only uploads and downloads can be cancelled.
        public bool IsCancellable => (_status == RemoteFileTaskStatus.InProgress && _type != RemoteTaskType.Delete) || _status == RemoteFileTaskStatus.Queued;

        private readonly FTPFileTask _task;

        public RemoteFileTaskInfo(FTPFileTask task, RemoteTaskType type, float progress, string nameAndPath)
        {
            _task = task;
            _type = type;
            _nameAndPath = nameAndPath;
            _progress = progress;
        }

        public void CancelRemoteFileTask()
        {
            if (IsCancellable)
            {
                _task.Cancel();
                Update();
            }
            else
            {
                Debug.LogWarning($"Task cannot be cancelled: {_nameAndPath} - cannot cancel a delete task that is already in progress");
            }
        }

        //Why have an explicit update method rather than just having progress/status be properties that point towards the task directly?
        //Because we want to be able to show the progress/status in the inspector, which means we need a field that gets updated every frame
        public void Update()
        {
            RemoteFileTaskStatus previousStatus = Status;
            RemoteFileTaskStatus newStatus;

            if (_task.IsCancelled)
                newStatus = RemoteFileTaskStatus.Cancelled;
            else if (_task.IsInProgress)
                newStatus = RemoteFileTaskStatus.InProgress;
            else if (_task.CompletionCode == FTPCompletionCode.Waiting)
                newStatus = RemoteFileTaskStatus.Queued;
            else if (_task.CompletionCode == FTPCompletionCode.Success)
                newStatus = RemoteFileTaskStatus.Succeeded;
            else
                newStatus = RemoteFileTaskStatus.Failed;

            _status = newStatus;

            if (_status == RemoteFileTaskStatus.Succeeded)
                _progress = 1;
            else if (_status == RemoteFileTaskStatus.Queued)
                _progress = 0;
            else if (_status == RemoteFileTaskStatus.Failed)
                _progress = 0;
            else if (_task is FTPFileTransferTask transferTask)
                _progress = transferTask.CurrentProgress; //Delete tasks don't have progress
            else
                _progress = 0; //Cancelled or delete task

            if (previousStatus != newStatus)
            {
                try
                {
                    OnStatusChanged?.Invoke(_status);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error invoking task status changed event: {e.Message}");
                }

                if (newStatus == RemoteFileTaskStatus.Succeeded || newStatus == RemoteFileTaskStatus.Failed)
                {
                    try
                    {
                        OnTaskCompleted?.Invoke(this);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Error invoking task completed event: {e.Message}");
                    }
                }
            }
        }
    }

    [Serializable]
    internal class RemoteFileSearchInfo : IRemoteFileSearchInfo
    {
        [SerializeField, LabelWidth(110f), Disable] private string _searchPath; //Relative to working path
        public string Path => _searchPath;

        public Dictionary<string, RemoteFileDetails> FilesFound { get; } = new();
        public event Action<IRemoteFileSearchInfo> OnSearchComplete;

        private readonly FTPRemoteFileListTask _task;

        public RemoteFileSearchInfo(FTPRemoteFileListTask task, string path)
        {
            _task = task;
            _searchPath = path;
            _task.OnComplete += OnTaskComplete;
        }

        private void OnTaskComplete(FTPTask task) 
        {
            if (_task.FoundFilesDetails != null)
            {
                foreach (FileDetails file in _task.FoundFilesDetails)
                {
                    string fileFoundPath = _searchPath == "/" || _searchPath == "" ? file.fileName : $"{_searchPath}/{file.fileName}";

                    RemoteFileDetails remoteFile = new(fileFoundPath, file.fileSize);
                    FilesFound.Add(fileFoundPath, remoteFile);
                }
            }

            try
            {
                OnSearchComplete?.Invoke(this);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error invoking task completed event: {e.Message}");
            }
        }
    }

    [Serializable]
    internal class RemoteFolderSearchInfo : IRemoteFolderSearchInfo
    {
        [SerializeField, LabelWidth(110f), Disable] private string _searchPath; //Relative to working path
        public string Path => _searchPath;

        public List<string> FoldersFound => _task.FoundFolderNames;
        public event Action<IRemoteFolderSearchInfo> OnSearchComplete;

        public string CompletionCode => _task.CompletionCode.ToString();

        private readonly FTPRemoteFolderListTask _task;

        public RemoteFolderSearchInfo(FTPRemoteFolderListTask task, string nameAndPath)
        {
            _task = task;
            _searchPath = nameAndPath;
            _task.OnComplete += OnTaskComplete;
        }

        private void OnTaskComplete(FTPTask task)
        {
            try
            {
                OnSearchComplete?.Invoke(this);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error invoking task completed event: {e.Message} -  {e.StackTrace}");
            }
        }
    }
}