using System;
using System.Collections.Generic;

public interface IInternalFileSystem
{
    public void RefreshLocalFiles();
    public void RefreshRemoteFiles();
    public IRemoteFileTaskInfo DownloadFile(string nameAndPath);
    public IRemoteFileTaskInfo UploadFile(string nameAndPath);
    public IRemoteFileTaskInfo DeleteRemoteFile(string nameAndPath);
    public List<IRemoteFileTaskInfo> GetQueuedTasks();
    public List<IRemoteFileTaskInfo> GetCompletedTasks();
}
