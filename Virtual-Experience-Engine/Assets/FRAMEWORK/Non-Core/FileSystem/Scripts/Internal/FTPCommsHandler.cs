
using Renci.SshNet;
using Renci.SshNet.Common;
using Renci.SshNet.Sftp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace VE2.NonCore.FileSystem.Internal
{
    internal enum FTPStatus { NotReady, Ready, Busy };

    internal enum FTPCompletionCode { Waiting, Success, Busy, CouldNotConnect, Cancelled, LocalFileError, RemoteFileError }

    internal struct FileDetails
    {
        public string fileName;
        public ulong fileSize;
    }

    internal interface IFTPCommsHandler
    {
        public FTPStatus Status { get; }
        public event Action<FTPStatus> OnStatusChanged;

        public void GetFileList(FTPRemoteFileListTask fileListTask);
        public void GetFolderList(FTPRemoteFolderListTask folderListTask);
        public void MakeFolder(FTPMakeFolderTask makeFolderTask);
        public void RemoveFileOrEmptyFolder(FTPDeleteTask deleteTask);
        public void DownloadFile(FTPDownloadTask downloadTask);
        public void UploadFile(FTPUploadTask uploadTask);
    }

    internal class FTPCommsHandler : IFTPCommsHandler
    {
        private FTPStatus _status;
        public FTPStatus Status
        {
            get => _status;
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

        private FileStream transferStream;
        private FTPFileTransferTask _currentFileTransferTask;

        private readonly SftpClient _sftpClient;

        public FTPCommsHandler(SftpClient sftpClient) //Client comes already instantiated with network details
        {
            _sftpClient = sftpClient;
            Status = FTPStatus.Ready;
        }


        #region Get directory info
        //############################################################################################################################################

        public async void GetFileList(FTPRemoteFileListTask fileListTask)
        {
            if (Status != FTPStatus.Ready)
            {
                //error handling - I'm busy or not available
                fileListTask.MarkCompleted(FTPCompletionCode.Busy);
                return;
            }
            else
            {
                if (fileListTask.RemotePath.Contains("..")) //nope!
                {
                    fileListTask.MarkCompleted(FTPCompletionCode.RemoteFileError);
                    return;
                }

                Status = FTPStatus.Busy;

                fileListTask.MarkInProgress();
                (IEnumerable<ISftpFile>, FTPCompletionCode) result = await DirectoryThreaded(fileListTask.RemotePath);
                IEnumerable<ISftpFile> files = result.Item1;
                FTPCompletionCode completionCode = result.Item2;

                if (completionCode == FTPCompletionCode.Success)
                {
                    List<FileDetails> returnFiles = new();
                    foreach (SftpFile file in files)
                    {
                        if (!file.IsDirectory)
                            returnFiles.Add(new FileDetails() { fileName = file.Name, fileSize = (ulong)file.Length });
                    }

                    fileListTask.FoundFilesDetails = returnFiles;
                    fileListTask.MarkCompleted(FTPCompletionCode.Success);
                }
                else
                {
                    fileListTask.MarkCompleted(completionCode);
                }

                Status = FTPStatus.Ready;

            }
        }

        public async void GetFolderList(FTPRemoteFolderListTask folderListTask)
        {
            if (Status != FTPStatus.Ready)
            {
                folderListTask.MarkCompleted(FTPCompletionCode.Busy);
                return;
            }

            if (folderListTask.RemotePath.Contains(".."))
            {
                folderListTask.MarkCompleted(FTPCompletionCode.RemoteFileError);
                return;
            }

            Status = FTPStatus.Busy;

            folderListTask.MarkInProgress();
            (IEnumerable<ISftpFile> files, FTPCompletionCode completionCode) = await DirectoryThreaded(folderListTask.RemotePath);

            // Handle success or failure based on completion code
            if (completionCode == FTPCompletionCode.Success)
            {
                List<string> returnFolders = new List<string>();
                foreach (SftpFile file in files)
                {
                    if (file.IsDirectory && file.Name.Substring(0, 1) != ".") //avoid .. and 
                        returnFolders.Add(file.Name);
                }

                folderListTask.FoundFolderNames = returnFolders;
                folderListTask.MarkCompleted(completionCode);
            }
            else
            {
                folderListTask.MarkCompleted(completionCode);
            }

            Status = FTPStatus.Ready;
        }

        private Task<(IEnumerable<ISftpFile>, FTPCompletionCode)> DirectoryThreaded(string remotePath)
        {
            return Task.Run(() =>
            {
                IEnumerable<ISftpFile> files = null;
                //V_Logger.Message($"SFTP: In DirThreaded {remotePath}");
                files = new List<SftpFile>();

                try
                {
                    _sftpClient.Connect();
                }
                catch
                {
                    Debug.LogError("SFTP: Could not connect (is network down?)");
                    return (files, FTPCompletionCode.CouldNotConnect);
                }

                try
                {
                    files = _sftpClient.ListDirectory(remotePath);
                }
                catch (Exception ex)
                {
                    _sftpClient.Disconnect(); //break connection

                    if (ex is SftpPathNotFoundException)
                    {
                        files = new List<SftpFile>();
                        return (files, FTPCompletionCode.Success);
                    }
                    else 
                    {
                        Debug.LogError($"SFTP: Could not list contents of {remotePath} - {ex.Message}-{ex.StackTrace}");
                        return (files, FTPCompletionCode.RemoteFileError);
                    }
                }

                try
                {
                    //surely these can't go wrong? catch it anyway
                    _sftpClient.Disconnect();
                }
                catch
                {
                    Debug.LogError("SFTP: Could not disconnect - this is probably a bad sign.");
                    return (files, FTPCompletionCode.RemoteFileError);
                }

                return (files, FTPCompletionCode.Success);
            });
        }

        //############################################################################################################################################
        #endregion

        #region Create folder
        //############################################################################################################################################

        public async void MakeFolder(FTPMakeFolderTask makeFolderTask)
        {
            if (Status != FTPStatus.Ready)
            {
                makeFolderTask.MarkCompleted(FTPCompletionCode.Busy);
                return;
            }

            if (makeFolderTask.RemotePath.Contains("..") || makeFolderTask.Name.Contains(".."))
            {
                makeFolderTask.MarkCompleted(FTPCompletionCode.RemoteFileError);
                return;
            }

            if (FileNameValid(makeFolderTask.Name))
            {
                Status = FTPStatus.Busy;

                makeFolderTask.MarkInProgress();
                FTPCompletionCode completionCode = await MakeFolderThreaded(makeFolderTask);
                makeFolderTask.MarkCompleted(completionCode);

                Status = FTPStatus.Ready;
            }
            else
            {
                Debug.LogError("Invalid folder name - " + makeFolderTask.Name + " path is " + makeFolderTask.RemotePath);
                makeFolderTask.MarkCompleted(FTPCompletionCode.RemoteFileError);
                Status = FTPStatus.Ready;
            }
        }

        private Task<FTPCompletionCode> MakeFolderThreaded(FTPMakeFolderTask makeFolderTask)
        {
            return Task.Run(() =>
            {
                string remoteFolder = makeFolderTask.RemotePath.EndsWith("/")? $"{makeFolderTask.RemotePath}{makeFolderTask.Name}" : $"{makeFolderTask.RemotePath}/{makeFolderTask.Name}";
                FTPCompletionCode result = FTPCompletionCode.Success;

                try
                {
                    _sftpClient.Connect();
                }
                catch
                {
                    result = FTPCompletionCode.CouldNotConnect;
                    Debug.LogError("SFTP: Could not connect (is network down?)");
                    return result;
                }

                try
                {
                    _sftpClient.CreateDirectory(remoteFolder);
                }
                catch
                {
                    result = FTPCompletionCode.RemoteFileError;
                }

                try
                {
                    _sftpClient.Disconnect();
                }
                catch
                {
                    Debug.LogError("SFTP: Could not disconnect after make folder - this is not a good sign.");
                    result = FTPCompletionCode.RemoteFileError;
                }

                return result;
            });
        }

        //############################################################################################################################################
        #endregion

        #region Remove file or folder
        //############################################################################################################################################

        public async void RemoveFileOrEmptyFolder(FTPDeleteTask deleteTask)
        {
            if (Status != FTPStatus.Ready)
            {
                deleteTask.MarkCompleted(FTPCompletionCode.Busy);
                return;
            }

            if (deleteTask.RemotePath.Contains(".."))
            {
                deleteTask.MarkCompleted(FTPCompletionCode.RemoteFileError);
                return;
            }

            if (FileNameValid(deleteTask.Name))
            {
                Status = FTPStatus.Busy;

                deleteTask.MarkInProgress();
                FTPCompletionCode completionCode = await DeleteThreaded(deleteTask);
                deleteTask.MarkCompleted(completionCode);

                Status = FTPStatus.Ready;
            }
            else
            {
                deleteTask.MarkCompleted(FTPCompletionCode.RemoteFileError);
                Status = FTPStatus.Ready;
            }
        }

        private Task<FTPCompletionCode> DeleteThreaded(FTPDeleteTask deleteTask)
        {
            return Task.Run(() =>
            {
                string remoteFile = deleteTask.RemotePath.EndsWith("/")? $"{deleteTask.RemotePath}{deleteTask.Name}" : $"{deleteTask.RemotePath}/{deleteTask.Name}";
                FTPCompletionCode result = FTPCompletionCode.Success;

                try
                {
                    _sftpClient.Connect();
                }
                catch
                {
                    result = FTPCompletionCode.CouldNotConnect;
                    Debug.LogError("SFTP: Could not connect (is network down?)");
                    return result;
                }

                SftpFileAttributes attribs;
                try
                {
                    attribs = _sftpClient.GetAttributes(remoteFile);
                }
                catch
                {
                    result = FTPCompletionCode.RemoteFileError;
                    _sftpClient.Disconnect();
                    return result;
                }

                if (!attribs.IsRegularFile && !attribs.IsDirectory)
                {
                    result = FTPCompletionCode.RemoteFileError;
                    _sftpClient.Disconnect();
                    return result;
                }

                try
                {
                    if (attribs.IsRegularFile)
                        _sftpClient.DeleteFile(remoteFile);
                    else
                        _sftpClient.DeleteDirectory(remoteFile);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"SFTP: Could not perform delete {remoteFile} - " + ex.Message);
                    result = FTPCompletionCode.RemoteFileError;
                }

                try
                {
                    _sftpClient.Disconnect();
                }
                catch
                {
                    Debug.LogError("SFTP: Could not disconnect after delete - this is not a good sign.");
                    result = FTPCompletionCode.RemoteFileError;
                }

                return result;
            });
        }

        //############################################################################################################################################
        #endregion

        #region Download file
        //############################################################################################################################################

        public async void DownloadFile(FTPDownloadTask downloadTask)
        {
            if (Status != FTPStatus.Ready)
            {
                downloadTask.MarkCompleted(FTPCompletionCode.Busy);
                return;
            }

            if (downloadTask.RemotePath.Contains(".."))
            {
                downloadTask.MarkCompleted(FTPCompletionCode.RemoteFileError);
                return;
            }

            if (FileNameValid(downloadTask.Name))
            {
                Debug.Log("Starting download of " + downloadTask.Name);
                Status = FTPStatus.Busy;

                downloadTask.MarkInProgress();
                FTPCompletionCode completionCode = await DownloadThreaded(downloadTask);
                downloadTask.MarkCompleted(completionCode);

                Status = FTPStatus.Ready;
            }
            else
            {
                Debug.LogError("Invalid file name");
                downloadTask.MarkCompleted(FTPCompletionCode.RemoteFileError);
                Status = FTPStatus.Ready;
            }
        }

        private Task<FTPCompletionCode> DownloadThreaded(FTPDownloadTask downloadTask)
        {
            return Task.Run(() =>
            {
                _currentFileTransferTask = downloadTask;
                string remoteFile = downloadTask.RemotePath.EndsWith("/")? $"{downloadTask.RemotePath}{downloadTask.Name}" : $"{downloadTask.RemotePath}/{downloadTask.Name}";
                string localFile = downloadTask.LocalPath.EndsWith("/")? $"{downloadTask.LocalPath}{downloadTask.Name}" : $"{downloadTask.LocalPath}/{downloadTask.Name}";

                try
                {
                    _sftpClient.Connect();
                }
                catch (Exception ex)
                {
                    Debug.LogError("SFTP: Could not connect (is network down?) - " + ex.Message);
                    return FTPCompletionCode.CouldNotConnect;
                }

                SftpFileAttributes attribs;
                try
                {
                    attribs = _sftpClient.GetAttributes(remoteFile);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"attrib error {remoteFile} - {ex.Message}");
                    _sftpClient.Disconnect();
                    return FTPCompletionCode.RemoteFileError;
                }

                if (!attribs.IsRegularFile)
                {
                    Debug.LogError("notfileerror");
                    _sftpClient.Disconnect();
                    return FTPCompletionCode.RemoteFileError;
                }

                downloadTask.TotalFileSizeToTransfer = (ulong)attribs.Size;

                try
                {
                    transferStream = File.OpenWrite(localFile);
                }
                catch (Exception ex)
                {
                    _sftpClient.Disconnect();
                    Debug.LogError("SFTP: Could not open local file for download: " + ex.Message + " - " + localFile);
                    return FTPCompletionCode.LocalFileError;
                }

                FTPCompletionCode completionCode = FTPCompletionCode.Success; //May get set otherwise

                try
                {
                    _sftpClient.DownloadFile(remoteFile, transferStream, SetFileTransferTaskProgress);
                }
                catch
                {
                    completionCode = downloadTask.IsCancelled ? FTPCompletionCode.Cancelled : FTPCompletionCode.RemoteFileError;
                }
                finally
                {

                    transferStream.Close();
                    transferStream.Dispose();
                    transferStream = null;

                    try
                    {
                        _sftpClient.Disconnect();

                        if (completionCode == FTPCompletionCode.Cancelled)
                        {
                            Debug.Log("Cancelled in flight - deleting local file " + localFile);
                            File.Delete(localFile);
                        }
                    }
                    catch
                    {
                        Debug.LogError("SFTP: Could not disconnect (or locally delete?) cancelled download.");
                        completionCode = FTPCompletionCode.RemoteFileError;
                    }
                }

                return completionCode;
            });
        }

        //############################################################################################################################################
        #endregion

        #region Upload file
        //############################################################################################################################################
        public async void UploadFile(FTPUploadTask uploadFileTask)
        {
            if (Status != FTPStatus.Ready)
            {
                uploadFileTask.MarkCompleted(FTPCompletionCode.Busy);
                return;
            }

            if (uploadFileTask.RemotePath.Contains(".."))
            {
                uploadFileTask.MarkCompleted(FTPCompletionCode.RemoteFileError);
                return;
            }

            if (FileNameValid(uploadFileTask.Name))
            {
                Status = FTPStatus.Busy;

                uploadFileTask.MarkInProgress();

                FTPCompletionCode completionCode = await UploadThreaded(uploadFileTask);
                uploadFileTask.MarkCompleted(completionCode);
                Status = FTPStatus.Ready;
            }
            else
            {
                Debug.LogError("Invalid file name - " + uploadFileTask.Name);
                uploadFileTask.MarkCompleted(FTPCompletionCode.LocalFileError);
                Status = FTPStatus.Ready;
            }
        }

        private Task<FTPCompletionCode> UploadThreaded(FTPUploadTask uploadTask)
        {
            return Task.Run(() =>
            {
                _currentFileTransferTask = uploadTask;

                string remoteFile = uploadTask.RemotePath.EndsWith("/")? $"{uploadTask.RemotePath}{uploadTask.Name}" : $"{uploadTask.RemotePath}/{uploadTask.Name}";
                string localFile = uploadTask.LocalPath.EndsWith("/")? $"{uploadTask.LocalPath}{uploadTask.Name}" : $"{uploadTask.LocalPath}/{uploadTask.Name}";

                try
                {
                    FileInfo f = new FileInfo(localFile);
                    if (!f.Exists)
                    {
                        Debug.LogError("SFTP: Local file does not exist for upload");
                        return FTPCompletionCode.LocalFileError;
                    }
                    uploadTask.TotalFileSizeToTransfer = (ulong)f.Length;
                }
                catch
                {
                    // Handle any potential errors when getting file size
                    Debug.LogError("SFTP: Could not open local file for upload");
                    return FTPCompletionCode.LocalFileError;
                }

                // Check we can open the file for reading
                FileStream transferStream = null;
                try
                {
                    transferStream = File.OpenRead(localFile);
                }
                catch
                {
                    Debug.LogError("SFTP: Could not open local file for upload");
                    return FTPCompletionCode.LocalFileError;
                }

                // Attempt to connect to the SFTP server
                try
                {
                    _sftpClient.Connect();
                }
                catch
                {
                    Debug.LogError("SFTP: Could not connect. Is the network down?");
                    return FTPCompletionCode.CouldNotConnect;
                }

                FTPCompletionCode completionCode = FTPCompletionCode.Success; //May get set otherwise

                try
                {
                    // Perform file upload with progress tracking
                    _sftpClient.UploadFile(transferStream, remoteFile, uploadTask.SetProgress);
                }
                catch (Exception ex)
                {
                    Debug.LogError("SFTP: Upload error to " + remoteFile + " - " + ex.Message);
                    // If upload fails, determine if it was canceled or due to another error
                    completionCode = uploadTask.IsCancelled ? FTPCompletionCode.Cancelled : FTPCompletionCode.RemoteFileError;
                }
                finally
                {
                    // Ensure the transfer stream is cleaned up after the operation
                    transferStream?.Close();
                    transferStream?.Dispose();
                }

                // If the upload was canceled, attempt to delete the remote file
                if (completionCode == FTPCompletionCode.Cancelled)
                {
                    try
                    {
                        _sftpClient.Disconnect();
                        _sftpClient.Connect();
                        _sftpClient.DeleteFile(remoteFile);
                    }
                    catch
                    {
                        Debug.LogError("Failed to delete remote partial upload");
                    }
                }

                // Finally, ensure the SFTP client disconnects properly
                try
                {
                    _sftpClient.Disconnect();
                }
                catch
                {
                    Debug.LogError("SFTP: Could not disconnect after upload");
                }

                // Return the completion code along with the size of the uploaded file
                return completionCode;
            });
        }

        //############################################################################################################################################
        #endregion

        private bool FileNameValid(string name)
        {
            if (name.Contains("/") || name.Contains("\\"))
                return false; //none of your ../.. jailbreaking!

            //more checks stolen from https://stackoverflow.com/questions/4650462/easiest-way-to-check-if-an-arbitrary-string-is-a-valid-filename
            if (string.IsNullOrWhiteSpace(name))
                return false;

            if (name.Length > 1 && name[1] == ':' && (name.Length < 4 || name.ToLower()[0] < 'a' || name.ToLower()[0] > 'z'))
                return false;

            return true;
        }

        private void SetFileTransferTaskProgress(ulong dataTransferred)
        {
            if (!_currentFileTransferTask.IsInProgress)
            {
                Debug.LogError("Tried to cancel a task that is not in progress");
                return;
            }

            if (transferStream == null)
            {
                Debug.LogError("Tried to cancel a task but TransferStream is null");
                return;
            }

            if (_currentFileTransferTask.IsCancelled)
                transferStream.Close();
            else 
                _currentFileTransferTask.SetProgress(dataTransferred);
        }
    }
}
