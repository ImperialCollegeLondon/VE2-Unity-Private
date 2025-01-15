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

    //This layer used to be responsible for building up a tree of remote and local files, since that functionality was removed, this layer doesn't do much!
    public class FileSystemService //TODO: Either remove this layer and put the code into V_FileSystemIntegrationBase, or remove code from V_FSIB and have it live here instead
    {
        #region higher-level interfaces 

        public FTPRemoteFileListTask GetRemoteFilesAtPath(string path)  
        {
            string correctedPath = path.StartsWith("/") ? path.Substring(1) : path;
            string remotePathAndNameFromWorking = $"{RemoteWorkingPath}/{correctedPath}";

            Debug.Log("Get remote files at " + remotePathAndNameFromWorking);
            return _ftpService.GetRemoteFilesAtPath(correctedPath);
        }

        public FTPRemoteFolderListTask GetRemoteFoldersAtPath(string path)
        {
            string correctedPath = path.StartsWith("/") ? path.Substring(1) : path;
            string remotePathAndNameFromWorking = $"{RemoteWorkingPath}/{correctedPath}";

            return _ftpService.GetRemoteFoldersAtPath(correctedPath);
        }

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
            Debug.Log($"Remote path from working: {remotePathFromWorking}");
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

                return true;
            }
            else
            {
                Debug.LogWarning($"File not found: {localPath}");
                return false;
            }
        }

        public Dictionary<string, FileDetails> GetAllLocalFiles()
        {
            return GetLocalFilesAtAbsolutePath(LocalWorkingPath, SearchOption.AllDirectories);
        }

        public Dictionary<string, FileDetails> GetAllLocalFilesAtPath(string path)
        {
            string correctedPath = path.StartsWith("/") ? path.Substring(1) : path;
            string localAbsolutePath = $"{LocalWorkingPath}/{correctedPath}";

            return GetLocalFilesAtAbsolutePath(localAbsolutePath, SearchOption.TopDirectoryOnly);
        }

        private Dictionary<string, FileDetails> GetLocalFilesAtAbsolutePath(string absoluteLocalPath, SearchOption searchOption) 
        {
            Dictionary<string, FileDetails> localFiles = new();

            if (string.IsNullOrWhiteSpace(absoluteLocalPath))
                throw new ArgumentException("Path cannot be null or empty.", nameof(absoluteLocalPath));

            if (!Directory.Exists(absoluteLocalPath))
                Directory.CreateDirectory(absoluteLocalPath);

            try
            {
                // Get all files recursively
                string[] files = Directory.GetFiles(absoluteLocalPath, "*", SearchOption.TopDirectoryOnly);
                foreach (string file in files)
                {
                    FileInfo fileInfo = new(file);
                    string correctedFileFullName = fileInfo.FullName.Replace("\\", "/"); //System.IO gives us paths with back slashes
                    string workingFileNameAndPath = correctedFileFullName.Replace($"{LocalWorkingPath}/", "").TrimStart('/');
                    localFiles.Add(workingFileNameAndPath, new FileDetails { fileNameAndWorkingPath = workingFileNameAndPath, fileSize = (ulong)fileInfo.Length });
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

            return localFiles;
        }

        public List<string> GetAllLocalFoldersAtPath(string path)
        {
            List<string> localFolders = new();
            string correctedPath = path.StartsWith("/") ? path.Substring(1) : path;
            string localPath = $"{LocalWorkingPath}/{correctedPath}";

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
                    string correctedFolderFullName = folder.Replace("\\", "/"); //System.IO gives us paths with back slashes
                    string workingFolderNameAndPath = correctedFolderFullName.Replace($"{LocalWorkingPath}/", "").TrimStart('/');
                    localFolders.Add(workingFolderNameAndPath);
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

            return localFolders;
        }

        #endregion


        private readonly FTPService _ftpService;
        public readonly string LocalWorkingPath;

        public readonly string RemoteWorkingPath;

        public FileSystemService(FTPService ftpService, string remoteWorkingPath, string localWorkingPath)
        {
            _ftpService = ftpService;

            RemoteWorkingPath = remoteWorkingPath;
            LocalWorkingPath = localWorkingPath;
        }

        private void OnRemoteDownloadComplete(FTPTask task)
        {
            task.OnComplete -= OnRemoteDownloadComplete;
        }

        public void TearDown() => _ftpService.TearDown();
    }
}
