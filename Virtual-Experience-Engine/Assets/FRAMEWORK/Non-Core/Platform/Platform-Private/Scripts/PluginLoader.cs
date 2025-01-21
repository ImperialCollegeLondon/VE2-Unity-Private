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
public static class V_PluginLoader
{
    private static GameObject currentPluginObject = null;
    /// <summary>
    /// Finds the bundle and dlls at the given folder path, loads the dll into the assembly, unpacks the bundle, and instantiates GameObjects as children of the given transform
    /// Will delete the current plugin GameObjects if there are any
    /// </summary>
    /// <param name="pluginName"></param>
    public static void LoadPlugin(string pluginName, int pluginVersion)
    {
        // destroys and reinstantiates the plugin object.
        if (currentPluginObject != null)
        {
            GameObject.Destroy(currentPluginObject);
        }

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

        AssetBundle bundle = null;

        foreach (LocalFileDetails localFile in localFiles)
        {
            if (localFile.FullLocalNameAndPath.EndsWith(".bundle"))
            {
                bundle = BundleUtility.getBundle(localFile.FullLocalNameAndPath);
            }
            else if (localFile.FullLocalNameAndPath.EndsWith(".dll"))
            {
                if (!localFile.FullLocalNameAndPath.Contains("lib_burst_generated"))
                {
                    AssemblyUtility.registerAssembly(localFile.FullLocalNameAndPath);
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

}

//TODO maybe move the below classes into a different file 

public static class AssemblyUtility
{
    static Dictionary<string, Assembly> registeredAssemblies = new Dictionary<string, Assembly>();
    public static void registerAssembly(string path)
    {
        string name = Path.GetFileName(path);
        if (registeredAssemblies.TryGetValue(name, out Assembly result))
            return;
        Assembly assembly = Assembly.LoadFile(path);
        registeredAssemblies.Add(name, assembly);
    }
}

public static class BundleUtility
{
    static Dictionary<string, AssetBundle> cachedBundles = new Dictionary<string, AssetBundle>();
    public static AssetBundle getBundle(string bundleFilePath)
    {
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