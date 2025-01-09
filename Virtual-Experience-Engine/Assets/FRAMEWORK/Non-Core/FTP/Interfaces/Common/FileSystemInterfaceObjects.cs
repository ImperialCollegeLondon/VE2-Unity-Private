using System;

namespace VE2_NonCore_FileSystem_Interfaces_Common
{
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

        public void CancelRemoteFileTask();
    }
}