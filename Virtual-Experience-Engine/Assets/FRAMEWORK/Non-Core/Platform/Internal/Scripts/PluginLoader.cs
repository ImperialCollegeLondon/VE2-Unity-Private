using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine.SceneManagement;
using Unity.Burst;
using VE2.PlatformNetworking;
using VE2_NonCore_FileSystem_Interfaces_Internal;
using VE2_NonCore_FileSystem;
using VE2_NonCore_FileSystem_Interfaces_Common;

//TODO needs some looking at, we're no longer instantiating a GameObject 
public class PluginLoader //TODO: Needs an interface
{
    public PluginLoader() {}

    /// <summary>
    /// Finds the bundle and dlls at the given folder path, loads the dll into the assembly, unpacks the bundle, and instantiates GameObjects as children of the given transform
    /// Will delete the current plugin GameObjects if there are any
    /// </summary>
    /// <param name="pluginName"></param>
    public void LoadPlugin(string pluginName, int pluginVersion)
    {
        Debug.Log("Instantiate plugin - " + pluginName + " V" + pluginVersion);

        /*
            We need to check to see if we have that file, if not, we need to download it
            This means we need a file service! 
            BUT, that file service should arleady be in the scene 
            Although, it probs needs to be renamed to e.g V_PlatformProvider, rather V_PlatformIntegration
        */

        IInternalFileSystem fileSystem = GameObject.FindObjectOfType<V_InternalFileSystem>();
        if (fileSystem == null)
        {
            Debug.LogError("Could not load plugin as FileSystem  can't be found");
            //If offline, then what? There will still be a file system?
            //Maybe we want to be able to test without having gone through the plugin 
            return;
        }

        Debug.Log($"Look for files at {pluginName}/{pluginVersion:D3}");
        List<LocalFileDetails> localFiles = fileSystem.GetLocalFilesAtPath($"{pluginName}/{pluginVersion:D3}").Values.ToList();
        Debug.Log("Found " + localFiles.Count + " files");
        foreach (LocalFileDetails localFile in localFiles)
        {
            Debug.Log(localFile.FullLocalNameAndPath);
        }

        if (Application.platform == RuntimePlatform.Android)
        {
            LaunchAndroidAPK(pluginName);
        }
        else 
        {
            LoadWindowsPlugin(localFiles);
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

    private void LaunchAndroidAPK(string apkName)
    {
        string packageName = $"{"com.ImperialCollegeLondon"}.{apkName}";

        Debug.Log($"Try launch {packageName}");
        AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        AndroidJavaObject intent = new AndroidJavaObject("android.content.Intent");

        // Set the target package and activity
        intent.Call<AndroidJavaObject>("setClassName", packageName, "com.unity3d.player.UnityPlayerGameActivity");

        if (intent != null)
        {
            //TODO: Need all settings handlers, need to inject command args here

            intent.Call<AndroidJavaObject>("putExtra", $"arg0", "TestArg0");
            intent.Call<AndroidJavaObject>("putExtra", $"arg1", "TestArg1");

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

    Dictionary<string, AssetBundle> cachedBundles = new Dictionary<string, AssetBundle>();
    private AssetBundle GetBundle(string bundleFilePath)
    {
        Debug.Log($"GETTING BUNDLE {bundleFilePath}, cached? {cachedBundles.ContainsKey(bundleFilePath)}");
        if (!cachedBundles.TryGetValue(bundleFilePath, out AssetBundle bundle))
        {
            Debug.Log(($"LOADING BUNDLE {bundleFilePath}"));
            bundle = AssetBundle.LoadFromFile(bundleFilePath);
            if (bundle == null)
            {
                Debug.Log(($"Could not load bundle from file {bundleFilePath}"));
            }
            cachedBundles.Add(bundleFilePath, bundle);
        }
        return bundle;
    }

}
