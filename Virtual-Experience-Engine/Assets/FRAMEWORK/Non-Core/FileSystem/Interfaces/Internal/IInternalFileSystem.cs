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

        public string LocalWorkingPath { get; }

        public Dictionary<string, LocalFileDetails> GetLocalFilesAtPath(string path);
        public List<string> GetLocalFoldersAtPath(string path);

        public IRemoteFileSearchInfo GetRemoteFilesAtPath(string path);
        public IRemoteFolderSearchInfo GetRemoteFoldersAtPath(string path);

        public IRemoteFileTaskInfo DownloadFile(string nameAndPath);
        public IRemoteFileTaskInfo UploadFile(string nameAndPath);

        //Internal system shouldn't be able to delete files, though this may change in future
        //public IRemoteFileTaskInfo DeleteRemoteFile(string nameAndPath); 
        //public bool DeleteLocalFile(string nameAndPath)'

        public List<IRemoteFileTaskInfo> GetQueuedFileTasks();
        public List<IRemoteFileTaskInfo> GetCompletedFileTasks();

        /// <summary>
        /// Allows the status of tasks to be updated outside play mode, useful for the plugin uploader
        /// </summary>
        public void Update();
    }
}
