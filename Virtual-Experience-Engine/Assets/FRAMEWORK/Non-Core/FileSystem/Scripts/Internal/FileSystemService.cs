using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Renci.SshNet;
using UnityEngine;
using VE2.NonCore.FileSystem.API;
using static VE2.NonCore.Platform.API.PlatformPublicSerializables;

namespace VE2.NonCore.FileSystem.Internal
{
    internal static class FileSystemServiceFactory
    {
        internal static FileSystemService CreateFileStorageService(ServerConnectionSettings ftpNetworkSettings, string workingPath)
        {
            string localWorkingPath = Application.persistentDataPath + "/files/" + workingPath; 
            string remoteWorkingPath = workingPath;

            SftpClient sftpClient = new(ftpNetworkSettings.ServerAddress, (int)ftpNetworkSettings.ServerPort, ftpNetworkSettings.Username, ftpNetworkSettings.Password);
            FTPCommsHandler commsHandler = new(sftpClient);
            FTPService ftpService = new(commsHandler, remoteWorkingPath, localWorkingPath);

            return new FileSystemService(ftpService, remoteWorkingPath, localWorkingPath);
        }
    }

    //Has the same interface as V_InternalFileSystem. The PluginBuilder/Exporter needs to use this without going through a MonoBehaviour first
    internal  class FileSystemService : IFileSystemInternal 
    {
        #region Interfaces 
        public bool IsFileSystemReady { get; private set; } = false;
        public event Action OnFileSystemReady;
        public string LocalWorkingPath {get; private set; }


        public Dictionary<string, LocalFileDetails> GetLocalFilesAtPath(string path)
        {
            Dictionary<string, LocalFileDetails> localFiles = new();

            string correctedPath = path.StartsWith("/") ? path.Substring(1) : path;
            string localAbsolutePath = $"{LocalWorkingPath}/{correctedPath}";

            if (string.IsNullOrWhiteSpace(localAbsolutePath))
                throw new ArgumentException("Path cannot be null or empty.", nameof(localAbsolutePath));

            if (!Directory.Exists(localAbsolutePath))
                Directory.CreateDirectory(localAbsolutePath);

            try
            {
                // Get all files recursively
                string[] files = Directory.GetFiles(localAbsolutePath, "*", SearchOption.TopDirectoryOnly);
                foreach (string file in files)
                {
                    FileInfo fileInfo = new(file);
                    string correctedFileFullName = fileInfo.FullName.Replace("\\", "/"); //System.IO gives us paths with back slashes
                    string workingFileNameAndPath = correctedFileFullName.Replace($"{LocalWorkingPath}/", "").TrimStart('/');
                    localFiles.Add(workingFileNameAndPath, new LocalFileDetails(workingFileNameAndPath, (ulong)fileInfo.Length, correctedFileFullName));
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine($"Access denied to a directory: {ex.Message}");
            }
            catch (IOException ex)
            {
                Console.WriteLine($"An I/O error occurred: {ex.Message}");
            }

            //Debug.LogWarning($"Found {localFiles.Count} local files at {localAbsolutePath}");

            return localFiles;
        }

        public List<string> GetLocalFoldersAtPath(string path)
        {
            List<string> localFolders = new();
            string correctedPath = path.StartsWith("/") ? path.Substring(1) : path;
            string localPath = $"{LocalWorkingPath}/{correctedPath}";

            //Debug.Log("Get local folders at " + localPath);

            if (string.IsNullOrWhiteSpace(localPath))
                throw new ArgumentException("Path cannot be null or empty.", nameof(localPath));

            if (!Directory.Exists(localPath))
                Directory.CreateDirectory(localPath);

            try
            {
                // Get all files recursively
                string[] folders = Directory.GetDirectories(localPath, "*", SearchOption.TopDirectoryOnly);
                foreach (string folder in folders)
                {
                    DirectoryInfo dirInfo = new(folder);
                    string folderName = dirInfo.Name;

                    if (string.IsNullOrWhiteSpace(folderName))
                    {
                        Debug.LogWarning("Found a folder with no name, skipping it.");
                        continue; // Skip folders with no name
                    }
                    
                    localFolders.Add(folderName);
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine($"Access denied to a directory: {ex.Message}");
            }
            catch (IOException ex)
            {
                Console.WriteLine($"An I/O error occurred: {ex.Message}");
            }

            //Debug.LogWarning($"Found {localFolders.Count} local folders at {localPath}");
            return localFolders;
        }

        public IRemoteFileSearchInfo GetRemoteFilesAtPath(string path)
        {
            string correctedPath = path.StartsWith("/") ? path.Substring(1) : path;
            string remotePathAndNameFromWorking = $"{RemoteWorkingPath}/{correctedPath}";

            Debug.Log("Get remote files at " + remotePathAndNameFromWorking);

            FTPRemoteFileListTask task = _ftpService.GetRemoteFilesAtPath(correctedPath);
            RemoteFileSearchInfo taskInfo = new(task, path);
            return taskInfo;
        }

        public IRemoteFolderSearchInfo GetRemoteFoldersAtPath(string path)
        {
            string correctedPathFromWorking = path.StartsWith("/") ? path.Substring(1) : path;

            Debug.Log("Get remote folders at " + correctedPathFromWorking);

            FTPRemoteFolderListTask task = _ftpService.GetRemoteFoldersAtPath(correctedPathFromWorking);
            RemoteFolderSearchInfo taskInfo = new(task, path);
            return taskInfo;
        }

        public IRemoteFileTaskInfo DownloadFile(string workingFileNameAndPath)
        {
            Debug.Log($"Queueing file for download: {workingFileNameAndPath}");
            string fileName = workingFileNameAndPath.Substring(workingFileNameAndPath.LastIndexOf("/") + 1);
            string remotePathFromWorking = workingFileNameAndPath.Contains("/") ? workingFileNameAndPath.Substring(0, workingFileNameAndPath.LastIndexOf("/")) : "";
            FTPDownloadTask task = _ftpService.DownloadFile(remotePathFromWorking, fileName);

            RemoteFileTaskInfo taskInfo = new(task, RemoteTaskType.Download, 0, workingFileNameAndPath);
            _queuedTasks.Add(taskInfo);
            taskInfo.OnTaskCompleted += OnRemoteFileTaskComplete;

            return taskInfo;
        }
        public IRemoteFileTaskInfo UploadFile(string workingFileNameAndPath)
        {
            Debug.Log($"Queueing file for upload: {workingFileNameAndPath}");
            string fileName = workingFileNameAndPath.Substring(workingFileNameAndPath.LastIndexOf("/") + 1);
            string remoteCorrectedFileNameAndPath = workingFileNameAndPath;
            string remotePathFromWorking = remoteCorrectedFileNameAndPath.Contains("/") ? remoteCorrectedFileNameAndPath.Substring(0, remoteCorrectedFileNameAndPath.LastIndexOf("/")) : "";
            FTPUploadTask task = _ftpService.UploadFile(remotePathFromWorking, fileName); //No need to refresh manually, will happen automatically

            RemoteFileTaskInfo taskInfo = new(task, RemoteTaskType.Upload, 0, workingFileNameAndPath);
            _queuedTasks.Add(taskInfo);
            taskInfo.OnTaskCompleted += OnRemoteFileTaskComplete;

            return taskInfo;
        }
        public IRemoteFileTaskInfo DeleteRemoteFile(string workingFileNameAndPath)
        {
            Debug.Log($"Queueing remote file for deletion: {workingFileNameAndPath}");
            string fileName = workingFileNameAndPath.Substring(workingFileNameAndPath.LastIndexOf("/") + 1);
            string remotePathFromWorking = workingFileNameAndPath.Contains("/") ? workingFileNameAndPath.Substring(0, workingFileNameAndPath.LastIndexOf("/")) : "";
            FTPDeleteTask task = _ftpService.DeleteRemoteFileOrEmptyFolder(remotePathFromWorking, fileName);

            RemoteFileTaskInfo taskInfo = new(task, RemoteTaskType.Delete, 0, workingFileNameAndPath);
            _queuedTasks.Add(taskInfo);
            taskInfo.OnTaskCompleted += OnRemoteFileTaskComplete;

            return taskInfo;
        }
        public bool DeleteLocalFile(string workingFileNameAndPath)
        {
            Debug.Log($"Deleting local file: {workingFileNameAndPath}");
            string localPath = $"{LocalWorkingPath}/{workingFileNameAndPath}";

            if (File.Exists(localPath))
            {
                File.Delete(localPath);

                string directoryPath = Path.GetDirectoryName(localPath);
                if (Directory.GetDirectories(directoryPath).Length == 0 && Directory.GetFiles(directoryPath).Length == 0)
                    Directory.Delete(directoryPath);

                return true;
            }
            else
            {
                Debug.LogWarning($"File not found: {localPath}");
                return false;
            }
        }
        public List<IRemoteFileTaskInfo> GetQueuedFileTasks() => _queuedTasks;
        public List<IRemoteFileTaskInfo> GetCompletedFileTasks() => _completedTasks;

        public void Update()
        {
            //Calling update may invoke OnTaskCompleted, which will cause a task to be removed from _queuedTasks
            //This means we need to copy the list to avoid a concurrent modification exception
            List<IRemoteFileTaskInfo> tasksToUpdate = _queuedTasks.ToList();

            foreach (RemoteFileTaskInfo task in tasksToUpdate)
                task.Update();
        }
        #endregion


        private List<IRemoteFileTaskInfo> _queuedTasks = new();
        private List<IRemoteFileTaskInfo> _completedTasks = new();

        private readonly FTPService _ftpService;
        public readonly string RemoteWorkingPath;

        public FileSystemService(FTPService ftpService, string remoteWorkingPath, string localWorkingPath)
        {
            _ftpService = ftpService;

            RemoteWorkingPath = remoteWorkingPath;
            LocalWorkingPath = localWorkingPath;

            IsFileSystemReady = true;

            try
            {
                OnFileSystemReady?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogError($"Error invoking OnFileSystemReady: {e.Message}");
            }
        }

        private void OnRemoteFileTaskComplete(IRemoteFileTaskInfo taskInfo)
        {
            taskInfo.OnTaskCompleted -= OnRemoteFileTaskComplete;

            _queuedTasks.Remove((RemoteFileTaskInfo)taskInfo);
            _completedTasks.Add((RemoteFileTaskInfo)taskInfo);
        }

        public void TearDown() => _ftpService.TearDown();
    }
}
