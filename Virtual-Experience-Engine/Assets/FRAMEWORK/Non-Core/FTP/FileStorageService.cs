using System;
using System.Collections.Generic;
using System.IO;
using Renci.SshNet;
using UnityEngine;

public static class FileStorageServiceFactory
{
    public static FileStorageService CreateFileStorageService(FTPNetworkSettings ftpNetworkSettings, string workingPath)
    {
        string localWorkingPath = CorrectLocalPath(Application.persistentDataPath + "\\files\\" + workingPath);
        string remoteWorkingPath = workingPath;

        SftpClient sftpClient = new(ftpNetworkSettings.IP, ftpNetworkSettings.Port, ftpNetworkSettings.Username, ftpNetworkSettings.Password);
        FTPCommsHandler commsHandler = new(sftpClient);
        FTPService ftpService = new(commsHandler, remoteWorkingPath, localWorkingPath);

        return new FileStorageService(ftpService, remoteWorkingPath, localWorkingPath);
    }

    private static string CorrectLocalPath(string path) => path.Replace("/", "\\");
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
        string fileName = workingFileNameAndPath.Substring(workingFileNameAndPath.LastIndexOf("\\") + 1);
        string remoteCorrectedFileNameAndPath = workingFileNameAndPath.Replace("\\", "/");
        string remotePathFromWorking = remoteCorrectedFileNameAndPath.Contains("/") ? remoteCorrectedFileNameAndPath.Substring(0, remoteCorrectedFileNameAndPath.LastIndexOf("/")) : "";
        FTPUploadTask task = _ftpService.UploadFile(remotePathFromWorking, fileName); //No need to refresh manually, will happen automatically
        return task;
    }

    public List<RemoteFileTaskDetails> GetAllUpcomingFileTransferDetails() => _ftpService.GetAllUpcomingFileTransferDetails();

    public readonly Dictionary<string, FileDetails> localFiles = new();
    //Need to show things in queue, and things in progress

    //TODO: are we using these?
    public Dictionary<string, FTPFileTransferTask> QueuedTransferTasks;
    public FTPFileTransferTask currentTransferTask;

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
                string workingFileNameAndPath = fileInfo.FullName.Replace($"{LocalWorkingPath}\\", "").TrimStart('\\');
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

    private void OnRemoteDownloadComplete(FTPFileTransferTask task)
    {
        task.OnComplete -= OnRemoteDownloadComplete;

        //Could add file manually rather than refreshing, but local refresh doesn't take long 
        RefreshLocalFiles(); 
    }

    public void TearDown() => _ftpService.TearDown();
}
