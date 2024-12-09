// using UnityEngine;

// //TODO: Plugin-facing interface, internal facing interface
// public class V_FTPIntegration : MonoBehaviour
// {
//     // Start is called once before the first execution of Update after the MonoBehaviour is created
//     void Start()
//     {
        
//     }

//     // Update is called once per frame
//     void Update()
//     {
        
//     }
// }

// public class FTPService 
// {

// }

// public class V_SFTP_Client : V_ILFM_Client
// {
//     private V_ILFM_Manager manager;
//     private V_ILFM_Client.Status status;

//     //Implement interface

//     public V_SFTP_Client(string host, int port, string username, string password)
//     {
//         SetConnectionDetails(host, port, username, password);
//         Initialise();
//     }

//     bool cancel = false;
//     public void CancelAction()
//     {
//         if (status == V_ILFM_Client.Status.Busy)
//             cancel = true;
//     }

//     public async void GetFileList(string remotePath)
//     {
//         if (status != V_ILFM_Client.Status.Ready)
//         {
//             //error handling - I'm busy or not available
//             manager.FileListComplete(remotePath, null, V_ILFM_Client.CompletionCodes.Busy);
//             return;
//         }
//         else
//         {
//             if (remotePath.Contains("..")) //nope!
//             {
//                 manager.FileListComplete(remotePath, null, V_ILFM_Client.CompletionCodes.RemoteFileError);
//                 return;
//             }
//             SetStatus(V_ILFM_Client.Status.Busy);
//             completionCode = V_ILFM_Client.CompletionCodes.Success; //may get set otherwise
//             await DirectoryThreaded(remotePath);
//             cancel = false; //any cancelling should be removed and ignored - not cancellable

//             if (completionCode == V_ILFM_Client.CompletionCodes.Success)
//             {
//                 List<V_ILFM_Manager.V_LargeFileDetails> returnFiles = new List<V_ILFM_Manager.V_LargeFileDetails>();
//                 foreach (SftpFile file in files)
//                 {
//                     if (!file.IsDirectory)
//                         returnFiles.Add(new V_ILFM_Manager.V_LargeFileDetails() { fileName = file.Name, fileSize = (ulong)file.Length });
//                 }
//                 manager?.FileListComplete(remotePath, returnFiles, completionCode);
//             }
//             else
//                 manager?.FileListComplete(remotePath, null, completionCode);
//             SetStatus(V_ILFM_Client.Status.Ready);
//         }
//     }

//     public V_ILFM_Client.Status GetStatus()
//     {
//         return status;
//     }

//     public async void MakeFolder(string remotePath, string newFolder)
//     {
//         if (status != V_ILFM_Client.Status.Ready)
//         {
//             //error handling - I'm busy or not available
//             manager.MakeFolderComplete(remotePath, newFolder, V_ILFM_Client.CompletionCodes.Busy);
//             return;
//         }
//         else
//         {
//             if (remotePath.Contains("..") || newFolder.Contains("..")) //nope!
//             {
//                 manager.MakeFolderComplete(remotePath, newFolder, V_ILFM_Client.CompletionCodes.RemoteFileError);
//                 return;
//             }
//             if (FileNameValid(newFolder))
//             {
//                 SetStatus(V_ILFM_Client.Status.Busy);
//                 completionCode = V_ILFM_Client.CompletionCodes.Success; //may get set otherwise
//                 await MakeFolderThreaded(remotePath + "/" + newFolder);

//                 manager.MakeFolderComplete(remotePath, newFolder, completionCode);
//                 cancel = false; //any cancelling should be removed and ignored - not cancellable
//                 SetStatus(V_ILFM_Client.Status.Ready);
//             }
//             else
//             {
//                 manager.MakeFolderComplete(remotePath, newFolder, V_ILFM_Client.CompletionCodes.RemoteFileError);
//                 SetStatus(V_ILFM_Client.Status.Ready);
//             }
//         }

//     }

//     public async void RemoveFileOrEmptyFolder(string remotePath, string fileName)
//     {
//         if (status != V_ILFM_Client.Status.Ready)
//         {
//             //error handling - I'm busy or not available
//             manager.FileOrFolderRemovalComplete(remotePath, fileName, V_ILFM_Client.CompletionCodes.Busy);
//             return;
//         }
//         else
//         {
//             if (remotePath.Contains("..")) //nope!
//             {
//                 manager.FileOrFolderRemovalComplete(remotePath, fileName, V_ILFM_Client.CompletionCodes.RemoteFileError);
//                 return;
//             }
//             if (FileNameValid(fileName))
//             {

//                 SetStatus(V_ILFM_Client.Status.Busy);
//                 completionCode = V_ILFM_Client.CompletionCodes.Success; //may get set otherwise
//                 await DeleteThreaded(remotePath + "/" + fileName);
//                 cancel = false; //any cancelling should be removed and ignored - not cancellable

//                 manager.FileOrFolderRemovalComplete(remotePath, fileName, completionCode);
//                 SetStatus(V_ILFM_Client.Status.Ready);
//             }
//             else
//             {
//                 manager.FileOrFolderRemovalComplete(remotePath, fileName, V_ILFM_Client.CompletionCodes.RemoteFileError);
//                 SetStatus(V_ILFM_Client.Status.Ready);
//             }
//         }
//     }

//     public async void RetrieveFile(string remotePath, string localPath, string fileName)
//     {

//         if (status != V_ILFM_Client.Status.Ready)
//         {
//             //error handling - I'm busy or not available
//             manager.FileDownloadComplete(remotePath, fileName, V_ILFM_Client.CompletionCodes.Busy);
//             return;
//         }
//         else
//         {
//             cancel = false; //reset at start of task or we CAN get them hanging
//             if (remotePath.Contains("..")) //nope!
//             {
//                 manager.FileDownloadComplete(remotePath, fileName, V_ILFM_Client.CompletionCodes.RemoteFileError);
//                 return;
//             }
//             if (FileNameValid(fileName))
//             {

//                 SetStatus(V_ILFM_Client.Status.Busy);
//                 completionCode = V_ILFM_Client.CompletionCodes.Success; //may get set otherwise
//                 await DownloadThreaded(remotePath + "/" + fileName, localPath + "\\" + fileName);

//                 manager.FileDownloadComplete(remotePath, fileName, completionCode);
//                 SetStatus(V_ILFM_Client.Status.Ready);
//             }
//             else
//             {
//                 manager.FileDownloadComplete(remotePath, fileName, V_ILFM_Client.CompletionCodes.RemoteFileError);
//                 SetStatus(V_ILFM_Client.Status.Ready);
//             }
//         }
//     }

//     private bool FileNameValid(string name)
//     {
//         if (name.Contains("/") || name.Contains("\\")) return false; //none of your ../.. jailbreaking!

//         //more checks stolen from https://stackoverflow.com/questions/4650462/easiest-way-to-check-if-an-arbitrary-string-is-a-valid-filename
//         if (string.IsNullOrWhiteSpace(name)) return false;
//         if (name.Length > 1 && name[1] == ':')
//         {
//             if (name.Length < 4 || name.ToLower()[0] < 'a' || name.ToLower()[0] > 'z') return false;
//             name = name.Substring(3);
//         }

//         return true;

//     }

//     public void SetManager(V_ILFM_Manager manager)
//     {
//         this.manager = manager;
//     }

//     public async void StoreFile(string remotePath, string localPath, string fileName)
//     {
//         if (status != V_ILFM_Client.Status.Ready)
//         {
//             //error handling - I'm busy or not available
//             manager.FileUploadComplete(remotePath, fileName, V_ILFM_Client.CompletionCodes.Busy);
//             return;
//         }
//         else
//         {
//             cancel = false; //reset at start of task or we CAN get them hanging
//             if (remotePath.Contains("..")) //nope!
//             {
//                 manager.FileUploadComplete(remotePath, fileName, V_ILFM_Client.CompletionCodes.RemoteFileError);
//                 return;
//             }
//             if (FileNameValid(fileName))
//             {
//                 SetStatus(V_ILFM_Client.Status.Busy);
//                 completionCode = V_ILFM_Client.CompletionCodes.Success; //may get set otherwise
//                 await UploadThreaded(remotePath + "/" + fileName, localPath + "\\" + fileName);

//                 manager.FileUploadComplete(remotePath, fileName, completionCode);
//                 SetStatus(V_ILFM_Client.Status.Ready);
//             }
//             else
//             {
//                 manager.FileUploadComplete(remotePath, fileName, V_ILFM_Client.CompletionCodes.LocalFileError);
//                 SetStatus(V_ILFM_Client.Status.Ready);
//             }
//         }
//     }

//     ///private stuff

//     private SftpClient sftpClient;
//     private string host, username, password;
//     private int port;

//     public void SetConnectionDetails(string host, int port, string username, string password)
//     {
//         this.host = host;
//         this.port = port;
//         this.username = username;
//         this.password = password;
//     }
//     private void Initialise()
//     {
//         sftpClient = new SftpClient(host, port, username, password);
//         SetStatus(V_ILFM_Client.Status.Ready);
//     }

//     private void SetStatus(V_ILFM_Client.Status newStatus)
//     {
//         status = newStatus;
//         manager?.StatusChanged(newStatus);
//     }

//     private string remotePath;
//     private long remoteFileSize;
//     private FileStream transferStream;
//     private V_ILFM_Client.CompletionCodes completionCode = V_ILFM_Client.CompletionCodes.Success;


//     private Task MakeFolderThreaded(string remoteFolder)
//     {
//         //Have to run this in different thread, no proper async support in SSH library
//         return Task.Run(() =>
//         {
//             remotePath = remoteFolder; //for return value

//             try
//             {
//                 sftpClient.Connect();
//             }
//             catch
//             {
//                 completionCode = V_ILFM_Client.CompletionCodes.CouldNotConnect;
//                 V_Logger.Error("SFTP: Could not connect (is network down?)");
//                 return;
//             }

//             //OK connected, try it
//             try
//             {
//                 sftpClient.CreateDirectory(remoteFolder);
//             }
//             catch
//             {
//                 //Nope. 
//                 completionCode = V_ILFM_Client.CompletionCodes.RemoteFileError;
//             }

//             try
//             {
//                 //surely these can't go wrong?
//                 sftpClient.Disconnect();
//             }
//             catch
//             {
//                 V_Logger.Error("SFTP: Could not disconnect after make folder - this is not a good sign.");
//             }
//         });
//     }

//     private Task DeleteThreaded(string remoteFile)
//     {
//         //Have to run this in different thread, no proper async support in SSH library
//         return Task.Run(() =>
//         {
//             remotePath = remoteFile; //for return value

//             try
//             {
//                 sftpClient.Connect();
//             }
//             catch
//             {
//                 completionCode = V_ILFM_Client.CompletionCodes.CouldNotConnect;
//                 V_Logger.Error("SFTP: Could not connect (is network down?)");
//                 return;
//             }

//             SftpFileAttributes attribs;
//             //Get file size (for progress calculation)
//             try
//             {
//                 attribs = sftpClient.GetAttributes(remoteFile);
//             }
//             catch
//             {
//                 //Could not get attributes, probably does not exist
//                 completionCode = V_ILFM_Client.CompletionCodes.RemoteFileError;
//                 sftpClient.Disconnect(); //break connection
//                 return;
//             }

//             if (!attribs.IsRegularFile && !attribs.IsDirectory)
//             {
//                 completionCode = V_ILFM_Client.CompletionCodes.RemoteFileError;
//                 sftpClient.Disconnect(); //break connection
//                 return;
//             }

//             //OK, it looks like a file or directory. Proceed!

//             //OK connected, try to get file
//             try
//             {
//                 //SetProgress is internal progress callback
//                 if (attribs.IsRegularFile)
//                     sftpClient.DeleteFile(remoteFile);
//                 else
//                     sftpClient.DeleteDirectory(remoteFile);
//             }
//             catch
//             {
//                 //Nope. 
//                 completionCode = V_ILFM_Client.CompletionCodes.RemoteFileError;
//             }

//             try
//             {
//                 //surely these can't go wrong?
//                 sftpClient.Disconnect();
//             }
//             catch
//             {
//                 V_Logger.Error("SFTP: Could not disconnect after download - this is not a good sign.");
//             }
//         });
//     }


//     private IEnumerable<SftpFile> files;

//     private Task DirectoryThreaded(string remotePath)
//     {
//         return Task.Run(() =>
//         {
//             //V_Logger.Message($"SFTP: In DirThreaded {remotePath}");
//             files = new List<SftpFile>();
//             this.remotePath = remotePath; //for return value

//             try
//             {
//                 sftpClient.Connect();
//             }
//             catch
//             {
//                 completionCode = V_ILFM_Client.CompletionCodes.CouldNotConnect;
//                 V_Logger.Error("SFTP: Could not connect (is network down?)");
//                 return;
//             }

//             try
//             {
//                 files = sftpClient.ListDirectory(remotePath);
//             }
//             catch
//             {
//                 //Could not list contents
//                 completionCode = V_ILFM_Client.CompletionCodes.RemoteFileError;
//                 //V_Logger.Error($"SFTP: Could not list contents of {remotePath}");
//                 sftpClient.Disconnect(); //break connection
//                 return;
//             }

//             try
//             {
//                 //surely these can't go wrong? catch it anyway
//                 sftpClient.Disconnect();
//             }
//             catch
//             {
//                 V_Logger.Error("SFTP: Could not disconnect - this is probably a bad sign.");
//                 completionCode = V_ILFM_Client.CompletionCodes.RemoteFileError;
//             }
//         });

//     }
//     private Task DownloadThreaded(string remoteFile, string localFile)
//     {
//         //Have to run this in different thread, no proper async support in SSH library
//         return Task.Run(() =>
//         {
//             remotePath = remoteFile; //for return value

//             try
//             {
//                 sftpClient.Connect();
//             }
//             catch
//             {
//                 completionCode = V_ILFM_Client.CompletionCodes.CouldNotConnect;
//                 V_Logger.Error("SFTP: Could not connect (is network down?)");
//                 return;
//             }

//             SftpFileAttributes attribs;
//             //Get file size (for progress calculation)
//             try
//             {
//                 attribs = sftpClient.GetAttributes(remoteFile);
//             }
//             catch
//             {
//                 //Could not get attributes, probably does not exist
//                 completionCode = V_ILFM_Client.CompletionCodes.RemoteFileError;
//                 V_Logger.Error($"attrib error {remoteFile}");
//                 sftpClient.Disconnect(); //break connection
//                 return;
//             }

//             if (!attribs.IsRegularFile)
//             {
//                 completionCode = V_ILFM_Client.CompletionCodes.RemoteFileError;
//                 V_Logger.Error("notfileerror");
//                 sftpClient.Disconnect(); //break connection
//                 return;
//             }

//             //OK, it looks like a file. Proceed!
//             remoteFileSize = attribs.Size;
//             lastDownloadedBytes = 0;   //to enable callback to sparsify calls to manager

//             //Check I can open local file
//             try
//             {
//                 transferStream = File.OpenWrite(localFile);
//             }
//             catch
//             {
//                 completionCode = V_ILFM_Client.CompletionCodes.LocalFileError;
//                 sftpClient.Disconnect(); //break connection
//                 return;
//             }

//             //OK connected, try to get file
//             try
//             {
//                 //SetProgress is internal progress callback
//                 sftpClient.DownloadFile(remoteFile, transferStream, SetProgress);
//             }
//             catch
//             {
//                 //Nope. It was probably cancelled
//                 if (cancel)
//                     completionCode = V_ILFM_Client.CompletionCodes.Cancelled;
//                 else
//                 {
//                     completionCode = V_ILFM_Client.CompletionCodes.RemoteFileError;
//                     V_Logger.Error("Final error");
//                 }
//             }
//             transferStream.Close();
//             transferStream.Dispose();
//             transferStream = null;

//             try
//             {
//                 //surely these can't go wrong? catch it anyway
//                 sftpClient.Disconnect();

//                 //cancelled - so delete. No partial downloads here!
//                 if (completionCode == V_ILFM_Client.CompletionCodes.Cancelled)
//                 {
//                     UnityEngine.Debug.Log("Cancelled in flight-  deleting local file " + localFile);
//                     File.Delete(localFile);
//                 }
//             }
//             catch
//             {
//                 V_Logger.Error("SFTP: Could not disconnect (or locally delete?) cancelled download - this is probably a bad sign.");
//                 completionCode = V_ILFM_Client.CompletionCodes.RemoteFileError;
//             }
//         });
//     }

//     private Task UploadThreaded(string remoteFile, string localFile)
//     {
//         //Have to run this in different thread, no proper async support in SSH library
//         return Task.Run(() =>
//         {
//             remotePath = remoteFile; //for return value

//             try
//             {
//                 FileInfo f = new FileInfo(localFile);
//                 if (!f.Exists)
//                 {
//                     completionCode = V_ILFM_Client.CompletionCodes.LocalFileError;
//                     return;
//                 }
//                 remoteFileSize = f.Length;
//             }
//             catch
//             {
//                 //maybe not possible? Catch it anyway
//                 V_Logger.Error("SFTP: Could not open local file for upload");
//                 completionCode = V_ILFM_Client.CompletionCodes.LocalFileError;
//                 return;
//             }

//             //OK.. there IS a file


//             //Check I can open it
//             try
//             {
//                 transferStream = File.OpenRead(localFile);
//             }
//             catch
//             {
//                 V_Logger.Error("SFTP: Could not open local file for upload");
//                 completionCode = V_ILFM_Client.CompletionCodes.LocalFileError;
//                 return;
//             }


//             //Local file is fine
//             try
//             {
//                 sftpClient.Connect();
//             }
//             catch
//             {
//                 completionCode = V_ILFM_Client.CompletionCodes.CouldNotConnect;
//                 V_Logger.Error("SFTP: Could not connect. Oh dear. Is network down?");
//                 return;
//             }



//             lastDownloadedBytes = 0;   //to enable callback to sparsify calls to manager

//             //OK connected, try to store it
//             try
//             {
//                 //SetProgress is internal progress callback
//                 sftpClient.UploadFile(transferStream, remoteFile, SetProgress);
//             }
//             catch
//             {
//                 //Nope. It was probably cancelled? Or maybe folder just doesn't exis
//                 if (cancel)
//                     completionCode = V_ILFM_Client.CompletionCodes.Cancelled;
//                 else
//                     completionCode = V_ILFM_Client.CompletionCodes.RemoteFileError;
//             }
//             transferStream.Close();
//             transferStream.Dispose();
//             transferStream = null;

//             if (completionCode == V_ILFM_Client.CompletionCodes.Cancelled)
//             {
//                 //TRY to delete remote file
//                 try
//                 {
//                     sftpClient.Disconnect();
//                     sftpClient.Connect();
//                     sftpClient.DeleteFile(remoteFile);
//                 }
//                 catch
//                 {
//                     V_Logger.Error("Failed to delete remote partial upload");
//                     ; //probably nothing to do - could not delete, maybe done by system
//                 }

//             }

//             //surely THIS can't go wrong?
//             try
//             {
//                 sftpClient.Disconnect();
//             }
//             catch
//             {
//                 V_Logger.Error("SFTP: Could not disconnect after upload. No idea why not - panic time?");
//             }
//         });
//     }

//     private ulong lastDownloadedBytes = 0;

//     /// <summary>
//     /// This is called by SSH system frequently (who knows how often - but too often I think)
//     /// during uploads or downloads
//     /// 
//     /// Handles cancel mechanism, and sporadic callbacks through interface to framework
//     /// </summary>
//     /// <param name="uploaded"></param>
//     private void SetProgress(ulong uploaded)
//     {
//         if (cancel)
//         {
//             if (transferStream != null)
//                 transferStream.Close();  //this breaks the transfer - it will exception out

//             cancel = false; //cancel has been done
//         }
//         else
//         {
//             //call framework with update every 5k, or if this is first call
//             if (lastDownloadedBytes == 0 || (uploaded - lastDownloadedBytes) > 5000)
//             {
//                 lastDownloadedBytes = uploaded;
//                 manager?.ProgressChanged(((float)uploaded) / ((float)remoteFileSize));
//             }
//         }
//     }

//     public async void GetFolderList(string remotePath)
//     {
//         if (status != V_ILFM_Client.Status.Ready)
//         {
//             //error handling - I'm busy or not available
//             manager.FolderListComplete(remotePath, null, V_ILFM_Client.CompletionCodes.Busy);
//             return;
//         }
//         else
//         {
//             if (remotePath.Contains("..")) //nope!
//             {
//                 manager.FolderListComplete(remotePath, null, V_ILFM_Client.CompletionCodes.RemoteFileError);
//                 return;
//             }
//             SetStatus(V_ILFM_Client.Status.Busy);
//             completionCode = V_ILFM_Client.CompletionCodes.Success; //may get set otherwise
//             await DirectoryThreaded(remotePath);
//             cancel = false; //any cancelling should be removed and ignored - not cancellable

//             if (completionCode == V_ILFM_Client.CompletionCodes.Success)
//             {
//                 List<string> returnFolders = new List<string>();
//                 foreach (SftpFile file in files)
//                 {
//                     if (file.IsDirectory && file.Name.Substring(0, 1) != ".") //avoid .. and .
//                         returnFolders.Add(file.Name);
//                 }
//                 manager?.FolderListComplete(remotePath, returnFolders, completionCode);
//                 SetStatus(V_ILFM_Client.Status.Ready);
//             }
//             else
//             {
//                 manager?.FolderListComplete(remotePath, null, completionCode);
//                 SetStatus(V_ILFM_Client.Status.Ready);
//             }
//         }

//     }

// }
