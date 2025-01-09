using System;
using System.Collections.Generic;
using UnityEngine;
using VE2_NonCore_FileSystem_Interfaces_Common;

namespace VE2_NonCore_FileSystem_Interfaces_Plugin
{
    public interface IPluginFileSystem
    {
        public void RefreshLocalFiles();
        public void RefreshRemoteFiles();
        public IRemoteFileTaskInfo DownloadFile(string nameAndPath);
        public IRemoteFileTaskInfo UploadFile(string nameAndPath);
        public IRemoteFileTaskInfo DeleteRemoteFile(string nameAndPath);
        public List<IRemoteFileTaskInfo> GetQueuedTasks();
        public List<IRemoteFileTaskInfo> GetCompletedTasks();
    }
}
