using System;
using System.Collections.Generic;
using UnityEngine;

namespace VE2.NonCore.FileSystem.API
{
    public interface IV_FileSystem
    {
        public bool IsFileSystemReady { get; }
        public event Action OnFileSystemReady;

        public string LocalAbsoluteWorkingPath { get; }

        public Dictionary<string, LocalFileDetails> GetLocalFilesAtPath(string path);

        /// <summary>
        /// Returns a list of folder names only, doesn't include the path to the folder
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public List<string> GetLocalFoldersAtPath(string path);

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


        public List<IRemoteFileTaskInfo> GetQueuedFileTasks();
        public List<IRemoteFileTaskInfo> GetCompletedFileTasks();
    }

    //CAUTION - These objects are customer-facing, changing these will break plugins=================================
    //===============================================================================================================

    //TODO - put behind interfaces

    [Serializable]
    public class LocalFileDetails : FileDetails
    {
        /// <summary>
        /// Full local name and path of the file on this machine
        /// </summary>
        [SerializeField, LabelWidth(180)] public string FullLocalNameAndPath;

        public LocalFileDetails(string nameAndPath, ulong size, string fullLocalNameAndPath) : base(nameAndPath, size)
        {
            FullLocalNameAndPath = fullLocalNameAndPath;
        }
    }

    [Serializable]
    public class RemoteFileDetails : FileDetails 
    {
        public RemoteFileDetails(string nameAndPath, ulong size) : base(nameAndPath, size) { }
    }

    [Serializable]
    public abstract class FileDetails 
    {
        /// <summary>
        /// According to the VE2 file system. Pass this string back to the file system to download/upload/delete that file
        /// </summary>
        [SerializeField, BeginHorizontal(ControlFieldWidth = true), LabelWidth(125)] public string NameAndPath;
        /// <summary>
        /// File size in bytes
        /// </summary>
        [SerializeField, EndHorizontal, Suffix("KB"), LabelWidth(60)] public ulong Size;

        protected FileDetails(string nameAndPath, ulong size)
        {
            NameAndPath = nameAndPath;
            Size = size;
        }
    }

    public enum RemoteTaskType
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

    public interface IRemoteFileTaskInfo
    {
        public RemoteTaskType Type { get; }
        public string NameAndPath { get; }
        public float Progress { get; }
        public RemoteFileTaskStatus Status { get; }

        public event Action<RemoteFileTaskStatus> OnStatusChanged;
        public event Action<IRemoteFileTaskInfo> OnTaskCompleted;

        public bool IsCancellable { get; }
        public void CancelRemoteFileTask();
    }

    public interface IRemoteFileSearchInfo
    {
        public string Path { get; }
        public Dictionary<string, RemoteFileDetails> FilesFound { get; }
        public event Action<IRemoteFileSearchInfo> OnSearchComplete;
    }

    public interface IRemoteFolderSearchInfo
    {
        public string Path { get; }
        public List<string> FoldersFound { get; }
        public event Action<IRemoteFolderSearchInfo> OnSearchComplete;
        public string CompletionCode { get; }
    }
}
