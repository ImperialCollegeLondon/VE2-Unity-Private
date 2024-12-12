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
        FTPService ftpService = new(commsHandler);

        return new FileStorageService(ftpService, workingPath);
    }
}

public class FileStorageService
{
    #region higher-level interfaces 
    public bool IsFileStorageServiceReady;
    public event Action OnFileStorageServiceReady;

    public readonly Dictionary<string, FileDetails> remoteFiles = new();
    public readonly Dictionary<string, FileDetails> localFiles = new();
    #endregion


    private readonly FTPService _ftpService;
    private readonly string _remoteWorkingPath;
    private readonly string _localWorkingPath;

    public FileStorageService(FTPService ftpService, string workingPath) //Path will either be Worlds or e.g PluginFiles/{worldName}
    {
        _ftpService = ftpService;

        _remoteWorkingPath =  "VE2/" + workingPath;
        _localWorkingPath = Application.persistentDataPath + "\\files" + CorrectLocalPath(workingPath);

        RefreshFileLists();
    }

    public void RefreshFileLists()
    {
        remoteFiles.Clear();
        localFiles.Clear();

        FindRemoteFilesInFolderAndSubFolders(_remoteWorkingPath);
        FindLocalFilesInFolderAndSubFolders(_localWorkingPath);
    }

    private void FindRemoteFilesInFolderAndSubFolders(string remoteFolderPath)
    {
        Debug.Log("Searching for remote files in " + remoteFolderPath);

        FTPRemoteFolderListTask subFolderListTask = _ftpService.GetRemoteFolderList(remoteFolderPath);
        subFolderListTask.OnComplete += HandleGetRemoteFolderListComplete;

        FTPRemoteFileListTask fileListTask = _ftpService.GetRemoteFileList(remoteFolderPath);
        fileListTask.OnComplete += HandleGetRemoteFileListComplete;
    }

    private void HandleGetRemoteFolderListComplete(FTPRemoteFolderListTask folderListTask)
    {
        folderListTask.OnComplete -= HandleGetRemoteFolderListComplete;

        Debug.Log("Done get remote folder list " + folderListTask.CompletionCode + " found folders " + folderListTask.FoundFolderNames.Count);

        if (folderListTask.CompletionCode != FTPCompletionCode.Success)
        {
            Debug.LogError($"Failed to get remote folder list: {folderListTask.CompletionCode}");
            return;
        }

        foreach (string subFolder in folderListTask.FoundFolderNames) //TODO: Does this return the full folder path, like VE2/PluginFiles/WorldName/Folder1/Folder2? or does it just return Folder2?
        {
            FindRemoteFilesInFolderAndSubFolders($"{folderListTask.RemotePath}/{subFolder}");

            //Do we want to create a local folder for each remote folder now?
            //May as well, I suppose? 
            // if (!Directory.Exists(_localWorkingPath + "\\" + subFolder))
            // {
            //     Directory.CreateDirectory(_localWorkingPath + "\\" + subFolder);
            // }
        }
    }

    private void HandleGetRemoteFileListComplete(FTPRemoteFileListTask fileListTask)
    {
        fileListTask.OnComplete -= HandleGetRemoteFileListComplete;

        Debug.Log("Done get remote file list " + fileListTask.CompletionCode + " found files " + fileListTask.FoundFilesDetails.Count); 

        if (fileListTask.CompletionCode != FTPCompletionCode.Success)
        {
            Debug.LogError($"Failed to get remote file list: {fileListTask.CompletionCode}");
            return;
        }

        foreach (FileDetails fileDetails in fileListTask.FoundFilesDetails)
        {
            Debug.Log("Add Remote file: " + fileDetails.fileName + " - " + fileDetails.fileSize);
            string fileNameAndPath = $"{fileListTask.RemotePath}/{fileDetails.fileName}";
            string workingFileNameAndPath = fileNameAndPath.Replace($"{_remoteWorkingPath}/", "");

            remoteFiles.Add(workingFileNameAndPath, fileDetails); //TODO: key should be full path here - confirmed the key is just the file name, we want the full path
        }

        Debug.Log("Ready? " + IsFileStorageServiceReady + " Busy? " + _ftpService.IsBusy);
        //Once we've found all remote files, the service is ready 
        if (!IsFileStorageServiceReady && !_ftpService.IsBusy)
        {
            Debug.Log("File storage service is ready!");
            IsFileStorageServiceReady = true;
            OnFileStorageServiceReady?.Invoke();
        }
    }

    private void FindLocalFilesInFolderAndSubFolders(string path)
    {
        Debug.Log("Searching for local files in " + path);

        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path cannot be null or empty.", nameof(path));

        if (!Directory.Exists(path))
            return; //No files!

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

    private string CorrectLocalPath(string path) => path.Replace("/", "\\");
}
