using System;
using System.Collections.Generic;
using System.IO;
using Renci.SshNet;
using UnityEngine;

public static class FileStorageServiceFactory
{
    public static FileStorageService CreateFileStorageService(FTPNetworkSettings ftpNetworkSettings, string workingPath)
    {
        SftpClient sftpClient = new(ftpNetworkSettings.IP, ftpNetworkSettings.Port, ftpNetworkSettings.Username, ftpNetworkSettings.Password);
        FTPCommsHandler commsHandler = new(sftpClient);
        FTPService ftpService = new(commsHandler, workingPath);

        return new FileStorageService(ftpService, workingPath);
    }
}

public class FileStorageService
{
    #region higher-level interfaces 
    public bool IsFileStorageServiceReady => _ftpService.IsFTPServiceReady;
    public event Action OnFileStorageServiceReady { add { _ftpService.OnFTPServiceReady += value; } remove { _ftpService.OnFTPServiceReady -= value; } }
    public event Action OnLocalFilesRefreshed;
    public event Action OnRemoteFilesRefreshed { add { _ftpService.OnRemoteFileListUpdated += value; } remove { _ftpService.OnRemoteFileListUpdated -= value; } }

    public readonly Dictionary<string, FileDetails> localFiles = new();
    //Need to show things in queue, and things in progress

    public Dictionary<string, FTPFileTransferTask> QueuedTransferTasks;
    public FTPFileTransferTask currentTransferTask;

    public event Action OnFileTransferComplete;

    public Dictionary<string, FileDetails> RemoteFiles => _ftpService.RemoteFiles;

    public void RefreshLocalFiles()
    {
        localFiles.Clear();
        FindLocalFilesInFolderAndSubFolders(LocalWorkingPath);
        OnLocalFilesRefreshed?.Invoke();
    }

    public void RefreshRemoteFiles() => _ftpService.RefreshRemoteFileList();
    #endregion


    private readonly FTPService _ftpService;
    public readonly string LocalWorkingPath;

    public FileStorageService(FTPService ftpService, string workingPath) //Path will either be Worlds or e.g PluginFiles/{worldName}
    {
        _ftpService = ftpService;

        //_remoteWorkingPath =  "VE2/" + workingPath;
        LocalWorkingPath = Application.persistentDataPath + "\\files\\" + CorrectLocalPath(workingPath);

        RefreshLocalFiles();
    }

    private void FindLocalFilesInFolderAndSubFolders(string path)
    {
        Debug.Log("Searching for local files in " + path);

        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path cannot be null or empty.", nameof(path));

        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        try
        {
            // Get all files recursively
            string[] files = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
            foreach (string file in files)
            {
                FileInfo fileInfo = new(file);
                localFiles.Add(fileInfo.FullName, new FileDetails{ fileName =  fileInfo.FullName, fileSize = (ulong)fileInfo.Length});
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
    }

    public void TearDown() => _ftpService.TearDown();

    private static string CorrectLocalPath(string path) => path.Replace("/", "\\");
}
