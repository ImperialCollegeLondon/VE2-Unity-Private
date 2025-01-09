using System;
using System.Collections.Generic;
using System.IO;
using Renci.SshNet;
using UnityEngine;

namespace VE2_NonCore_FileSystem
{
    public static class FileSystemServiceFactory
    {
        public static FileSystemService CreateFileStorageService(FTPNetworkSettings ftpNetworkSettings, string workingPath)
        {
            string localWorkingPath = Application.persistentDataPath + "/files/" + workingPath; 
            string remoteWorkingPath = workingPath;

            SftpClient sftpClient = new(ftpNetworkSettings.IP, ftpNetworkSettings.Port, ftpNetworkSettings.Username, ftpNetworkSettings.Password);
            FTPCommsHandler commsHandler = new(sftpClient);
            FTPService ftpService = new(commsHandler, remoteWorkingPath, localWorkingPath);

            return new FileSystemService(ftpService, remoteWorkingPath, localWorkingPath);
        }
    }

    public class FileSystemService
    {
        #region higher-level interfaces 
        public bool IsFileStorageServiceReady => _ftpService.IsFTPServiceReady;
        public event Action OnFileStorageServiceReady { add { _ftpService.OnFTPServiceReady += value; } remove { _ftpService.OnFTPServiceReady -= value; } }
        public event Action OnLocalFilesRefreshed;
        public event Action OnRemoteFilesRefreshed { add { _ftpService.OnRemoteFileListUpdated += value; } remove { _ftpService.OnRemoteFileListUpdated -= value; } }

        public FTPDownloadTask DownloadFile(string workingFileNameAndPath)
        {
            Debug.Log($"Queueing file for download: {workingFileNameAndPath}");
            string fileName = workingFileNameAndPath.Substring(workingFileNameAndPath.LastIndexOf("/") + 1);
            string remotePathFromWorking = workingFileNameAndPath.Contains("/") ? workingFileNameAndPath.Substring(0, workingFileNameAndPath.LastIndexOf("/")) : "";
            FTPDownloadTask task = _ftpService.DownloadFile(remotePathFromWorking, fileName);
            task.OnComplete += OnRemoteDownloadComplete;
            return task;
        }

        public FTPUploadTask UploadFile(string workingFileNameAndPath)
        {
            Debug.Log($"Queueing file for upload: {workingFileNameAndPath}");
            string fileName = workingFileNameAndPath.Substring(workingFileNameAndPath.LastIndexOf("/") + 1);
            string remoteCorrectedFileNameAndPath = workingFileNameAndPath;
            string remotePathFromWorking = remoteCorrectedFileNameAndPath.Contains("/") ? remoteCorrectedFileNameAndPath.Substring(0, remoteCorrectedFileNameAndPath.LastIndexOf("/")) : "";
            FTPUploadTask task = _ftpService.UploadFile(remotePathFromWorking, fileName); //No need to refresh manually, will happen automatically
            return task;
        }

        public FTPDeleteTask DeleteRemoteFile(string workingFileNameAndPath)
        {
            Debug.Log($"Queueing remote file for deletion: {workingFileNameAndPath}");
            string fileName = workingFileNameAndPath.Substring(workingFileNameAndPath.LastIndexOf("/") + 1);
            string remotePathFromWorking = workingFileNameAndPath.Contains("/") ? workingFileNameAndPath.Substring(0, workingFileNameAndPath.LastIndexOf("/")) : "";
            FTPDeleteTask task = _ftpService.DeleteRemoteFileOrEmptyFolder(remotePathFromWorking, fileName);
            return task;
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

                RefreshLocalFiles();

                return true;
            }
            else
            {
                Debug.LogWarning($"File not found: {localPath}");
                return false;
            }
        }

        public readonly Dictionary<string, FileDetails> LocalFiles = new();
        public Dictionary<string, FileDetails> RemoteFiles => _ftpService.RemoteFiles;

        public void RefreshLocalFiles()
        {
            LocalFiles.Clear();

            if (string.IsNullOrWhiteSpace(LocalWorkingPath))
                throw new ArgumentException("Path cannot be null or empty.", nameof(LocalWorkingPath));

            if (!Directory.Exists(LocalWorkingPath))
                Directory.CreateDirectory(LocalWorkingPath);

            try
            {
                // Get all files recursively
                string[] files = Directory.GetFiles(LocalWorkingPath, "*", SearchOption.AllDirectories);
                foreach (string file in files)
                {
                    FileInfo fileInfo = new(file);
                    string correctedFileFullName = fileInfo.FullName.Replace("\\", "/"); //System.IO gives us paths with back slashes
                    string workingFileNameAndPath = correctedFileFullName.Replace($"{LocalWorkingPath}/", "").TrimStart('/');
                    LocalFiles.Add(workingFileNameAndPath, new FileDetails { fileNameAndWorkingPath = workingFileNameAndPath, fileSize = (ulong)fileInfo.Length });
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

            OnLocalFilesRefreshed?.Invoke();
        }

        public void RefreshRemoteFiles() => _ftpService.RefreshRemoteFileList();
        #endregion


        private readonly FTPService _ftpService;
        public readonly string LocalWorkingPath;

        public readonly string RemoteWorkingPath;

        public FileSystemService(FTPService ftpService, string remoteWorkingPath, string localWorkingPath)
        {
            _ftpService = ftpService;

            RemoteWorkingPath = remoteWorkingPath;
            LocalWorkingPath = localWorkingPath;

            RefreshLocalFiles();
        }

        private void OnRemoteDownloadComplete(FTPTask task)
        {
            task.OnComplete -= OnRemoteDownloadComplete;

            //Could add file manually rather than refreshing, but local refresh doesn't take long 
            RefreshLocalFiles();
        }

        public void TearDown() => _ftpService.TearDown();
    }
}
