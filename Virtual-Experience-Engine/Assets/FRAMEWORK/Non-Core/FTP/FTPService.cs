using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;


public class FTPService // : IFTPService
{
    #region Higher-level interfaces 
    /// <summary>
    /// Queue a file for download
    /// </summary>
    /// <param name="path">path should use / as separator not \</param>
    /// <param name="filename"></param>
    /// <param name="callback">callback on completion or failure</param>
    /// <returns>TasK ID (for checking status/progress, or for cancelling)</returns>
    public FTPDownloadTask DownloadFile(string path, string filename)
    {
        FTPDownloadTask task = new(_nextQueueEntryID, path, GetLocalPath(path, true), filename);
        Enqueue(task);
        Debug.Log($"Enqueuing download id {task.TaskID}");
        return task;
    }

    /// <summary>
    /// Queue a file for upload
    /// </summary>
    /// <param name="path">path should use / as separator not \</param>
    /// <param name="filename"></param>
    /// <param name="callback">callback on completion or failure</param>
    /// <returns>TasK ID  (for checking status/progress, or for cancelling)</returns>
    public FTPUploadTask UploadFile(string path, string filename)
    {
        FTPUploadTask task = new(_nextQueueEntryID, path, GetLocalPath(path), filename);
        Enqueue(task);
        return task;
    }

    /// <summary>
    /// Queue a request for list of files in a given remote path
    /// </summary>
    /// <param name="path">path should use / as separator not \</param>
    /// <param name="callback">callback on completion or failure</param>
    /// <returns>TasK ID  (for checking status)</returns>
    public FTPRemoteFileListTask GetRemoteFileList(string path)
    {
        FTPRemoteFileListTask task = new(_nextQueueEntryID, path);
        Enqueue(task);
        return task;
    }

    /// <summary>
    /// Queue a request for list of subfolders in a given remote path
    /// </summary>
    /// <param name="path">path should use / as separator not \</param>
    /// <param name="callback">callback on completion or failure</param>
    /// <returns>TasK ID  (for checking status)</returns>
    public FTPRemoteFolderListTask GetRemoteFolderList(string path)
    {
        FTPRemoteFolderListTask task = new(_nextQueueEntryID, path);
        Enqueue(task);
        return task;
    }

    /// <summary>
    /// Queue a request to delete a remote file (does not delete local file)
    /// </summary>
    /// <param name="path">path should use / as separator not \</param>
    /// <param name="callback">callback on completion or failure</param>
    /// <returns>TasK ID  (for checking status)</returns>
    public FTPDeleteTask DeleteRemoteFileOrEmptyFolder(string path, string filename)
    {
        FTPDeleteTask task = new(_nextQueueEntryID, path, filename);
        Enqueue(task);
        return task;
    }

    /// <summary>
    /// Queue a request to create a remote folder (does not create it locally)
    /// </summary>
    /// <param name="path">path should use / as separator not \</param>
    /// <param name="callback">callback on completion or failure</param>
    /// <returns>TasK ID  (for checking status)</returns>
    public FTPMakeFolderTask MakeRemoteFolder(string path, string folderName)
    {
        FTPMakeFolderTask task = new(_nextQueueEntryID, path, folderName);
        Enqueue(task);
        return task;
    }

    //for debug
    private void LogQueue()
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
    public void CancelTask(int taskID)
    {
        Status s = GetTaskStatus(taskID);
        if (s == Status.underway)
        {
            if (_currentTask is FTPExchangeFileOperationTask exchangeTask)
                exchangeTask.Cancel();
        }
        else if (s == Status.queued)
        {
            foreach (FTPTaskBase task in _taskQueue)
            {
                if (task.TaskID == taskID)
                {
                    //can't actually remove items from queue - so leave there, marked as cancelled
                    task.Cancel();
                    //Debug.Log($"Cancelled {taskID}");
                }
            }
        }
    }

    /// <summary>
    /// Get progress of task (as a float 0-100%). Underway tasks will return at most 99%
    /// Complete tasks will return 100%. Queued or non-existent tasks will return 0%
    /// An underway task that is not an upload or download will show 0%, until it shows 100%
    /// </summary>
    /// <param name="taskID">The Task ID returned when task was queued</param>
    /// <returns></returns>
    public float GetTaskPercentageProgress(int taskID)
    {
        Status s = GetTaskStatus(taskID);

        if (s == Status.completed) 
            return 100f;
        else if (s == Status.unknown || s == Status.queued)
            return 0f;

        //Task is underway
        
        if (_currentTask is FTPExchangeFileOperationTask exchangeTask)
            return Mathf.Clamp(exchangeTask.CurrentProgress * 100f, 0f, 99f); //ensure only get 100% on completion
        else 
            return 0f;
    }

    public enum Status { queued, underway, unknown, completed }

    /// <summary>
    /// Find status of a task (queued, underway, completed, or unknown)
    /// Task IDs that are not recognised or have been successfully cancelled will give unknown.
    /// </summary>
    /// <param name="taskID">The Task ID returned when task was queued</param>
    /// <returns></returns>
    public Status GetTaskStatus(int taskID)
    {
        if (_completedTasks.Contains(taskID)) 
            return Status.completed;

        if (_currentTask != null && taskID == _currentTask.TaskID)
            return Status.underway;

        foreach (FTPTaskBase task in _taskQueue)
        {
            if (task.TaskID == taskID)
            {
                if (task.IsCancelled)
                    return Status.unknown;
                else
                    return Status.queued;
            }
        }
        return Status.unknown; //did not find it at all
    }
    #endregion


    private int _nextQueueEntryID = 0;

    private HashSet<int> _completedTasks = new();  //keep track so we can report their status

    private Queue<FTPTaskBase> _taskQueue = new();
    private FTPTaskBase _currentTask = null;

    private readonly FTPCommsHandler _commsHandler;

    public FTPService(FTPCommsHandler commsHandler)
    {
        _commsHandler = commsHandler;

        //These logically belong on the layer above, but are here as those are statics
        string pathWorlds = Application.persistentDataPath + "\\files\\Worlds";

        //create folders if they do not exist
        if (!Directory.Exists(pathWorlds)) Directory.CreateDirectory(pathWorlds);

        //TODO: Sort out the above 

        //TODO: wire up ProgressChanged and StatusChanged
    }

    public void TearDown()
    {
        if (_currentTask != null) 
            CancelTask(_currentTask.TaskID);

        foreach (FTPTaskBase t in _taskQueue)
            CancelTask(_currentTask.TaskID);
    }

    private string GetLocalPath(string path, bool create = false)
    {
        string loc = path.Replace("/", "\\");
        if (loc == "")
            loc = Application.persistentDataPath + "\\files";
        else
            loc = Application.persistentDataPath + "\\files\\" + loc;

        if (create)
            Directory.CreateDirectory(loc);

        return loc;
    }

    //gets called on startup, when something is added, and whenever client changes status to ready
    //if this gets buggy it could more robustly but less efficiently sit in Update()
    private void ProcessQueue()
    {
        if (_commsHandler.Status == FTPStatus.Busy) 
            return;

        if (_currentTask != null && !_currentTask.IsCancelled)
            _completedTasks.Add(_currentTask.TaskID); //record it as done

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

    public void HandleStatusChanged(FTPStatus newStatus)
    {
        //if client is now ready - process next queue item
        if (newStatus == FTPStatus.Ready)
            ProcessQueue();
    }
}



public abstract class FTPTaskBase
{
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
        CompletionCode = code;
        OnComplete?.Invoke((TCompletedTask)this);
    }
}

public abstract class FTPExchangeFileOperationTask : FTPTask<FTPExchangeFileOperationTask>
{
    public event Action<float> OnProgressChanged;
    public float CurrentProgress => _dataTransferred / RemoteFileSize;
    public ulong RemoteFileSize;
    private ulong _dataTransferred;
    private ulong _lastDataTransferred;

    public readonly string LocalPath;
    public readonly string Name;

    public FTPExchangeFileOperationTask(int taskID, string remotePath, string localPath, string name) : base(taskID, remotePath) 
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

public class FTPDownloadTask : FTPExchangeFileOperationTask
{
    public FTPDownloadTask(int taskID, string remotePath, string localPath, string name) : base(taskID, remotePath, localPath, name) { }
}

public class FTPUploadTask : FTPExchangeFileOperationTask
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