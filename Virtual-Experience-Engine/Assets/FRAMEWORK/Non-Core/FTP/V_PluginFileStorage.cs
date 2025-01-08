using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum TaskType
{
    Download,
    Upload,
    Delete
}

public enum RemoteFileTaskStatus
{
    Queued,
    InProgress,
    Cancelled,
    Succeeded,
    Failed
}

[Serializable]
public class RemoteFileTaskInfo //TODO: needs an interface
{
    [BeginHorizontal(ControlFieldWidth = false), SerializeField, Disable] private TaskType _type;
    public TaskType Type => _type;

    [SerializeField, LabelWidth(110f), Disable] private string _nameAndPath; //Relative to working path
    public string NameAndPath => _nameAndPath;

    [SerializeField, Disable] private float _progress; //TODO: Progress sometimes show 0 when completed
    public float Progress => _progress;

    [EditorButton(nameof(CancelTask), "Cancel", activityType: ButtonActivityType.OnPlayMode, Order = 1)]  
    [EndHorizontal, SerializeField, Disable] private RemoteFileTaskStatus _status;
    public RemoteFileTaskStatus Status => _status;

    public event Action<RemoteFileTaskStatus> OnStatusChanged;
    public event Action<RemoteFileTaskInfo> OnTaskCompleted;

    private readonly FTPFileTask _task;

    public RemoteFileTaskInfo(FTPFileTask task, TaskType type, float progress, string nameAndPath)
    {
        _task = task;
        _type = type;
        _nameAndPath = nameAndPath; 
        _progress = progress;
    }

    public void CancelTask()
    {
        //Once underway, only uploads and downloads can be cancelled 
        bool isCancellable = !_status.Equals(RemoteFileTaskStatus.InProgress) && _type.Equals(TaskType.Delete);
        if (isCancellable)
        {
            _task.Cancel();
            Update();
        }
        else 
        {
            UnityEngine.Debug.LogWarning($"Task cannot be cancelled: {_nameAndPath} - cannot cancel a delete task that is already in progress");
        }
    }

    //Why have an explicit update method rather than just having progress/status be properties that point towards the task directly?
    //Because we want to be able to show the progress/status in the inspector, which means we need a field that gets updated every frame
    public void Update() 
    {
        if (_task is FTPFileTransferTask transferTask)
            _progress = transferTask.CurrentProgress;
        else
            _progress = 0;

        RemoteFileTaskStatus previousStatus = Status;
        RemoteFileTaskStatus newStatus = _task.CompletionCode switch
        {
            FTPCompletionCode.Waiting => RemoteFileTaskStatus.Queued,
            FTPCompletionCode.Busy => RemoteFileTaskStatus.InProgress,
            _ when _task.IsCancelled => RemoteFileTaskStatus.Cancelled,
            _ when _task.IsCompleted && _task.CompletionCode.Equals(FTPCompletionCode.Success) => RemoteFileTaskStatus.Succeeded,
            _ => RemoteFileTaskStatus.Failed
        };

        _status = newStatus;

        if (previousStatus != newStatus)
        {
            try
            {
                OnStatusChanged?.Invoke(_status);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"Error invoking task status changed event: {e.Message}");
            }

            if (newStatus == RemoteFileTaskStatus.Succeeded || newStatus == RemoteFileTaskStatus.Failed)
            {
                try
                {
                    OnTaskCompleted?.Invoke(this);
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError($"Error invoking task completed event: {e.Message}");
                }
            }
        }
    }
}

public class V_PluginFileStorage : MonoBehaviour
{
    [SerializeField, SpaceArea(spaceAfter: 10)] private FTPNetworkSettings ftpNetworkSettings;

    [Title("Play mode debug")]
    [Help("Enter play mode to view local and remote files")]

    [EditorButton(nameof(OpenLocalWorkingFolder), "Open Local Working Folder", activityType: ButtonActivityType.Everything, Order = 2)]
    [EditorButton(nameof(RefreshLocalFiles), "Refresh Local Files", activityType: ButtonActivityType.OnPlayMode, Order = -1)]
    [EditorButton(nameof(UploadAllFiles), "Upload All Files", activityType: ButtonActivityType.OnPlayMode, Order = -1)]
    [EditorButton(nameof(DeleteAllLocalFiles), "Delete All Local Files", activityType: ButtonActivityType.OnPlayMode, Order = -1)]
    [SerializeField, Disable, BeginGroup("Local Files"), EndGroup, SpaceArea(spaceBefore: 10)] private List<string> _localFilesAvailable = new(); //TODO don't show full local path


    [EditorButton(nameof(RefreshRemoteFiles), "Refresh Remote Files", activityType: ButtonActivityType.OnPlayMode, Order = -1)]
    [EditorButton(nameof(DownloadAllFiles), "Download all Files", activityType: ButtonActivityType.OnPlayMode, Order = -1)]
    [EditorButton(nameof(DeleteAllRemoteFiles), "Delete All Remote Files", activityType: ButtonActivityType.OnPlayMode, Order = -1)]
    [SerializeField, Disable, BeginGroup("Remote Files"), EndGroup, SpaceArea(spaceBefore: 10)] private List<string> _remoteFilesAvailable = new();

    [SerializeField, IgnoreParent, BeginGroup("Remote File Tasks"), SpaceArea(spaceBefore: 10)] private List<RemoteFileTaskInfo> _queuedTasks = new();
    [EditorButton(nameof(CancelAllTasks), "Cancel All Tasks", activityType: ButtonActivityType.OnPlayMode, Order = -1)]
    [SerializeField, IgnoreParent, EndGroup] private List<RemoteFileTaskInfo> _completedTasks = new();


    #region interface stuff 
    public void RefreshLocalFiles() => _fileStorageService.RefreshLocalFiles();
    public void RefreshRemoteFiles() => _fileStorageService.RefreshRemoteFiles();
    public void DownloadFile(string nameAndPath) //TODO: need to return these objs
    {
        FTPDownloadTask task = _fileStorageService.DownloadFile(nameAndPath);
        _queuedTasks.Add(new RemoteFileTaskInfo(task, TaskType.Download, 0, nameAndPath));
    }
    public void UploadFile(string nameAndPath) 
    {
        FTPUploadTask task = _fileStorageService.UploadFile(nameAndPath);
        _queuedTasks.Add(new RemoteFileTaskInfo(task, TaskType.Upload, 0, nameAndPath));
    }
    public void DeleteRemoteFile(string nameAndPath) 
    {
        FTPDeleteTask task = _fileStorageService.DeleteRemoteFile(nameAndPath);
        _queuedTasks.Add(new RemoteFileTaskInfo(task, TaskType.Delete, 0, nameAndPath));
    }
    #endregion

    private FileStorageService _fileStorageService;
    private string _localWorkingFilePath => $"VE2/PluginFiles/{SceneManager.GetActiveScene().name}";

    private void OnEnable()
    {
        _fileStorageService = FileStorageServiceFactory.CreateFileStorageService(ftpNetworkSettings, _localWorkingFilePath);
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
            if (task.Status == RemoteFileTaskStatus.Succeeded || task.Status == RemoteFileTaskStatus.Failed)
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
        string path = Application.persistentDataPath + "/files/" + _localWorkingFilePath;
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
                Console.WriteLine("The specified path does not exist.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
    }

    private void HandleLocalFilesRefreshed() 
    {
        _localFilesAvailable.Clear();
        foreach (var file in _fileStorageService.localFiles)
            _localFilesAvailable.Add(file.Key);
    }

    private void HandleRemoteFilesRefreshed() 
    {
        _remoteFilesAvailable.Clear();
        foreach (var file in _fileStorageService.RemoteFiles)
            _remoteFilesAvailable.Add(file.Key);
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
            _fileStorageService.DeleteLocalFile(fileNameAndPath);
    }

    private void DeleteAllRemoteFiles() 
    {
        List<string> remoteFileNames = new List<string>(_fileStorageService.RemoteFiles.Keys);
        foreach (string fileNameAndPath in remoteFileNames)
            DeleteRemoteFile(fileNameAndPath);
    }

    private void CancelAllTasks()
    {
        List<RemoteFileTaskInfo> tasks = new List<RemoteFileTaskInfo>(_queuedTasks);
        foreach (RemoteFileTaskInfo task in tasks)
            _fileStorageService.CancelTask(task.NameAndPath);
    }
    #endregion
}
