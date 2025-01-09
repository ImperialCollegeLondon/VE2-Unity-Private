using System;
using System.Collections.Generic;
using VE2_NonCore_FileSystem_Interfaces_Common;

namespace VE2_NonCore_FileSystem_Interfaces_Internal
{
    //Why is this mostly a copy/paste of IPluginFileSystem? 
    //Because the hub will create a filesystem instance that will persist between scenes
    //We don't want the plugin to accidentally start talking to the internal file system, so we need separate interfaces for the two
    //Annoyingly, this does mean duplicate code! These two interfaces need to live in separate, isolated assemblies
    public interface IInternalFileSystem
    {
        public bool IsFileSystemReady { get; }
        public event Action OnFileSystemReady;
        public void RefreshLocalFiles();
        public void RefreshRemoteFiles();
        public Dictionary<string, LocalFileDetails> GetLocalFiles();
        public Dictionary<string, RemoteFileDetails> GetRemoteFiles();
        public IRemoteFileTaskInfo DownloadFile(string nameAndPath);
        public IRemoteFileTaskInfo UploadFile(string nameAndPath);
        //public IRemoteFileTaskInfo DeleteRemoteFile(string nameAndPath); Internal system shouldn't be able to delete files, at least, not yet!
        //public bool DeleteLocalFile(string nameAndPath)'
        public Dictionary<string, IRemoteFileTaskInfo> GetQueuedTasks();
        public Dictionary<string, IRemoteFileTaskInfo> GetCompletedTasks();
    }
}
