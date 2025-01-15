using System;
using System.Collections.Generic;
using UnityEngine;

namespace VE2_NonCore_FileSystem_Interfaces_Common
{
    //CAUTION - These objects are customer-facing, changing these will break plugins=================================
    //===============================================================================================================

    [Serializable]
    public class LocalFileDetails : FileDetails
    {
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
        public string NameAndPath { get; }
        public event Action<Dictionary<string, RemoteFileDetails>> OnFilesFound;
    }

    public interface IRemoteFolderSearchInfo
    {
        public string Path { get; }
        public event Action<List<string>> OnFoldersFound;
    }
}