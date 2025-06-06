#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Reflection;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine.SceneManagement;
using System.Text;
using System.Security.Cryptography;
using UnityEditor.Build.Reporting;
using VE2.NonCore.FileSystem.Internal;
using VE2.NonCore.FileSystem.API;
using static VE2.NonCore.Platform.API.PlatformPublicSerializables;

//TODO: Need to check for DLLs (rather than just assemblies) referenced the scene/scripts, and include them in the build. E.G Mathnet.Numerics.dll

namespace VE2.NonCore.Platform.Internal
{
    public static class Configuration
    {
        public static string[] ignoreAssembliesFilters = new[]
        {
            "System.*",
            "Unity.*",
            "UnityEngine.*",
            "Assembly-CSharp",
            "Cinemachine",
            "Toolbox",
            "Toolbox.*",
            "VE2.Core.*",
            "VE2.NonCore.*",
            "VE2.Common",
            "VE2.Common.*",
        };

    }

    public class VE2PluginBuilder
    {
        [MenuItem("VE2/Build VE2 plugin...", priority = 2)]
        internal static void ShowWindow()
        {
            var window = ScriptableObject.CreateInstance<VE2PluginBuilderWindow>();
            window.position = new Rect(Screen.width / 2, Screen.height / 2, 400, 250);
            window.titleContent = new GUIContent("Build VE2 plugin");
            window.Show();
        }
    }

    class VE2PluginBuilderWindow : EditorWindow
    {
        public enum EnvironmentType
        {
            Undefined,
            Windows,
            Android
        }

        bool isSceneDirty = true;
        string scenePath = "";
        //string destinationPath = "";

    #pragma warning disable 0414 // private field assigned but not used.
        bool pathContainsFiles = false;

        bool worldNameIllegal = false;
        bool assembliesValid = true;
        string assemblyErrors = "";
        Assembly[] locatedAssemblies = new Assembly[0];
        bool compressBundles = false;

        string studentPassword = "";
        string staffPassword = "";

        bool passwordsWereIllegal = false;

        private Scene _sceneToExport;
        private string _worldFolderName => $"{_sceneToExport.name}";
        private EnvironmentType _environmentType = EnvironmentType.Undefined;
        private EnvironmentType _lastEnvironmentType = EnvironmentType.Undefined;


        private IFileSystemInternal _fileSystem;

        private int _highestRemoteVersionFound = -1;
        private bool _searchingForVersion = false;

        private void SearchForVersion()
        {
            _searchingForVersion = true;

            _sceneToExport = SceneManager.GetActiveScene();
            Debug.Log("OnEnable - " + _sceneToExport.name);

            ServerConnectionSettings ftpNetworkSettings = new("ViRSE", "fwf3f3j21r3ed", "13.87.84.200", 22); //TODO: Load in from SO

            //TODO: maybe just the factory can move to the internal interface asmdef?
            _fileSystem = FileSystemServiceFactory.CreateFileStorageService(ftpNetworkSettings, $"VE2/Worlds/{_environmentType}");

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
                Debug.Log("No version found for this world, this version will be V1");
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

            // A regular expression to match strings that consist of 3 digits only
            Regex numericRegex = new Regex(@"^\d{3}$");

            foreach (string folder in search.FoldersFound)
            {
                Debug.Log(folder);

                // Check if the folder name matches the format
                if (numericRegex.IsMatch(folder))
                {
                    // Parse the numeric part of the folder name
                    if (int.TryParse(folder, out int folderNumber))
                    {
                        // Update the highest number if this one is greater
                        if (folderNumber > _highestRemoteVersionFound)
                            _highestRemoteVersionFound = folderNumber;
                    }
                }
            }

            //In case we didn't find any
            _highestRemoteVersionFound = Mathf.Max(0, _highestRemoteVersionFound);
            _searchingForVersion = false;
        }


        private void OnDisable()
        {
            Debug.Log("OnDisable");
        }

        private void OnGUI()
        {
            UnityEngine.SceneManagement.Scene sceneToExport = SceneManager.GetActiveScene();

            if (sceneToExport.name == "") //this happens after it starts - use it!
            {
                EditorGUILayout.LabelField("Processing... please wait. This window will close on completion");
                return;
            }

            bool lastIsSceneDirty = isSceneDirty;
            isSceneDirty = sceneToExport.isDirty;

            //Check the scene if the root GO hasn't yet been validated, or if the scene has just been saved
            if ((lastIsSceneDirty && !isSceneDirty))
                GetSceneDataAndScripts(sceneToExport);

            EditorGUILayout.Space(10);

            // WorldName

            V_PlatformIntegration platformIntegration = FindObjectOfType<V_PlatformIntegration>();
            if (platformIntegration == null)
            {
                EditorGUILayout.HelpBox("Cannot find a V_PlatformIntegration script!", (UnityEditor.MessageType)MessageType.Error);
                return;
            }

            string worldName = SceneManager.GetActiveScene().name;

            GUILayout.Label($"World Name: {worldName}");

            var filenameRegex = @"^[\w\-. ]+$";
            worldNameIllegal = !Regex.IsMatch(worldName, filenameRegex);

            if (worldNameIllegal || string.IsNullOrWhiteSpace(worldName))
            {
                EditorGUILayout.HelpBox("World name is illegal", (UnityEditor.MessageType)MessageType.Error);
                return;
            }

            if (isSceneDirty)
            {
                EditorGUILayout.HelpBox("Your scene has unsaved changes - please save your scene to continue", (UnityEditor.MessageType)MessageType.Error);
                return;
            }

            if (!assembliesValid)
            {
                EditorGUILayout.HelpBox($"Assembly scan :  {string.Join("\r\n,", locatedAssemblies.Select(ExtractFileName))}. Following errors were encountered: {assemblyErrors}", (UnityEditor.MessageType)MessageType.Error);
                return;
            }
            else
            {
                string assemblyDiagnostics = "";
                if (locatedAssemblies != null && locatedAssemblies.Length > 0)
                    assemblyDiagnostics = $"{locatedAssemblies.Length} code assemblies will be included in the build : {string.Join("\r\n,", locatedAssemblies.Select(ExtractFileName))}";
                else
                    assemblyDiagnostics = $"No code assemblies will be included in the build.";

                if (assemblyDiagnostics.Contains("YourPluginNameHere"))
                {
                    EditorGUILayout.HelpBox("You need to rename your assembly name to something unique for your world (e.g. the world name). Do this in the inspector for the Assembly Definition asset, which is in the YOUR_PLUGIN_HERE folder", (UnityEditor.MessageType)MessageType.Error);
                    return;
                }

                EditorGUILayout.HelpBox(assemblyDiagnostics, (UnityEditor.MessageType)MessageType.Info);
            }

            EditorGUI.BeginDisabledGroup(!assembliesValid);

            EditorGUILayout.Separator();

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
            
            if (_highestRemoteVersionFound == 0)
                EditorGUILayout.HelpBox("No remote versions found, this will be V1!", (UnityEditor.MessageType)MessageType.Info);
            else
                EditorGUILayout.HelpBox($"Remote versions found, this will be V{_highestRemoteVersionFound + 1}!", (UnityEditor.MessageType)MessageType.Info);

            EditorGUILayout.Separator();

            //PASSWORDS ######################################################################
            //################################################################################

            EditorGUILayout.LabelField("Leave passwords blank if not required");

            studentPassword = EditorGUILayout.TextField(
                "Student password:",
                studentPassword);

            staffPassword = EditorGUILayout.TextField(
                "Staff password:",
                staffPassword);

            if (studentPassword.Length > 0)
                EditorGUILayout.HelpBox("Student passwords are not currently supported in the VE2 framework. Entering one will do no harm, but (at present) it will have no effect.", (UnityEditor.MessageType)MessageType.Info);

            if (!ArePasswordsLegal())
            {
                EditorGUILayout.HelpBox("Passwords can only contain alphanumeric characters, a-z and 0-9, and must be 15 characters or less. No spaces and no symbols!", (UnityEditor.MessageType)MessageType.Error);
                EditorGUI.EndDisabledGroup();
                return;
            }

            EditorGUILayout.Separator();

            //BUILD ##########################################################################
            //################################################################################

            string destinationPath = Path.Combine(Application.persistentDataPath, "files", "VE2", "Worlds", _environmentType.ToString(), _worldFolderName, (_highestRemoteVersionFound + 1).ToString("D3"));

            if (GUILayout.Button("Build"))
            {
                ExecuteBuild(locatedAssemblies, destinationPath, worldName, false);
                this.Close();
            }
            else if (GUILayout.Button("Build with ECS/Burst"))
            {
                ExecuteBuild(locatedAssemblies, destinationPath, worldName, true);
                this.Close();
            }
            
            EditorGUI.EndDisabledGroup();
        }

        private bool ArePasswordsLegal()
        {
            Regex r = new Regex("^[a-z0-9]*$");

            if (staffPassword != "" && !r.IsMatch(staffPassword.ToLower())) return false;
            if (studentPassword != "" && !r.IsMatch(studentPassword.ToLower())) return false;

            if (staffPassword.Length > 15 || studentPassword.Length > 15) return false;

            return true;
        }


        // bool IsBuildOkay()
        // {
        //     return !string.IsNullOrEmpty(destinationPath) && assembliesValid;
        // }


        private void ExecuteBuild(IEnumerable<Assembly> assembliesToInclude, string destinationFolder, string bundleName, bool ecsOrBurst)
        {
            if (_environmentType == EnvironmentType.Windows)
            {
                BuildWindowsBundle(bundleName, destinationFolder); //editor script problems here
                DoWindowsScriptOnlyBuild(destinationFolder, assembliesToInclude.Select(ExtractFileName), bundleName, ecsOrBurst); //AND here!
            }
            else if (_environmentType == EnvironmentType.Android)
            {
                DoAPKBuild(bundleName, destinationFolder);
            }

            MakeMetadata(destinationFolder);
            // if (BuildExists) DoScriptOnlyBuild();
            // else DoFullBuild();
        }

        private string GetSHA256Hash(string s)
        {

            if (s == "") return "";
            else
            {
                //from https://stackoverflow.com/questions/16999361/obtain-sha-256-string-of-a-string
                StringBuilder Sb = new StringBuilder();

                using (SHA256 hashEngine = SHA256.Create())
                {
                    byte[] result = hashEngine.ComputeHash(Encoding.UTF8.GetBytes(s.ToLower()));

                    foreach (byte b in result)
                        Sb.Append(b.ToString("x2"));
                }

                return Sb.ToString();
            }
        }

        private void MakeMetadata(string destinationFolder)
        {
            string metadata = "studentPassword=" + GetSHA256Hash(studentPassword) + "\n";
            metadata += "staffPassword=" + GetSHA256Hash(staffPassword) + "\n";

            File.WriteAllText(destinationFolder + "/metadata.txt", metadata);
        }

        // if you select C:\bundle\ as bundle location, then it will create two folders:
        // c:\bundle\__build\, for project build
        // c:\bundle\export\ , for items.

        private void DoWindowsScriptOnlyBuild(string destination, IEnumerable<string> managedAssemblyNames, string bundleName, bool ecsOrBurst)
        {
            if (_environmentType == EnvironmentType.Undefined)
            {
                Debug.LogError("Environment type is undefined");
                return;
            }

            var bpo = new BuildPlayerOptions()
            {
                locationPathName = Path.Combine(destination, "__build", "plugin"),
                target = _environmentType == EnvironmentType.Windows ? BuildTarget.StandaloneWindows64 : BuildTarget.Android,
                options = ecsOrBurst ? BuildOptions.None : BuildOptions.BuildScriptsOnly,
            };

            BuildPipeline.BuildPlayer(bpo);

            var managedAssembliesDirectory = Path.Combine(destination, "__build", "plugin_Data", "Managed");
            foreach (var name in managedAssemblyNames)
            {
                var finalName = Path.Combine(managedAssembliesDirectory, name);
                FileUtil.CopyFileOrDirectory(finalName, Path.Combine(destination, name));
                Debug.Log("Added managed DLL " + finalName + " to export");
            }

            if (ecsOrBurst)
            {
                var burstAssemblyName = Path.Combine(destination, "__build", "plugin_Data", "Plugins", "x86_64", "lib_burst_generated.dll");
                if (File.Exists(burstAssemblyName))
                {
                    FileUtil.CopyFileOrDirectory(burstAssemblyName, Path.Combine(destination, $"lib_burst_generated_{bundleName}.dll"));
                    Debug.Log("Added burst DLL " + burstAssemblyName + " to export");
                }
            }

            //CleanupScriptBuild(destination);
        }

        private string ExtractFileName(Assembly assembly)
        {
            return new FileInfo(assembly.Location).Name;
        }

        private void BuildWindowsBundle(string name, string destinationFolder)
        {
            if (_environmentType == EnvironmentType.Undefined)
            {
                Debug.LogError("Environment type is undefined");
                return;
            }

            name = name.ToLowerInvariant();

            if (Directory.Exists(destinationFolder))
                FileUtil.DeleteFileOrDirectory(destinationFolder);


            var buildMap = new AssetBundleBuild[] {
                new AssetBundleBuild() {
                    assetBundleName = $"{name}.bundle",
                    assetNames = new[] {
                        scenePath,
                    },
                },
            };

            if (!Directory.Exists(destinationFolder))
                Directory.CreateDirectory(destinationFolder);

            var bundleBuildOptions = BuildAssetBundleOptions.None;

            if (!compressBundles) 
                bundleBuildOptions |= BuildAssetBundleOptions.UncompressedAssetBundle;

            var manfiest = BuildPipeline.BuildAssetBundles(destinationFolder, buildMap, bundleBuildOptions, BuildTarget.StandaloneWindows64);
            Debug.Log(destinationFolder);

            CleanupBundleBuild(destinationFolder, new[] { $"{name}.bundle" });
        }

        private void CleanupBundleBuild(string destinationFolder, IEnumerable<string> allowedNames)
        {
            var set = new HashSet<string>(allowedNames);
            // remove unnecessary bundle files:
            var di = new DirectoryInfo(destinationFolder);
            foreach (var file in di.EnumerateFiles())
            {
                if (!set.Contains(file.Name.ToLowerInvariant()))
                {
                    Debug.Log($"Delete unnecessary file {file.FullName}");
                    FileUtil.DeleteFileOrDirectory(file.FullName);
                }
            }
        }

        private void CleanupScriptBuild(string destinationFolder)
        {
            string buildFolderPath = Path.Combine(destinationFolder, "__build");
            Debug.Log($"PRE Delete build folder {buildFolderPath}");

            bool deleteSuccess = FileUtil.DeleteFileOrDirectory(buildFolderPath);
            Debug.Log($"Delete build folder {buildFolderPath} success: {deleteSuccess}");
        }

        private void DoAPKBuild(string name, string destinationFolder) 
        {
            //TODO: need to change builder to only build this scene - should then switch scene settings back(?)

            string oldProductName = PlayerSettings.productName;
            string oldAppIdentifier = PlayerSettings.applicationIdentifier;

            PlayerSettings.productName = $"VE2 - {name}";
            PlayerSettings.applicationIdentifier = "com.ImperialCollegeLondon." + name;

            // Get scenes from the Build Settings
            //string[] scenes = GetEnabledScenes();

            // Define build options
            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = new string[] { SceneManager.GetActiveScene().path },
                locationPathName = $"{destinationFolder}/{name}.apk",
                target = BuildTarget.Android,
                options = BuildOptions.None
            };

            // Execute build process
            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;

            if (summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                Debug.Log("APK Build succeeded: " + summary.totalSize + " bytes");
            }
            else
            {
                Debug.LogError("APK Build failed: " + summary.result);
            }

            PlayerSettings.productName = oldProductName;
            PlayerSettings.applicationIdentifier = oldAppIdentifier;
        }

        private static string[] GetEnabledScenes()
        {
            return EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();
        }

        private void GetSceneDataAndScripts(UnityEngine.SceneManagement.Scene sceneToExport)
        {
            scenePath = sceneToExport.path;

            if (sceneToExport.isDirty)
            {
                isSceneDirty = true;
                return;
            }
            isSceneDirty = false;

            locatedAssemblies = ScanSceneForAssemblies(sceneToExport);
            locatedAssemblies = RemoveUnnecessaryAssemblies(locatedAssemblies).ToArray();

            Debug.Log($"Found assemblies : {locatedAssemblies.Length}");
            if (locatedAssemblies.Length > 0)
                Debug.Log(string.Join(",", locatedAssemblies.Select(ass => $"{ass.FullName}\r\n")));

            (assembliesValid, assemblyErrors) = ValidateAssemblies(locatedAssemblies);

            //if (assembliesValid)
            //    locatedAssemblies = RemoveUnnecessaryAssemblies(locatedAssemblies).ToArray();

        }

        // Some assemblies should be excluded because they are already present with Unity installation.
        // We found no better way to do this but have a blacklist of names for the assemblies.
        private bool ShouldAssemblyBeExcluded(Assembly ass)
        {
            var name = ass.GetName().Name;
            foreach (var filter in Configuration.ignoreAssembliesFilters)
            {
                var rFilter = WildcardToRegex(filter);
                if (Regex.IsMatch(name, rFilter))
                    return true;
            }
            return false;
        }

        private string CheckAssembly(Assembly ass)
        {
            if (ass.FullName.Contains("Assembly-CSharp"))
                return "Some of the objects you want to export have scripts from the default  assembly, Assembly-CSharp . This is not allowed, because if loaded into VE2, it could cause conflict with other plugins and VE2 default assembly of the same name. Please use Assembly Definition Files for ALL the monobehaviours that will be attached to the activity, so that a proper assembly can be generated.";

            return null;
        }

        private (bool result, string error) ValidateAssemblies(Assembly[] assemblies)
        {
            List<string> errors = new List<string>();

            foreach (var ass in assemblies)
            {
                var potentialError = CheckAssembly(ass);
                if (!string.IsNullOrWhiteSpace(potentialError))
                    errors.Add(potentialError);

            }

            return (errors.Count == 0, string.Join(",", errors));
        }

        /// <returns>the final assembly list, having excluded the unnecessary ones</returns>
        private IEnumerable<Assembly> RemoveUnnecessaryAssemblies(IEnumerable<Assembly> assemblies)
        {
            var set = new HashSet<Assembly>(assemblies);

            var toExclude = set.Where(item => ShouldAssemblyBeExcluded(item)).ToArray();

            if (toExclude.Count() > 0)
                Debug.Log($"Ignoring Unity assemblies : {string.Join(",", toExclude.Select(ass => ass.FullName))}");

            set.ExceptWith(toExclude);

            return set;
        }

        private Assembly[] ScanSceneForAssemblies(UnityEngine.SceneManagement.Scene sceneToExport)
        {
            var assemblySet = new HashSet<Assembly>();

            GameObject[] rootGameObjects = sceneToExport.GetRootGameObjects();

            foreach (GameObject rootGameObject in rootGameObjects)
            {
                assemblySet.UnionWith(GetAssembliesInScriptsReferencedByGameObject(rootGameObject));

                // a "bodge" to get all children of the selected prefab: 
                var allChildren = rootGameObject.GetComponentsInChildren<Transform>(true);

                // not terribly optimal, but we don't care - editor script, rarely executed.
                foreach (var child in allChildren)
                    assemblySet.UnionWith(GetAssembliesInScriptsReferencedByGameObject(child.gameObject));
            }

            return assemblySet.ToArray();
        }

        private IEnumerable<Assembly> GetAssembliesInScriptsReferencedByGameObject(GameObject go)
        {
            foreach (var component in go.GetComponents<MonoBehaviour>())
            {
                if (component == null || component.GetType() == null || component.GetType().Assembly == null)
                {
                    Debug.LogWarning("Missing assemblies encountered on Gameobject: " + go.name);
                }

                yield return component.GetType().Assembly;
            }
        }
        private static string WildcardToRegex(string wildcard) => "^" + Regex.Escape(wildcard).Replace("\\?", ".").Replace("\\*", ".*") + "$";

        // private void OnDisable() 
        // {
        //     if (_fileSystem != null)
        //         _fileSystem.TearDown(); //TODO: expose interface
        // }
    }
}

#endif
