#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using UnityEngine.SceneManagement;
using System.Text.RegularExpressions;
using System.Linq;
using static VE2.NonCore.Platform.Internal.VE2PluginBuilderWindow;
using VE2.NonCore.FileSystem.Internal;
using VE2.NonCore.FileSystem.API;
using static VE2.NonCore.Platform.API.PlatformPublicSerializables;

namespace VE2.NonCore.Platform.Internal
{
    public class PluginUploader
    {
        [MenuItem("VE2/Upload built world...", priority = 2)]
        internal static void ShowWindow()
        {
            var window = ScriptableObject.CreateInstance<PluginUploaderWindow>();
            window.position = new Rect(Screen.width / 2, Screen.height / 2, 400, 250);
            window.titleContent = new GUIContent("Upload built VE2 world");
            V_PlatformIntegration platformIntegration = GameObject.FindObjectOfType<V_PlatformIntegration>();
            window.noName = platformIntegration == null;
            if (!window.noName)
            {
                window.worldName = SceneManager.GetActiveScene().name;
            }

            window.Show();
        }
    }

    class PluginUploaderWindow : EditorWindow
    {
        // private const string username = "ViRSE", password = "fwf3f3j21r3ed", host = "13.87.84.200";
        // private const int port = 22;
        public string worldName = "";
        public bool noName = false;

        private bool uploading = false;
        private bool doneUpload = false;
        private bool errorUploading = false;

        private List<IRemoteFileTaskInfo> uploadTasks = new();

        private List<string> exportFiles;

        private EnvironmentType _environmentType = EnvironmentType.Undefined;
        private EnvironmentType _lastEnvironmentType = EnvironmentType.Undefined;

        private Scene _sceneToExport;
        private string _worldFolderName => $"{_sceneToExport.name}";

        private IFileSystemInternal _fileSystem;

        private int _highestRemoteVersionFound = -1;
        private int _highestLocalVersionFound = -1;
        private bool _searchingForVersion = false;

        private void SearchForVersion()
        {
            _searchingForVersion = true;

            _sceneToExport = SceneManager.GetActiveScene();
            Debug.Log("OnEnable - " + _sceneToExport.name);

            ServerConnectionSettings ftpNetworkSettings = new("ViRSE", "fwf3f3j21r3ed", "13.87.84.200", 22); //TODO: Load in from SO

            //TODO: maybe just the factory can move to the internal interface asmdef?
            _fileSystem = FileSystemServiceFactory.CreateFileStorageService(ftpNetworkSettings, $"VE2/Worlds/{_environmentType}");

            List<string> localWorldVersions = _fileSystem.GetLocalFoldersAtPath(_worldFolderName);
            Debug.Log("Searched for local folders, found " + localWorldVersions.Count);
            foreach (string folder in localWorldVersions)
            {
                Debug.Log(folder);
            }

            _highestLocalVersionFound = GetHighestVersionNumberFromFoldersList(localWorldVersions);
            exportFiles = GetFilesToUpload();

            IRemoteFolderSearchInfo worldsSearch = _fileSystem.GetRemoteFoldersAtPath("");
            worldsSearch.OnSearchComplete += HandleWorldsSearchComplete;
        }

        private void HandleWorldsSearchComplete(IRemoteFolderSearchInfo worldSearch)
        {
            if (worldSearch.CompletionCode.ToUpper().Contains("ERROR"))
            {
                Debug.LogError("Error connecting to remote file system");
            }

            Debug.Log("Look for world folder " + _worldFolderName);

            Debug.Log("Found world folders");
            foreach (string folder in worldSearch.FoldersFound)
            {
                Debug.Log(folder);
            }

            if (!worldSearch.FoldersFound.Contains(_worldFolderName))
            {
                Debug.Log("No remote folder found for this world");
                _highestRemoteVersionFound = 0;
                _searchingForVersion = false;
            }
            else
            {
                IRemoteFolderSearchInfo versionSearch = _fileSystem.GetRemoteFoldersAtPath(_worldFolderName);
                versionSearch.OnSearchComplete += HandleVersionSearchComplete;
            }
        }

        private void HandleVersionSearchComplete(IRemoteFolderSearchInfo search)
        {
            search.OnSearchComplete -= HandleVersionSearchComplete;

            Debug.Log("Found version subfolders... " + search.CompletionCode);

            _highestRemoteVersionFound = GetHighestVersionNumberFromFoldersList(search.FoldersFound);
            _searchingForVersion = false;
        }

        private int GetHighestVersionNumberFromFoldersList(List<string> folderList)
        {
            // A regular expression to match strings that consist of 3 digits only
            Regex numericRegex = new Regex(@"^\d{3}$"); 

            int highestFolderFound = 0;

            foreach (string folderPathAndName in folderList)
            {
                string folderName = folderPathAndName.Contains("/") ? folderPathAndName.Substring(folderPathAndName.LastIndexOf("/") + 1) : folderPathAndName;

                Debug.Log(folderName);

                // Check if the folder name matches the format
                if (numericRegex.IsMatch(folderName))
                {
                    // Parse the numeric part of the folder name
                    if (int.TryParse(folderName, out int folderNumber))
                    {
                        // Update the highest number if this one is greater
                        if (folderNumber > highestFolderFound)
                            highestFolderFound = folderNumber;
                    }
                }
            }

            return highestFolderFound;
        }

        private void OnInspectorUpdate()
        {
            if (_fileSystem != null)
            {
                _fileSystem.Update(); //Hack, mimic the update call from the file system's monobehaviour, this gets TaskInfos to update
            }

            if (uploading)
            {
                Repaint();
            }
        }
        private void OnGUI()
        {
            GUIStyle yellow = new GUIStyle(EditorStyles.textField);
            yellow.normal.textColor = Color.yellow;

            GUIStyle green = new GUIStyle(EditorStyles.textField);
            green.normal.textColor = Color.green;

            GUIStyle red = new GUIStyle(EditorStyles.textField);
            red.normal.textColor = Color.red;

            GUIStyle greenbold = new GUIStyle(EditorStyles.textField);
            greenbold.normal.textColor = Color.green;
            greenbold.fontSize = 14;

            EditorGUILayout.Separator();

            if (noName)
            {
                EditorGUILayout.HelpBox("Cannot find a V_PlatformIntegration script!", (UnityEditor.MessageType)MessageType.Error);
                return;
            }

            if (worldName == "")
            {
                EditorGUILayout.HelpBox("World name cannot be blank!", (UnityEditor.MessageType)MessageType.Error);
                return;
            }

            //BUILD TYHPE## ##################################################################
            //################################################################################

            _environmentType = (EnvironmentType)EditorGUILayout.EnumPopup("Build type", _environmentType);

            if (_environmentType == EnvironmentType.Undefined)
            {
                EditorGUILayout.HelpBox("Please enter a build type", (UnityEditor.MessageType)MessageType.Info);
                EditorGUI.EndDisabledGroup();
                return;
            }

            if (_environmentType != _lastEnvironmentType)
                _highestRemoteVersionFound = -1;

            _lastEnvironmentType = _environmentType;

            EditorGUILayout.Separator();

            //WORLD VERSION ##################################################################
            //################################################################################

            if (!_searchingForVersion && _highestRemoteVersionFound == -1)
            {
                SearchForVersion();
                EditorGUI.EndDisabledGroup();
                return;
            }
            else if (_searchingForVersion)
            {
                EditorGUILayout.LabelField("Searching for remote versions...");
                EditorGUI.EndDisabledGroup();
                return;
            }

            if (_highestLocalVersionFound == 0)
            {
                EditorGUILayout.HelpBox("No local versions found, please build your plugin", (UnityEditor.MessageType)MessageType.Warning);
                return;
            }
            else if (_highestLocalVersionFound <= _highestRemoteVersionFound)
            {
                EditorGUILayout.HelpBox("Remote version number is ahead of local version, please rebuild your plugin", (UnityEditor.MessageType)MessageType.Warning);
                return;
            }
            else if ((_environmentType == EnvironmentType.Windows && exportFiles.Count < 3) || (_environmentType == EnvironmentType.Android && exportFiles.Count < 2))
            {
                EditorGUILayout.HelpBox($"Local build {_worldFolderName}V{_highestLocalVersionFound} is missing files. Please rebuild", (UnityEditor.MessageType)MessageType.Error);
                return;
            }
            else 
            {
                EditorGUILayout.HelpBox($"{_worldFolderName} V{_highestLocalVersionFound} Validated - {exportFiles.Count} files found", (UnityEditor.MessageType)MessageType.Info);
            }

            //UPLOAD STATUS ##################################################################
            //################################################################################

            EditorGUILayout.Separator();

            if (errorUploading)
            {
                EditorGUILayout.HelpBox("Upload failed!", (UnityEditor.MessageType)MessageType.Error);
                return;
            }

            if (uploading || doneUpload)
            {
                EditorGUILayout.LabelField($"Uploading {_worldFolderName} V{_highestLocalVersionFound} ... please wait... do not close this window!");

                foreach (IRemoteFileTaskInfo taskInfo in uploadTasks)
                {
                    //Debug.Log($"Check {taskInfo.NameAndPath} - {taskInfo.Status}");
                    if (taskInfo.Status == RemoteFileTaskStatus.Queued)
                        EditorGUILayout.LabelField($"{taskInfo.NameAndPath} - awaiting", red);
                    else if (taskInfo.Status == RemoteFileTaskStatus.Succeeded)
                        EditorGUILayout.LabelField($"{taskInfo.NameAndPath} - complete", green);
                    else
                        EditorGUILayout.LabelField($"{taskInfo.NameAndPath} - uploading - {(int)(taskInfo.Progress * 100)}%", yellow);
                }

                if (doneUpload)
                {
                    EditorGUILayout.Separator();
                    EditorGUILayout.HelpBox("Upload complete!", (UnityEditor.MessageType)MessageType.Info);
                }

                return;
            }
            

            //BEGIN UPLOAD# ##################################################################
            //################################################################################

            foreach (string file in exportFiles)
            {
                EditorGUILayout.LabelField($"{Path.GetFileName(file)}", yellow);
            }

            if (GUILayout.Button("Begin Upload"))
            {
                BeginUpload();
            }
        }

        private void OnDestroy()
        {
            //_fileSystem.TearDown?
        }

        private void BeginUpload()
        {
            uploading = true;
            errorUploading = false;

            uploadTasks.Clear();

            foreach (string file in exportFiles)
            {
                IRemoteFileTaskInfo uploadTask = _fileSystem.UploadFile(file);
                uploadTasks.Add(uploadTask);
                uploadTask.OnTaskCompleted += OnUploadComplete;
            }
        }

        public void OnUploadComplete(IRemoteFileTaskInfo taskInfo)
        {
            taskInfo.OnTaskCompleted -= OnUploadComplete;

            if (taskInfo.Status != RemoteFileTaskStatus.Succeeded)
            {
                errorUploading = true;

                foreach (IRemoteFileTaskInfo task in uploadTasks)
                {
                    if (task.Status == RemoteFileTaskStatus.InProgress || task.Status == RemoteFileTaskStatus.Queued)
                        task.CancelRemoteFileTask();
                }

                Repaint();
            }
            else 
            {
                bool uploadsStillTodo = false;

                foreach (IRemoteFileTaskInfo task in uploadTasks)
                {
                    if (task.Status == RemoteFileTaskStatus.InProgress || task.Status == RemoteFileTaskStatus.Queued)
                        uploadsStillTodo = true;
                }

                if (!uploadsStillTodo)
                {
                    doneUpload = true;
                    uploading = false;
                    Repaint();
                }
            }
        }

        private List<string> GetFilesToUpload()
        {
            List<string> files = new List<string>();
            try
            {
                List<string> fileEntries = _fileSystem.GetLocalFilesAtPath($"{_worldFolderName}/{_highestLocalVersionFound.ToString("D3")}").Keys.ToList(); // Directory.GetFiles(Application.persistentDataPath + "/build/" + worldName + "/export");

                foreach (string fileName in fileEntries)
                    files.Add(fileName);
                return files;
            }
            catch
            {
                return files;
            }
        }
    }
}

#endif