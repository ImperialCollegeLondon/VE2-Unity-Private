using System;
using System.Collections.Generic;
using VE2_NonCore_FileSystem_Interfaces_Common;

namespace VE2_NonCore_FileSystem_Interfaces_Plugin
{
    public interface IPluginFileSystem
    {
        public bool IsFileSystemReady { get; }
        public event Action OnFileSystemReady;

        public Dictionary<string, LocalFileDetails> GetAllLocalFiles();
        public Dictionary<string, LocalFileDetails> GetLocalFilesAtPath(string path);
        public List<string> GetLocalFoldersAtPath(string path);

        public IRemoteFileSearchInfo GetAllRemoteFiles();
        public IRemoteFileSearchInfo GetRemoteFilesAtPath(string path);
        public IRemoteFolderSearchInfo GetRemoteFoldersAtPath(string path);

        public IRemoteFileTaskInfo DownloadFile(string nameAndPath);
        public IRemoteFileTaskInfo UploadFile(string nameAndPath);
        public IRemoteFileTaskInfo DeleteRemoteFile(string nameAndPath);

        /// <summary>
        /// This operation is synchronous and will execute immediately. Will return true if successful, false if not
        /// </summary>
        /// <param name="nameAndPath"></param>
        /// <returns></returns>
        public bool DeleteLocalFile(string nameAndPath);


        public Dictionary<string, IRemoteFileTaskInfo> GetQueuedTasks();
        public Dictionary<string, IRemoteFileTaskInfo> GetCompletedTasks();
    }
}
