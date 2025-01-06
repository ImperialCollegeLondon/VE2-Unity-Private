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

    //private static string CorrectLocalPath(string path) => path.Replace("/", "\\");
    private static string CorrectLocalPath(string path) => path.Replace("/", "\\");
}

public class FileStorageService
{
    #region higher-level interfaces 
    public bool IsFileStorageServiceReady => _ftpService.IsFTPServiceReady;
    public event Action OnFileStorageServiceReady { add { _ftpService.OnFTPServiceReady += value; } remove { _ftpService.OnFTPServiceReady -= value; } }
    public event Action OnLocalFilesRefreshed;
    public event Action OnRemoteFilesRefreshed { add { _ftpService.OnRemoteFileListUpdated += value; } remove { _ftpService.OnRemoteFileListUpdated -= value; } }

    public FTPDownloadTask DownloadFile(string workingFileNameAndPath)
    {
        //Client should just say "test.txt" or "/folder/test.txt"

        // if (!workingFileNameAndPath.StartsWith("/"))
        //     workingFileNameAndPath = "/" + workingFileNameAndPath;

        string fileName = workingFileNameAndPath.Substring(workingFileNameAndPath.LastIndexOf("/") + 1);
        
        string remotePathFromWorking = workingFileNameAndPath.Contains("/") ? workingFileNameAndPath.Substring(0, workingFileNameAndPath.LastIndexOf("/")) : "";

        // string remotePath = RemoteWorkingPath + "/" + remotePathFromWorking;
        // string localPath = LocalWorkingPath + "\\" + CorrectLocalPath(remotePathFromWorking);

        //Debug.Log($"Downloading file {fileName} from {remotePath} to {localPath}, started with {workingFileNameAndPath}");
        //Debug.Log($"Downloading file {fileName} at {remotePathFromWorking}, started with {workingFileNameAndPath}");

        FTPDownloadTask task = _ftpService.DownloadFile(remotePathFromWorking, fileName);
        task.OnComplete += OnRemoteDownloadComplete;
        return task;
    }

    public readonly Dictionary<string, FileDetails> localFiles = new();
    //Need to show things in queue, and things in progress

    public Dictionary<string, FTPFileTransferTask> QueuedTransferTasks;
    public FTPFileTransferTask currentTransferTask;

    public event Action OnFileTransferComplete; //TODO: Do we need this? The file list updates tell us this anyways 

    public Dictionary<string, FileDetails> RemoteFiles => _ftpService.RemoteFiles;

    public void RefreshLocalFiles()
    {
        localFiles.Clear();

        //Debug.Log("Searching for local files in " + LocalWorkingPath);

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
                //Debug.Log("<color=green>Found local file: " + fileInfo.FullName + " - local working = " + LocalWorkingPath + " contains? " + fileInfo.FullName.Contains(LocalWorkingPath) + "</color>");
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

    public FileStorageService(FTPService ftpService, string remoteWorkingPath, string localWorkingPath) //Path will either be Worlds or e.g PluginFiles/{worldName}
    {
        _ftpService = ftpService;

        //_remoteWorkingPath =  "VE2/" + workingPath;
        RemoteWorkingPath = remoteWorkingPath;
        LocalWorkingPath = localWorkingPath;

        RefreshLocalFiles();
    }

    private void OnRemoteDownloadComplete(FTPFileTransferTask task)
    {
        task.OnComplete -= OnRemoteDownloadComplete;

        Debug.Log("Download complete");
        Debug.Log(task.CompletionCode);

        RefreshLocalFiles(); //TODO, could just add the file rather than searching for it?
    }

    public void TearDown() => _ftpService.TearDown();
}
