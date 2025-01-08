using System;
using System.Collections.Generic;
using System.IO;
using Renci.SshNet;
using UnityEngine;

public static class FileStorageServiceFactory
{
    public static FileStorageService CreateFileStorageService(FTPNetworkSettings ftpNetworkSettings, string workingPath)
    {
        string localWorkingPath = Application.persistentDataPath + "/files/" + workingPath; //TODO: path combine? Do that everywhere actually
        string remoteWorkingPath = workingPath;

        SftpClient sftpClient = new(ftpNetworkSettings.IP, ftpNetworkSettings.Port, ftpNetworkSettings.Username, ftpNetworkSettings.Password);
        FTPCommsHandler commsHandler = new(sftpClient);
        FTPService ftpService = new(commsHandler, remoteWorkingPath, localWorkingPath);

        return new FileStorageService(ftpService, remoteWorkingPath, localWorkingPath);
    }
}

public class FileStorageService //TODO: Rename, FileExchangeService? LocalRemoteFileService?
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

    public void DeleteLocalFile(string workingFileNameAndPath)
    {
        Debug.Log($"Deleting local file: {workingFileNameAndPath}");
        string localPath = $"{LocalWorkingPath}/{workingFileNameAndPath}"; //TODO: combine

        if (File.Exists(localPath))
        {
            File.Delete(localPath);
            RefreshLocalFiles();
        }
        else 
        {
            Debug.LogWarning($"File not found: {localPath}");
        }
    }

    public readonly Dictionary<string, FileDetails> localFiles = new();
    public Dictionary<string, FileDetails> RemoteFiles => _ftpService.RemoteFiles;

    public void RefreshLocalFiles()
    {
        localFiles.Clear();

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

        OnLocalFilesRefreshed?.Invoke();
    }

    public void RefreshRemoteFiles() => _ftpService.RefreshRemoteFileList();
    #endregion


    private readonly FTPService _ftpService;
    public readonly string LocalWorkingPath;

    public readonly string RemoteWorkingPath;

    public FileStorageService(FTPService ftpService, string remoteWorkingPath, string localWorkingPath) 
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
