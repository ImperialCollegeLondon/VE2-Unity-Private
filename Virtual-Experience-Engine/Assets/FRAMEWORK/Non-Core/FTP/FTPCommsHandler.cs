
using Renci.SshNet;
using Renci.SshNet.Sftp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using FTPStatus = IFTPCommsHandler.FTPStatus;
using CompletionCodes = IFTPCommsHandler.CompletionCodes;
using UnityEngine;


public interface IFTPCommsHandler
{
    public FTPStatus Status { get; }
    public event Action<FTPStatus> OnStatusChanged;
    public enum FTPStatus { NotReady, Ready, Busy };

    public enum CompletionCodes { Success, Busy, CouldNotConnect, Cancelled, LocalFileError, RemoteFileError } //TODO: LocalFileError shouldn't live here?

    public void RetrieveFile(string remotePath, string localPath, string fileName);

    public void StoreFile(string remotePath, string localPath, string fileName);


    public void RemoveFileOrEmptyFolder(string remotePath, string fileName);

    public void CancelAction();
 
    public void MakeFolder(string remotePath, string newFolder);

    public void GetFolderList(string remotePath);
    public void GetFileList(string remotePath);

    public struct FileDetails
    {
        public string fileName;
        public ulong fileSize;
    }
}

public class FTPCommsHandler : IFTPCommsHandler
{
    private FTPStatus _status;
    public FTPStatus Status {
        get =>_status; 
        private set 
        {
            if (value != Status)
            {
                _status = value;
                OnStatusChanged?.Invoke(_status);
            }
        }
    }
    public event Action<FTPStatus> OnStatusChanged;

    ///private stuff

    private readonly SftpClient _sftpClient;
    //private readonly FTPNetworkSettings _ftpNetworkSettings;

    //Implement interface

    public FTPCommsHandler(SftpClient sftpClient) //CLient comes already instantiated with network details //TODO: need to inject remote file path?
    {
        _sftpClient = sftpClient;
        Status = FTPStatus.Ready;
    }

    bool cancel = false;
    public void CancelAction()
    {
        if (Status == FTPStatus.Busy)
            cancel = true;
    }

    public async void GetFileList(string remotePath)
    {
        if (Status != FTPStatus.Ready)
        {
            //error handling - I'm busy or not available
            manager.FileListComplete(remotePath, null, CompletionCodes.Busy);
            return;
        }
        else
        {
            if (remotePath.Contains("..")) //nope!
            {
                manager.FileListComplete(remotePath, null, CompletionCodes.RemoteFileError);
                return;
            }

            Status = FTPStatus.Busy;

            completionCode = CompletionCodes.Success; //may get set otherwise

            await DirectoryThreaded(remotePath);

            cancel = false; //any cancelling should be removed and ignored - not cancellable

            if (completionCode == CompletionCodes.Success)
            {
                List<V_ILFM_Manager.V_LargeFileDetails> returnFiles = new List<V_ILFM_Manager.V_LargeFileDetails>();
                foreach (SftpFile file in files)
                {
                    if (!file.IsDirectory)
                        returnFiles.Add(new V_ILFM_Manager.V_LargeFileDetails() { fileName = file.Name, fileSize = (ulong)file.Length });
                }
                manager?.FileListComplete(remotePath, returnFiles, completionCode);
            }
            else
                manager?.FileListComplete(remotePath, null, completionCode);

            Status = FTPStatus.Ready;

        }
    }


    public async void MakeFolder(string remotePath, string newFolder)
    {
        if (Status !=FTPStatus.Ready)
        {
            //error handling - I'm busy or not available
            manager.MakeFolderComplete(remotePath, newFolder, CompletionCodes.Busy);
            return;
        }
        else
        {
            if (remotePath.Contains("..") || newFolder.Contains("..")) //nope!
            {
                manager.MakeFolderComplete(remotePath, newFolder, CompletionCodes.RemoteFileError);
                return;
            }
            if (FileNameValid(newFolder))
            {
                Status = FTPStatus.Busy;
                completionCode = CompletionCodes.Success; //may get set otherwise
                await MakeFolderThreaded(remotePath + "/" + newFolder);

                manager.MakeFolderComplete(remotePath, newFolder, completionCode);
                cancel = false; //any cancelling should be removed and ignored - not cancellable
                Status = FTPStatus.Ready;
            }
            else
            {
                manager.MakeFolderComplete(remotePath, newFolder, CompletionCodes.RemoteFileError);
                Status = FTPStatus.Ready;
            }
        }

    }

    public async void RemoveFileOrEmptyFolder(string remotePath, string fileName)
    {
        if (Status != FTPStatus.Ready)
        {
            //error handling - I'm busy or not available
            manager.FileOrFolderRemovalComplete(remotePath, fileName, CompletionCodes.Busy);
            return;
        }
        else
        {
            if (remotePath.Contains("..")) //nope!
            {
                manager.FileOrFolderRemovalComplete(remotePath, fileName, CompletionCodes.RemoteFileError);
                return;
            }
            if (FileNameValid(fileName))
            {

                Status = FTPStatus.Busy;
                completionCode = CompletionCodes.Success; //may get set otherwise
                await DeleteThreaded(remotePath + "/" + fileName);
                cancel = false; //any cancelling should be removed and ignored - not cancellable

                manager.FileOrFolderRemovalComplete(remotePath, fileName, completionCode);
                Status = FTPStatus.Ready;
            }
            else
            {
                manager.FileOrFolderRemovalComplete(remotePath, fileName, CompletionCodes.RemoteFileError);
                Status = FTPStatus.Ready;
            }
        }
    }

    public async void RetrieveFile(string remotePath, string localPath, string fileName)
    {

        if (Status != V_ILFM_Client.Status.Ready)
        {
            //error handling - I'm busy or not available
            manager.FileDownloadComplete(remotePath, fileName, CompletionCodes.Busy);
            return;
        }
        else
        {
            cancel = false; //reset at start of task or we CAN get them hanging
            if (remotePath.Contains("..")) //nope!
            {
                manager.FileDownloadComplete(remotePath, fileName, CompletionCodes.RemoteFileError);
                return;
            }
            if (FileNameValid(fileName))
            {

                Status = FTPStatus.Busy;
                completionCode = CompletionCodes.Success; //may get set otherwise
                await DownloadThreaded(remotePath + "/" + fileName, localPath + "\\" + fileName);

                manager.FileDownloadComplete(remotePath, fileName, completionCode);
                Status = FTPStatus.Ready;
            }
            else
            {
                manager.FileDownloadComplete(remotePath, fileName, CompletionCodes.RemoteFileError);
                Status = FTPStatus.Ready;
            }
        }
    }

    private bool FileNameValid(string name)
    {
        if (name.Contains("/") || name.Contains("\\")) return false; //none of your ../.. jailbreaking!

        //more checks stolen from https://stackoverflow.com/questions/4650462/easiest-way-to-check-if-an-arbitrary-string-is-a-valid-filename
        if (string.IsNullOrWhiteSpace(name)) return false;
        if (name.Length > 1 && name[1] == ':')
        {
            if (name.Length < 4 || name.ToLower()[0] < 'a' || name.ToLower()[0] > 'z') return false;
            name = name.Substring(3);
        }

        return true;

    }

    public async void StoreFile(string remotePath, string localPath, string fileName)
    {
        if (Status != FTPStatus.Ready)
        {
            //error handling - I'm busy or not available
            manager.FileUploadComplete(remotePath, fileName, CompletionCodes.Busy);
            return;
        }
        else
        {
            cancel = false; //reset at start of task or we CAN get them hanging
            if (remotePath.Contains("..")) //nope!
            {
                manager.FileUploadComplete(remotePath, fileName, CompletionCodes.RemoteFileError);
                return;
            }
            if (FileNameValid(fileName))
            {
                Status = FTPStatus.Busy;
                completionCode = CompletionCodes.Success; //may get set otherwise
                await UploadThreaded(remotePath + "/" + fileName, localPath + "\\" + fileName);

                manager.FileUploadComplete(remotePath, fileName, completionCode);
                Status = FTPStatus.Ready;
            }
            else
            {
                manager.FileUploadComplete(remotePath, fileName, CompletionCodes.LocalFileError);
                Status = FTPStatus.Ready;
            }
        }
    }


    private string remotePath;
    private long remoteFileSize;
    private FileStream transferStream;
    //private CompletionCodes completionCode = CompletionCodes.Success;


    private Task MakeFolderThreaded(string remoteFolder)
    {
        //Have to run this in different thread, no proper async support in SSH library
        return Task.Run(() =>
        {
            remotePath = remoteFolder; //for return value

            try
            {
                _sftpClient.Connect();
            }
            catch
            {
                completionCode = CompletionCodes.CouldNotConnect;
                Debug.LogError("SFTP: Could not connect (is network down?)");
                return;
            }

            //OK connected, try it
            try
            {
                _sftpClient.CreateDirectory(remoteFolder);
            }
            catch
            {
                //Nope. 
                completionCode = CompletionCodes.RemoteFileError;
            }

            try
            {
                //surely these can't go wrong?
                _sftpClient.Disconnect();
            }
            catch
            {
                Debug.LogError("SFTP: Could not disconnect after make folder - this is not a good sign.");
            }
        });
    }

    private Task DeleteThreaded(string remoteFile)
    {
        //Have to run this in different thread, no proper async support in SSH library
        return Task.Run(() =>
        {
            remotePath = remoteFile; //for return value

            try
            {
                _sftpClient.Connect();
            }
            catch
            {
                completionCode = CompletionCodes.CouldNotConnect;
                Debug.LogError("SFTP: Could not connect (is network down?)");
                return;
            }

            SftpFileAttributes attribs;
            //Get file size (for progress calculation)
            try
            {
                attribs = _sftpClient.GetAttributes(remoteFile);
            }
            catch
            {
                //Could not get attributes, probably does not exist
                completionCode = CompletionCodes.RemoteFileError;
                _sftpClient.Disconnect(); //break connection
                return;
            }

            if (!attribs.IsRegularFile && !attribs.IsDirectory)
            {
                completionCode = CompletionCodes.RemoteFileError;
                _sftpClient.Disconnect(); //break connection
                return;
            }

            //OK, it looks like a file or directory. Proceed!

            //OK connected, try to get file
            try
            {
                //SetProgress is internal progress callback
                if (attribs.IsRegularFile)
                    _sftpClient.DeleteFile(remoteFile);
                else
                    _sftpClient.DeleteDirectory(remoteFile);
            }
            catch
            {
                //Nope. 
                completionCode = CompletionCodes.RemoteFileError;
            }

            try
            {
                //surely these can't go wrong?
                _sftpClient.Disconnect();
            }
            catch
            {
                Debug.LogError("SFTP: Could not disconnect after download - this is not a good sign.");
            }
        });
    }


    //private IEnumerable<ISftpFile> files;


    private Task DownloadThreaded(string remoteFile, string localFile)
    {
        //Have to run this in different thread, no proper async support in SSH library
        return Task.Run(() =>
        {
            remotePath = remoteFile; //for return value

            try
            {
                _sftpClient.Connect();
            }
            catch
            {
                completionCode = CompletionCodes.CouldNotConnect;
                Debug.LogError("SFTP: Could not connect (is network down?)");
                return;
            }

            SftpFileAttributes attribs;
            //Get file size (for progress calculation)
            try
            {
                attribs = _sftpClient.GetAttributes(remoteFile);
            }
            catch
            {
                //Could not get attributes, probably does not exist
                completionCode = CompletionCodes.RemoteFileError;
                Debug.LogError($"attrib error {remoteFile}");
                _sftpClient.Disconnect(); //break connection
                return;
            }

            if (!attribs.IsRegularFile)
            {
                completionCode = CompletionCodes.RemoteFileError;
                Debug.LogError("notfileerror");
                _sftpClient.Disconnect(); //break connection
                return;
            }

            //OK, it looks like a file. Proceed!
            remoteFileSize = attribs.Size;
            lastDownloadedBytes = 0;   //to enable callback to sparsify calls to manager

            //Check I can open local file
            try
            {
                transferStream = File.OpenWrite(localFile);
            }
            catch
            {
                completionCode = CompletionCodes.LocalFileError;
                _sftpClient.Disconnect(); //break connection
                return;
            }

            //OK connected, try to get file
            try
            {
                //SetProgress is internal progress callback
                _sftpClient.DownloadFile(remoteFile, transferStream, SetProgress);
            }
            catch
            {
                //Nope. It was probably cancelled
                if (cancel)
                    completionCode = CompletionCodes.Cancelled;
                else
                {
                    completionCode = CompletionCodes.RemoteFileError;
                    Debug.LogError("Final error");
                }
            }
            transferStream.Close();
            transferStream.Dispose();
            transferStream = null;

            try
            {
                //surely these can't go wrong? catch it anyway
                _sftpClient.Disconnect();

                //cancelled - so delete. No partial downloads here!
                if (completionCode == CompletionCodes.Cancelled)
                {
                    UnityEngine.Debug.Log("Cancelled in flight-  deleting local file " + localFile);
                    File.Delete(localFile);
                }
            }
            catch
            {
                Debug.LogError("SFTP: Could not disconnect (or locally delete?) cancelled download - this is probably a bad sign.");
                completionCode = CompletionCodes.RemoteFileError;
            }
        });
    }

    private Task UploadThreaded(string remoteFile, string localFile)
    {
        //Have to run this in different thread, no proper async support in SSH library
        return Task.Run(() =>
        {
            remotePath = remoteFile; //for return value

            try
            {
                FileInfo f = new FileInfo(localFile);
                if (!f.Exists)
                {
                    completionCode = CompletionCodes.LocalFileError;
                    return;
                }
                remoteFileSize = f.Length;
            }
            catch
            {
                //maybe not possible? Catch it anyway
                Debug.LogError("SFTP: Could not open local file for upload");
                completionCode = CompletionCodes.LocalFileError;
                return;
            }

            //OK.. there IS a file


            //Check I can open it
            try
            {
                transferStream = File.OpenRead(localFile);
            }
            catch
            {
                Debug.LogError("SFTP: Could not open local file for upload");
                completionCode = CompletionCodes.LocalFileError;
                return;
            }


            //Local file is fine
            try
            {
                _sftpClient.Connect();
            }
            catch
            {
                completionCode = CompletionCodes.CouldNotConnect;
                Debug.LogError("SFTP: Could not connect. Oh dear. Is network down?");
                return;
            }



            lastDownloadedBytes = 0;   //to enable callback to sparsify calls to manager

            //OK connected, try to store it
            try
            {
                //SetProgress is internal progress callback
                _sftpClient.UploadFile(transferStream, remoteFile, SetProgress);
            }
            catch
            {
                //Nope. It was probably cancelled? Or maybe folder just doesn't exis
                if (cancel)
                    completionCode = CompletionCodes.Cancelled;
                else
                    completionCode = CompletionCodes.RemoteFileError;
            }
            transferStream.Close();
            transferStream.Dispose();
            transferStream = null;

            if (completionCode == CompletionCodes.Cancelled)
            {
                //TRY to delete remote file
                try
                {
                    _sftpClient.Disconnect();
                    _sftpClient.Connect();
                    _sftpClient.DeleteFile(remoteFile);
                }
                catch
                {
                    Debug.LogError("Failed to delete remote partial upload");
                    //; //probably nothing to do - could not delete, maybe done by system
                }

            }

            //surely THIS can't go wrong?
            try
            {
                _sftpClient.Disconnect();
            }
            catch
            {
                Debug.LogError("SFTP: Could not disconnect after upload. No idea why not - panic time?");
            }
        });
    }

    private ulong lastDownloadedBytes = 0;

    /// <summary>
    /// This is called by SSH system frequently (who knows how often - but too often I think)
    /// during uploads or downloads
    /// 
    /// Handles cancel mechanism, and sporadic callbacks through interface to framework
    /// </summary>
    /// <param name="uploaded"></param>
    private void SetProgress(ulong uploaded)
    {
        if (cancel)
        {
            if (transferStream != null)
                transferStream.Close();  //this breaks the transfer - it will exception out

            cancel = false; //cancel has been done
        }
        else
        {
            //call framework with update every 5k, or if this is first call
            if (lastDownloadedBytes == 0 || (uploaded - lastDownloadedBytes) > 5000)
            {
                lastDownloadedBytes = uploaded;
                manager?.ProgressChanged(((float)uploaded) / ((float)remoteFileSize));
            }
        }
    }

    public async void GetFolderList(string remotePath)
    {
        if (Status != FTPStatus.Ready)
        {
            //error handling - I'm busy or not available
            manager.FolderListComplete(remotePath, null, CompletionCodes.Busy);
            return;
        }
        else
        {
            if (remotePath.Contains("..")) //nope!
            {
                manager.FolderListComplete(remotePath, null, CompletionCodes.RemoteFileError);
                return;
            }
            Status = FTPStatus.Busy;

            (IEnumerable<ISftpFile>, CompletionCodes) result = await DirectoryThreaded(remotePath);
            IEnumerable<ISftpFile> files = result.Item1;
            CompletionCodes completionCode = result.Item2;

            cancel = false; //any cancelling should be removed and ignored - not cancellable

            if (completionCode == CompletionCodes.Success)
            {
                List<string> returnFolders = new List<string>();
                foreach (SftpFile file in files)
                {
                    if (file.IsDirectory && file.Name.Substring(0, 1) != ".") //avoid .. and .
                        returnFolders.Add(file.Name);
                }
                manager?.FolderListComplete(remotePath, returnFolders, completionCode);
                Status = FTPStatus.Ready;
            }
            else
            {
                manager?.FolderListComplete(remotePath, null, completionCode);
                Status = FTPStatus.Ready;
            }
        }

    }

    private Task<(IEnumerable<ISftpFile>, CompletionCodes)> DirectoryThreaded(string remotePath)
    {
        return Task.Run(() =>
        {
            IEnumerable<ISftpFile> files = null;
            //V_Logger.Message($"SFTP: In DirThreaded {remotePath}");
            files = new List<SftpFile>();
            this.remotePath = remotePath; //for return value

            try
            {
                _sftpClient.Connect();
            }
            catch
            {
                Debug.LogError("SFTP: Could not connect (is network down?)");
                return (files, CompletionCodes.CouldNotConnect);
            }

            try
            {
                files = _sftpClient.ListDirectory(remotePath);
            }
            catch
            {
                //Could not list contents
                //Debug.LogError($"SFTP: Could not list contents of {remotePath}");
                _sftpClient.Disconnect(); //break connection
                return (files, CompletionCodes.RemoteFileError);
            }

            try
            {
                //surely these can't go wrong? catch it anyway
                _sftpClient.Disconnect();
            }
            catch
            {
                Debug.LogError("SFTP: Could not disconnect - this is probably a bad sign.");
                return (files, CompletionCodes.RemoteFileError);
            }

            return (files, CompletionCodes.Success);
        });

    }

}
