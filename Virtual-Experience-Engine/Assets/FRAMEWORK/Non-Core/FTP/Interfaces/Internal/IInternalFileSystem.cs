using System;
using System.Collections.Generic;
using VE2_NonCore_FileSystem_Interfaces_Common;

namespace VE2_NonCore_FileSystem_Interfaces_Internal
{
    public interface IInternalFileSystem
    {
        public void RefreshLocalFiles();
        public void RefreshRemoteFiles();
        public IRemoteFileTaskInfo DownloadFile(string nameAndPath);
        public IRemoteFileTaskInfo UploadFile(string nameAndPath);
        //public IRemoteFileTaskInfo DeleteRemoteFile(string nameAndPath); Internal system shouldn't be able to delete files, at least, not yet!
        public List<IRemoteFileTaskInfo> GetQueuedTasks();
        public List<IRemoteFileTaskInfo> GetCompletedTasks();
    }
}
