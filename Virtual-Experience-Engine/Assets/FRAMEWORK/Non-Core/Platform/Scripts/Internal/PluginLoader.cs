using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine.SceneManagement;
using Unity.Burst;
using VE2.NonCore.FileSystem.Internal;
using VE2.NonCore.FileSystem.API;
using VE2.Core.Player.API;

namespace VE2.NonCore.Platform.Internal
{
    // internal class PluginLoaderFactory
    // {
    //     public static PluginLoader Create(IPlatformSettingsHandler platformSettingsHandler, IPlayerServiceInternal playerServiceInternal)
    //     {
    //         return new PluginLoader();
    //     }
    // }

    internal class PluginLoader //TODO: Needs an interface?
    {
        private readonly IPlatformSettingsHandler _platformSettingsHandler;
        private readonly IPlayerServiceInternal _playerServiceInternal;

        public PluginLoader(IPlatformSettingsHandler platformSettingsHandler, IPlayerServiceInternal playerServiceInternal) 
        {
            _platformSettingsHandler = platformSettingsHandler;
            _playerServiceInternal = playerServiceInternal;
        }

        /// <summary>
        /// Finds the bundle and dlls at the given folder path, loads the dll into the assembly, unpacks the bundle, and instantiates GameObjects as children of the given transform
        /// Will delete the current plugin GameObjects if there are any
        /// </summary>
        /// <param name="worldFolderName"></param>
        public void LoadPlugin(string worldFolderName, int pluginVersion)
        {
            Debug.Log("Instantiate plugin - " + worldFolderName + " V" + pluginVersion);

            /*
                We need to check to see if we have that file, if not, we need to download it
                This means we need a file service! 
                BUT, that file service should arleady be in the scene 
                Although, it probs needs to be renamed to e.g V_PlatformProvider, rather V_PlatformIntegration
            */

            IFileSystemInternal fileSystem = GameObject.FindObjectOfType<InternalFileSystem>();
            if (fileSystem == null)
            {
                Debug.LogError("Could not load plugin as FileSystem  can't be found");
                //If offline, then what? There will still be a file system?
                //Maybe we want to be able to test without having gone through the plugin 
                return;
            }

            Debug.Log($"Look for files at {worldFolderName}/{pluginVersion:D3}");
            List<LocalFileDetails> localFiles = fileSystem.GetLocalFilesAtPath($"{worldFolderName}/{pluginVersion:D3}").Values.ToList();
            Debug.Log("Found " + localFiles.Count + " files");
            foreach (LocalFileDetails localFile in localFiles)
            {
                Debug.Log(localFile.FullLocalNameAndPath);
            }

            if (Application.platform == RuntimePlatform.Android)
            {
                LaunchAndroidAPK(worldFolderName);
            }
            else 
            {
                LoadWindowsPlugin(localFiles);
            }
        }

        public void LoadHub()
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                LaunchAndroidAPK("Virtual-Experience-Engine");
            }
            else 
            {
                SceneManager.LoadScene("Hub", LoadSceneMode.Single);
            }
        }

        private void LoadWindowsPlugin(List<LocalFileDetails> localFiles) 
        {
            //TODO: Need to make sure the DesktopSettingsBus objects are populated with their data

            AssetBundle bundle = null;

            foreach (LocalFileDetails localFile in localFiles)
            {
                if (localFile.FullLocalNameAndPath.EndsWith(".bundle"))
                {
                    bundle = GetBundle(localFile.FullLocalNameAndPath);
                    Debug.Log("Registered bundle - " + localFile.FullLocalNameAndPath + " success? " + (bundle != null));
                }
                else if (localFile.FullLocalNameAndPath.EndsWith(".dll"))
                {
                    if (!localFile.FullLocalNameAndPath.Contains("lib_burst_generated"))
                    {
                        RegisterAssembly(localFile.FullLocalNameAndPath);
                        Debug.Log("Registered managed assembly - " + localFile.FullLocalNameAndPath);
                    }
                    else
                    {
                        BurstRuntime.LoadAdditionalLibrary(localFile.FullLocalNameAndPath);
                        Debug.Log("Registered burst assembly - " + localFile.FullLocalNameAndPath);
                    }
                }
            }

            if (bundle == null)
            {
                Debug.LogError("Could not load plugin as bundle could not be found");
                return;
            }

            string[] scenePath = bundle.GetAllScenePaths();
            SceneManager.LoadScene(scenePath[0], LoadSceneMode.Single);
        }

        private void LaunchAndroidAPK(string worldFolderName) //TODO: Version number will have to be part of apk name?
        {
            // string apkName = worldFolderName.Split('-')[1]; //Chop off the category
            // string packageName = $"{"com.ImperialCollegeLondon"}.{apkName}";
            string packageName = $"com.ImperialCollegeLondon.{worldFolderName}";

            Debug.Log($"Try launch {packageName}");
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaObject intent = new AndroidJavaObject("android.content.Intent");

            // Set the target package and activity
            intent.Call<AndroidJavaObject>("setClassName", packageName, "com.unity3d.player.UnityPlayerGameActivity");

            if (intent != null)
            {
                intent = _platformSettingsHandler.AddArgsToIntent(intent);
                intent = _playerServiceInternal.AddArgsToIntent(intent);

                currentActivity.Call("startActivity", intent);
                Debug.Log("App launched successfully");
            }
            else
            {
                Debug.LogError("Launch intent is null. The app might not be installed.");
            }
        }

        Dictionary<string, Assembly> registeredAssemblies = new Dictionary<string, Assembly>();
        private void RegisterAssembly(string path)
        {
            string name = Path.GetFileName(path);
            if (registeredAssemblies.TryGetValue(name, out Assembly result))
                return;
            Assembly assembly = Assembly.LoadFile(path);
            registeredAssemblies.Add(name, assembly);
        }

        private AssetBundle GetBundle(string bundleFilePath)
        {
            string bundleFileName = Path.GetFileName(bundleFilePath);
            AssetBundle bundle = null;

            foreach (AssetBundle loadedBundle in AssetBundle.GetAllLoadedAssetBundles())
            {
                Debug.Log("Check bundle - " + loadedBundle.name + " == " + bundleFilePath);

                if (loadedBundle.name == bundleFileName)
                {
                    bundle = loadedBundle;
                }
            }

            Debug.Log($"GETTING BUNDLE {bundleFilePath}, already loaded? {bundle != null}");

            if (bundle == null)
            {
                Debug.Log($"LOADING BUNDLE {bundleFilePath}");
                bundle = AssetBundle.LoadFromFile(bundleFilePath); //bundle already
                if (bundle == null)
                {
                    Debug.Log(($"Could not load bundle from file {bundleFilePath}"));
                }
            }
            return bundle;
        }

    }
}
