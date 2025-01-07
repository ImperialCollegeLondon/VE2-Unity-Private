using System;
using System.Collections.Generic;
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

//TODO: Needs some reworking, lots of tomfoolery with the path handling, have definetely overcomplicated it!
public class FTPService // : IFTPService //TODO: Rename to RemoteFileService? TODO: review summaries, sigs have now changed 
{
    #region Higher-level interfaces 
    public bool IsFTPServiceReady;
    public event Action OnFTPServiceReady;

    public event Action OnRemoteFileListUpdated;
    public readonly Dictionary<string, FileDetails> RemoteFiles = new();

    public void RefreshRemoteFileList()
    {
        RemoteFiles.Clear();
        _remoteFolders.Clear();
        _remoteFolders.Add(""); //Root of working path

        FindRemoteFilesInFolderAndSubFolders(""); //Root of working path
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
        string localPath = $"{_localWorkingPath}\\{relativePath}".Replace("/", "\\");

        FTPDownloadTask task = new(_nextQueueEntryID, remotePath, localPath, filename);
        task.OnComplete += OnDownloadFileComplete;

        Enqueue(task);
        return task;
    }

    private void OnDownloadFileComplete(FTPFileTransferTask task)
    {
        string correctedFileNameAndPath = task.RemotePath.Replace($"{_remoteWorkingPath}/", "") + "/" + task.Name;
        if (correctedFileNameAndPath.StartsWith("/")) //If starting relative path was ""
            correctedFileNameAndPath = correctedFileNameAndPath.Substring(1);

        Debug.Log($"Download {correctedFileNameAndPath} finished. Result = {task.CompletionCode}");
        task.OnComplete -= OnDownloadFileComplete;
    }

    /// <summary>
    /// Queue a file for upload
    /// </summary>
    /// <param name="relativePath">path should use / as separator not \ if file is at root, should be empty string</param>
    /// <param name="filename"></param>
    /// <returns>Task (for checking status/progress, or for cancelling)</returns>
    public FTPUploadTask UploadFile(string relativePath, string filename)
    {
        //Need to check if remote folder exists, if not, create it before uploading file 
        if (!_remoteFolders.Contains(relativePath))
        {
            string pathToFolder = relativePath.Contains("/") ? relativePath.Substring(0, relativePath.LastIndexOf("/")) : "";
            string folderName = relativePath.Contains("/") ? relativePath.Substring(relativePath.LastIndexOf("/") + 1) : relativePath; 

            MakeRemoteFolder(pathToFolder, folderName); //Queue a task to make a remote folder
        }

        string remotePath = $"{_remoteWorkingPath}/{relativePath}";
        string localPath = $"{_localWorkingPath}\\{relativePath}".Replace("/", "\\");

        FTPUploadTask task = new(_nextQueueEntryID, remotePath, localPath, filename);
        task.OnComplete += OnUploadFileComplete;
        Enqueue(task);
        return task;
    }

    private void OnUploadFileComplete(FTPFileTransferTask task)
    {
        string correctedFileNameAndPath = task.RemotePath.Replace($"{_remoteWorkingPath}/", "") + "/" + task.Name;
        if (correctedFileNameAndPath.StartsWith("/")) //If starting relative path was ""
            correctedFileNameAndPath = correctedFileNameAndPath.Substring(1);

        Debug.Log($"Upload {correctedFileNameAndPath} finished. Result = {task.CompletionCode}");

        task.OnComplete -= OnUploadFileComplete;
        if (task.CompletionCode == FTPCompletionCode.Success)
        {
            //If we've uploaded a file, we should add it to the list of remote files
            if (!RemoteFiles.ContainsKey(correctedFileNameAndPath))
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
        remotePath = remotePath == "" ? _remoteWorkingPath : $"{_remoteWorkingPath}/{remotePath}";
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

        string correctedFileNameAndPath = task.RemotePath.Replace($"{_remoteWorkingPath}/", "") + "/" + task.Name;
        if (correctedFileNameAndPath.StartsWith("/")) //If starting relative path was ""
            correctedFileNameAndPath = correctedFileNameAndPath.Substring(1);

        Debug.Log($"Delete {correctedFileNameAndPath} finished. Result = {task.CompletionCode}");

        task.OnComplete -= OnDeleteRemoteFileComplete;
        if (task.CompletionCode == FTPCompletionCode.Success && RemoteFiles.ContainsKey(correctedFileNameAndPath)) 
        {
            RemoteFiles.Remove(correctedFileNameAndPath);
            OnRemoteFileListUpdated?.Invoke();
        }
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
        Debug.Log($"Creating remote directory at {remotePath}/{folderName}");
        FTPMakeFolderTask task = new(_nextQueueEntryID, remotePath, folderName);
        task.OnComplete += OnRemoteFolderMade;
        Enqueue(task);
        return task;
    }

    private void OnRemoteFolderMade(FTPRemoteTask task)
    {
        task.OnComplete -= OnRemoteFolderMade;

        if (task.CompletionCode == FTPCompletionCode.Success)
            _remoteFolders.Add(task.RemotePath);
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
    /// <param name="relativePath">The path and name of the file relative to the working directory</param>
    public void CancelTask(string relativePath) 
    {
        string relativeRemotePath = relativePath.Replace("\\", "/");
        if (relativeRemotePath.StartsWith("/")) //If starting relative path was ""
            relativeRemotePath = relativeRemotePath.Substring(1);

        if (_currentTask.FullRemotePath.Replace($"{_remoteWorkingPath}/", "").Equals(relativeRemotePath))
        {
            //If the current task is transferring that file
            if (_currentTask is FTPFileTransferTask exchangeTask)
            {
                Debug.Log($"Cancelling task in progress: {relativePath}");
                exchangeTask.Cancel();
            }
            else
            {
                Debug.LogWarning($"Tried to cancel a task in progress but that task is not a file transfer task - {relativePath}");
            }

            return;
        }

        foreach (FTPTaskBase queuedTask in _taskQueue)
        {
            if (queuedTask.FullRemotePath.Replace($"{_remoteWorkingPath}/", "").Equals(relativeRemotePath))
            {
                Debug.Log($"Cancelling task in progress: {relativePath}");
                queuedTask.Cancel();
                return;
            }
        }

        Debug.LogError($"Tried to cancel a task that was not found in the queue or in progress {relativePath}");
    }

    /// <summary>
    /// Get progress of task (as a float 0-100%). Underway tasks will return at most 99%
    /// Queued will return 0%, and tasks that are not in progress (either completed or not queued) will return -1
    /// An underway task that is not an upload or download will show 0%, until it shows 100%
    /// </summary>
    /// <param name="taskID">The Task ID returned when task was queued</param>
    /// <returns></returns>
    public float GetFileTransferProgress(string fileNameAndPath) //TODO:
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

    /// <summary>
    /// Only shows transfer or delete tasks that are in progress or upcoming
    /// </summary>
    /// <returns></returns>
    public List<RemoteFileTaskDetails> GetAllUpcomingFileTransferDetails()
    {
        List<RemoteFileTaskDetails> details = new();

        if (_currentTask == null)
            return details;

        List<FTPTaskBase> currentAndFutureTasks = new() { _currentTask };
        foreach (FTPTaskBase ftpTask in _taskQueue)
            currentAndFutureTasks.Add(ftpTask);

        foreach (FTPTaskBase ftpTask in currentAndFutureTasks)
        {
            if (ftpTask.IsCancelled)
                continue;

            if (ftpTask is FTPDownloadTask downloadTask)
                details.Add(new RemoteFileTaskDetails("Download", downloadTask.CurrentProgress, downloadTask.FullRemotePath.Replace($"{_remoteWorkingPath}/", "").Replace("/", "\\")));
            else if (ftpTask is FTPUploadTask uploadTask)
                details.Add(new RemoteFileTaskDetails("Upload", uploadTask.CurrentProgress, uploadTask.FullRemotePath.Replace($"{_remoteWorkingPath}/", "")));
            else if (ftpTask is FTPDeleteTask deleteTask)
                details.Add(new RemoteFileTaskDetails("Delete", 0, deleteTask.FullRemotePath.Replace($"{_remoteWorkingPath}/", "")));
        }

        return details;
    }
    #endregion

    private FTPTaskBase _currentTask = null;
    private Queue<FTPTaskBase> _taskQueue = new();
    private int _nextQueueEntryID = 0;
    private bool IsWorking => _taskQueue.Count > 0 || (_currentTask != null && !_currentTask.IsCompleted && !_currentTask.IsCancelled);
    private List<string> _remoteFolders = new();

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
        FTPRemoteFolderListTask subFolderListTask = GetRemoteFolderList(remoteFolderPath);
        subFolderListTask.OnComplete += HandleGetRemoteFolderListComplete;

        FTPRemoteFileListTask fileListTask = GetRemoteFileList(remoteFolderPath);
        fileListTask.OnComplete += HandleGetRemoteFileListComplete;
    }

    private void HandleGetRemoteFolderListComplete(FTPRemoteFolderListTask folderListTask)
    {
        folderListTask.OnComplete -= HandleGetRemoteFolderListComplete;

        if (folderListTask.CompletionCode != FTPCompletionCode.Success)
        {
            Debug.LogError($"Failed to get remote folder list: {folderListTask.CompletionCode}");
            return;
        }

        foreach (string subFolder in folderListTask.FoundFolderNames)
        {
            _remoteFolders.Add(subFolder);
            string fileNameAndPath = $"{folderListTask.RemotePath}/{subFolder}"; 
            string workingFileNameAndPath = fileNameAndPath.Replace($"{_remoteWorkingPath}/", "");
            FindRemoteFilesInFolderAndSubFolders(workingFileNameAndPath);
        }
    }

    private void HandleGetRemoteFileListComplete(FTPRemoteFileListTask fileListTask)
    {
        fileListTask.OnComplete -= HandleGetRemoteFileListComplete;

        if (fileListTask.CompletionCode != FTPCompletionCode.Success)
        {
            Debug.LogError($"Failed to get remote file list: {fileListTask.CompletionCode}");
            return;
        }

        foreach (FileDetails fileDetails in fileListTask.FoundFilesDetails)
        {
            string fileNameAndPath = $"{fileListTask.RemotePath}/{fileDetails.fileNameAndWorkingPath}"; 
            string workingFileNameAndPath = fileNameAndPath.Replace($"{_remoteWorkingPath}/", "").TrimStart('/');
            RemoteFiles.Add(workingFileNameAndPath, fileDetails); 
        }

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

    private void Enqueue(FTPTaskBase task)
    {
        _taskQueue.Enqueue(task);
        ProcessQueue();
    }

    private void HandleStatusChanged(FTPStatus newStatus)
    {
        if (newStatus == FTPStatus.Ready) //If client is ready
            ProcessQueue();
    }
}



public abstract class FTPTaskBase
{
    public bool IsCompleted { get; protected set; } = false;
    public bool IsCancelled { get; private set; } = false;
    public int TaskID { get; }
    public string RemotePath { get; }

    /// <summary>
    /// Includes filename/end folder if applicable. For list operations, will be the same as RemotePath
    /// </summary>
    public abstract string FullRemotePath { get; }

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
    public float CurrentProgress => _dataTransferred / Mathf.Max(TotalFileSizeToTransfer, 1); 
    public ulong TotalFileSizeToTransfer;
    private ulong _dataTransferred;
    private ulong _lastDataTransferred;

    public override string FullRemotePath => RemotePath.EndsWith('/') ? $"{RemotePath}{Name}" : $"{RemotePath}/{Name}";

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
    public override string FullRemotePath => RemotePath;
    public List<string> FoundFolderNames;

    public FTPRemoteFolderListTask(int taskID, string remotePath) : base(taskID, remotePath) { }
}

public class FTPRemoteFileListTask : FTPTask<FTPRemoteFileListTask>
{
    public override string FullRemotePath => RemotePath;
    public List<FileDetails> FoundFilesDetails;

    public FTPRemoteFileListTask(int taskID, string remotePath) : base(taskID, remotePath) { }
}

public abstract class FTPRemoteTask : FTPTask<FTPRemoteTask>
{
    public override string FullRemotePath => RemotePath.EndsWith('/') ? $"{RemotePath}{Name}" : $"{RemotePath}/{Name}";
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