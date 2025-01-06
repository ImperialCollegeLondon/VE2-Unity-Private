using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Renci.SshNet;
using UnityEngine;

public static class FTPServiceFactory
{
    public static FTPService Create(ConnectionInfo connectionInfo, string remoteWorkingPath, string localWorkingPath)
    {
        SftpClient sftpClient = new(connectionInfo);
        FTPCommsHandler ftpCommsHandler = new(sftpClient);
        return new FTPService(ftpCommsHandler, remoteWorkingPath, localWorkingPath);
    }
}

public class FTPService // : IFTPService //TODO: Rename to RemoteFileService? 
{
    #region Higher-level interfaces 
    public bool IsFTPServiceReady;
    public event Action OnFTPServiceReady;

    public bool IsWorking => _taskQueue.Count > 0 || (_currentTask != null && !_currentTask.IsCompleted && !_currentTask.IsCancelled);

    public event Action OnRemoteFileListUpdated;
    public readonly Dictionary<string, FileDetails> RemoteFiles = new();

    public void RefreshRemoteFileList()
    {
        RemoteFiles.Clear();

        FindRemoteFilesInFolderAndSubFolders(""); //Root of working path
    }

    /// <summary>
    /// Queue a file for download
    /// </summary>
    /// <param name="relativePath">path should use / as separator not \</param>
    /// <param name="filename"></param>
    /// <param name="callback">callback on completion or failure</param>
    /// <returns>TasK ID (for checking status/progress, or for cancelling)</returns>
    public FTPDownloadTask DownloadFile(string relativePath, string filename)
    {
        Debug.Log("FTPService Download, relative path = " + relativePath);
        string remotePath = $"{_remoteWorkingPath}/{relativePath}";
        //remotePath = remotePath.TrimEnd('/');

        string localPath = $"{_localWorkingPath}\\{relativePath}".Replace("/", "\\");

        Debug.Log($"<color=red>Downloading file {filename} from {remotePath} to {localPath}, started with {relativePath}, local working = {_localWorkingPath}</color>");

        FTPDownloadTask task = new(_nextQueueEntryID, remotePath, localPath, filename);
        Enqueue(task);
        Debug.Log($"Enqueuing download id {task.TaskID}");
        return task;
    }

    /// <summary>
    /// Queue a file for upload
    /// </summary>
    /// <param name="remotePath">path should use / as separator not \</param>
    /// <param name="filename"></param>
    /// <param name="callback">callback on completion or failure</param>
    /// <returns>TasK ID  (for checking status/progress, or for cancelling)</returns>
    public FTPUploadTask UploadFile(string remotePath, string localPath, string filename)
    {
        remotePath = $"{_remoteWorkingPath}/{remotePath}";
        FTPUploadTask task = new(_nextQueueEntryID, remotePath, localPath, filename);
        task.OnComplete += OnUploadFileComplete;
        Enqueue(task);
        return task;
    }

    private void OnUploadFileComplete(FTPFileTransferTask task)
    {
        task.OnComplete -= OnUploadFileComplete;
        if (task.CompletionCode == FTPCompletionCode.Success)
        {
            //If we've uploaded a file, we should add it to the list of remote files
            string correctedFileNameAndPath = task.RemotePath.Replace($"{_remoteWorkingPath}/", "");
            RemoteFiles.Add(correctedFileNameAndPath, new FileDetails { fileNameAndWorkingPath = task.Name, fileSize = task.TotalFileSizeToTransfer });
            OnRemoteFileListUpdated?.Invoke();
        }
    }

    /// <summary>
    /// Queue a request for list of files in a given remote path
    /// </summary>
    /// <param name="remotePath">path should use / as separator not \</param>
    /// <param name="callback">callback on completion or failure</param>
    /// <returns>TasK ID  (for checking status)</returns>
    public FTPRemoteFileListTask GetRemoteFileList(string remotePath)
    {
        remotePath = $"{_remoteWorkingPath}/{remotePath}";
        FTPRemoteFileListTask task = new(_nextQueueEntryID, remotePath);
        Enqueue(task);
        return task;
    }

    /// <summary>
    /// Queue a request for list of subfolders in a given remote path
    /// </summary>
    /// <param name="remotePath">path should use / as separator not \</param>
    /// <param name="callback">callback on completion or failure</param>
    /// <returns>TasK ID  (for checking status)</returns>
    public FTPRemoteFolderListTask GetRemoteFolderList(string remotePath)
    {
        remotePath = $"{_remoteWorkingPath}/{remotePath}";
        Debug.Log("Get remote folder list " + remotePath);
        FTPRemoteFolderListTask task = new(_nextQueueEntryID, remotePath);
        Enqueue(task);
        return task;
    }

    /// <summary>
    /// Queue a request to delete a remote file (does not delete local file)
    /// </summary>
    /// <param name="remotePath">path should use / as separator not \</param>
    /// <param name="callback">callback on completion or failure</param>
    /// <returns>TasK ID  (for checking status)</returns>
    public FTPDeleteTask DeleteRemoteFileOrEmptyFolder(string remotePath, string filename)
    {
        remotePath = $"{_remoteWorkingPath}/{remotePath}";
        FTPDeleteTask task = new(_nextQueueEntryID, remotePath, filename);
        task.OnComplete += OnDeleteRemoteFileComplete;
        Enqueue(task);
        return task;
    }

    public void OnDeleteRemoteFileComplete(FTPRemoteTask task)
    {
        task.OnComplete -= OnDeleteRemoteFileComplete;
        if (task.CompletionCode == FTPCompletionCode.Success && RemoteFiles.ContainsKey(task.RemotePath)) 
        {
            RemoteFiles.Remove(task.RemotePath);
            OnRemoteFileListUpdated?.Invoke();
        }

        //TODO: if this was a folder, we should also delete the directory it was in automatically?
    }

    /// <summary>
    /// Queue a request to create a remote folder (does not create it locally)
    /// </summary>
    /// <param name="remotePath">path should use / as separator not \</param>
    /// <param name="callback">callback on completion or failure</param>
    /// <returns>TasK ID  (for checking status)</returns>
    public FTPMakeFolderTask MakeRemoteFolder(string remotePath, string folderName)
    {
        remotePath = $"{_remoteWorkingPath}/{remotePath}";
        FTPMakeFolderTask task = new(_nextQueueEntryID, remotePath, folderName);
        Enqueue(task);
        return task;
    }

    //for debug
    private void LogQueue() //TODO - wire this into frontend
    {
        string s = "";
        if (_currentTask != null)
            s += "Current: " + _currentTask.ToString() + "\n";
        else
            s += "Current: null\n";

        int i = 0;
        foreach (FTPTaskBase task in _taskQueue)
        {
            s += $"Queue {i++}:" + task.ToString() + "\n";
        }
        Debug.Log(s);
    }

    /// <summary>
    /// Cancel a task - will cancel any queued task, but once underway only uploads and downloads will be cancelled
    /// cancelling an underway task of another type will have no effect.
    /// </summary>
    /// <param name="taskID">The Task ID returned when task was queued</param>
    public void CancelTask(string fileNameAndPath)
    {
        //If the current task is transferring that file
        if (_currentTask.RemotePath.Replace($"{_remoteWorkingPath}/", "").Equals(fileNameAndPath))
        {
            if (_currentTask is FTPFileTransferTask exchangeTask)
                exchangeTask.Cancel();
            else 
                Debug.LogWarning($"Tried to cancel a task in progress but that task is not a file transfer task - {fileNameAndPath}");

            return;
        }

        foreach (FTPTaskBase queuedTask in _taskQueue)
        {
            if (queuedTask.RemotePath.Replace($"{_remoteWorkingPath}/", "").Equals(fileNameAndPath))
            {
                queuedTask.Cancel();
                return;
            }
        }

        Debug.LogError($"Tried to cancel a task that was not found in the queue or in progress {fileNameAndPath}");
    }

    /// <summary>
    /// Get progress of task (as a float 0-100%). Underway tasks will return at most 99%
    /// Queued will return 0%, and tasks that are not in progress (either completed or not queued) will return -1
    /// An underway task that is not an upload or download will show 0%, until it shows 100%
    /// </summary>
    /// <param name="taskID">The Task ID returned when task was queued</param>
    /// <returns></returns>
    public float GetFileTransferProgress(string fileNameAndPath)
    {
        //If the current task is transferring that file
        if (_currentTask.RemotePath.Replace($"{_remoteWorkingPath}/", "").Equals(fileNameAndPath))
        {
            if (_currentTask is FTPFileTransferTask exchangeTask)
                return Mathf.Clamp(exchangeTask.CurrentProgress * 100f, 0f, 99f); //ensure only get 100% on completion
            else
                throw new Exception($"FTP system found the requested file {fileNameAndPath}, and tried to get its progress, but it is not a file transfer task!");
        }

        foreach (FTPTaskBase queuedTask in _taskQueue)
        {
            if (queuedTask.RemotePath.Replace($"{_remoteWorkingPath}/", "").Equals(fileNameAndPath))
            {
                if (queuedTask is FTPFileTransferTask)
                    return 0;
                else
                    throw new Exception($"FTP system found the requested file {fileNameAndPath}, and tried to get its progress, but it is not a file transfer task!");
            }
        }

        return -1;
    }
    #endregion


    private int _nextQueueEntryID = 0;

    private Queue<FTPTaskBase> _taskQueue = new();
    private FTPTaskBase _currentTask = null;

    private readonly FTPCommsHandler _commsHandler;
    private readonly string _remoteWorkingPath;
    private readonly string _localWorkingPath;

    public FTPService(FTPCommsHandler commsHandler, string remoteWorkingPath, string localWorkingPath)
    {
        _commsHandler = commsHandler;
        _remoteWorkingPath = remoteWorkingPath;
        _localWorkingPath = localWorkingPath;

        _commsHandler.OnStatusChanged += HandleStatusChanged;

        RefreshRemoteFileList(); //Once this completes, service will be ready
    }

    private void FindRemoteFilesInFolderAndSubFolders(string remoteFolderPath)
    {
        Debug.Log("Searching for remote files in " + remoteFolderPath);

        FTPRemoteFolderListTask subFolderListTask = GetRemoteFolderList(remoteFolderPath);
        subFolderListTask.OnComplete += HandleGetRemoteFolderListComplete;

        FTPRemoteFileListTask fileListTask = GetRemoteFileList(remoteFolderPath);
        fileListTask.OnComplete += HandleGetRemoteFileListComplete;
    }

    private void HandleGetRemoteFolderListComplete(FTPRemoteFolderListTask folderListTask)
    {
        folderListTask.OnComplete -= HandleGetRemoteFolderListComplete;

        Debug.Log($"Done get remote folder list for {folderListTask.RemotePath} - {folderListTask.CompletionCode}");

        if (folderListTask.CompletionCode != FTPCompletionCode.Success)
        {
            Debug.LogError($"Failed to get remote folder list: {folderListTask.CompletionCode}");
            return;
        }

        foreach (string subFolder in folderListTask.FoundFolderNames) //TODO: Does this return the full folder path, like VE2/PluginFiles/WorldName/Folder1/Folder2? or does it just return Folder2?
        {
            Debug.Log("Check sub folder " + subFolder);
            string fileNameAndPath = $"{folderListTask.RemotePath}/{subFolder}";
            string workingFileNameAndPath = fileNameAndPath.Replace($"{_remoteWorkingPath}/", "");
            FindRemoteFilesInFolderAndSubFolders(workingFileNameAndPath);

            //Do we want to create a local folder for each remote folder now?
            //May as well, I suppose? 
            // if (!Directory.Exists(_localWorkingPath + "\\" + subFolder))
            // {
            //     Directory.CreateDirectory(_localWorkingPath + "\\" + subFolder);
            // }
        }
    }

    private void HandleGetRemoteFileListComplete(FTPRemoteFileListTask fileListTask)
    {
        fileListTask.OnComplete -= HandleGetRemoteFileListComplete;

        Debug.Log($"Done get remote file list for folder {fileListTask.RemotePath} - {fileListTask.CompletionCode}");

        if (fileListTask.CompletionCode != FTPCompletionCode.Success)
        {
            Debug.LogError($"Failed to get remote file list: {fileListTask.CompletionCode}");
            return;
        }

        foreach (FileDetails fileDetails in fileListTask.FoundFilesDetails)
        {
            Debug.Log("Add Remote file: " + fileDetails.fileNameAndWorkingPath + " - " + fileDetails.fileSize + " path " + fileListTask.RemotePath);
            string fileNameAndPath = $"{fileListTask.RemotePath}/{fileDetails.fileNameAndWorkingPath}"; 
            string workingFileNameAndPath = fileNameAndPath.Replace($"{_remoteWorkingPath}/", "").TrimStart('/');

            RemoteFiles.Add(workingFileNameAndPath, fileDetails); //TODO: key should be full path here - confirmed the key is just the file name, we want the full path
        }

        Debug.Log("Ready? " + IsFTPServiceReady + " Busy? " + IsWorking);

        OnRemoteFileListUpdated?.Invoke();

        //Once we've found all remote files, the service is ready 
        if (!IsFTPServiceReady && !IsWorking)
        {
            Debug.Log("File storage service is ready!");
            IsFTPServiceReady = true;
            OnFTPServiceReady?.Invoke();
        }
    }

    public void TearDown()
    {
        if (_currentTask != null) 
            _currentTask.Cancel();

        foreach (FTPTaskBase t in _taskQueue)
            _currentTask.Cancel();

        _commsHandler.OnStatusChanged -= HandleStatusChanged;
    }

    //gets called on startup, when something is added, and whenever client changes status to ready
    //if this gets buggy it could more robustly but less efficiently sit in Update()
    private void ProcessQueue() //TODO: Maybe remove task from queue when it's completed?
    {
        Debug.Log("Processing queue");
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

    private void Enqueue(FTPTaskBase task)
    {
        _taskQueue.Enqueue(task);

        ProcessQueue();
    }

    private void HandleStatusChanged(FTPStatus newStatus)
    {
        //if client is now ready - process next queue item
        if (newStatus == FTPStatus.Ready)
            ProcessQueue();
    }
}



public abstract class FTPTaskBase
{
    public bool IsCompleted { get; protected set; } = false;
    public bool IsCancelled { get; private set; } = false;
    public int TaskID { get; }
    public string RemotePath { get; }

    protected FTPTaskBase(int taskID, string remotePath)
    {
        TaskID = taskID;
        RemotePath = remotePath;
    }

    public void Cancel() => IsCancelled = true;
}

public abstract class FTPTask<TCompletedTask> : FTPTaskBase where TCompletedTask : FTPTask<TCompletedTask>
{
    public event Action<TCompletedTask> OnComplete;
    public FTPCompletionCode CompletionCode;

    protected FTPTask(int taskID, string remotePath) : base(taskID, remotePath) { }

    public void MarkCompleted(FTPCompletionCode code)
    {
        IsCompleted = true;
        CompletionCode = code;
        OnComplete?.Invoke((TCompletedTask)this);
    }
}

public abstract class FTPFileTransferTask : FTPTask<FTPFileTransferTask>
{
    public event Action<float> OnProgressChanged;
    public float CurrentProgress => _dataTransferred / TotalFileSizeToTransfer; 
    public ulong TotalFileSizeToTransfer;
    private ulong _dataTransferred;
    private ulong _lastDataTransferred;

    public readonly string LocalPath;
    public readonly string Name;

    public FTPFileTransferTask(int taskID, string remotePath, string localPath, string name) : base(taskID, remotePath) 
    {
        LocalPath = localPath;
        Name = name;
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

    public override string ToString() => $"ID:{TaskID} Name:{Name}  Rpath:{RemotePath} Lpath:{LocalPath}";
}

public class FTPDownloadTask : FTPFileTransferTask
{
    public FTPDownloadTask(int taskID, string remotePath, string localPath, string name) : base(taskID, remotePath, localPath, name) { }
}

public class FTPUploadTask : FTPFileTransferTask
{
    public FTPUploadTask(int taskID, string remotePath, string localPath, string name) : base(taskID, remotePath, localPath, name) { }
}

public class FTPRemoteFolderListTask : FTPTask<FTPRemoteFolderListTask>
{
    public List<string> FoundFolderNames;

    public FTPRemoteFolderListTask(int taskID, string remotePath) : base(taskID, remotePath) { }
}

public class FTPRemoteFileListTask : FTPTask<FTPRemoteFileListTask>
{
    public List<FileDetails> FoundFilesDetails;

    public FTPRemoteFileListTask(int taskID, string remotePath) : base(taskID, remotePath) { }
}

public abstract class FTPRemoteTask : FTPTask<FTPRemoteTask>
{
    public readonly string Name;

    public FTPRemoteTask(int taskID, string remotePath, string name) : base(taskID, remotePath)
    {
        Name = name;
    }

    public override string ToString() => $"ID:{TaskID} Name:{Name}  Rpath:{RemotePath}";
}

public class FTPMakeFolderTask : FTPRemoteTask
{
    public FTPMakeFolderTask(int taskID, string remotePath, string name) : base(taskID, remotePath, name) { }
}

public class FTPDeleteTask : FTPRemoteTask
{
    public FTPDeleteTask(int taskID, string remotePath, string name) : base(taskID, remotePath, name) { }
}