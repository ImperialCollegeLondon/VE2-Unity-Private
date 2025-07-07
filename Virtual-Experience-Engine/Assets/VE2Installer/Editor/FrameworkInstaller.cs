using UnityEditor;
using UnityEngine;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using System.Collections.Generic;

// Alias to resolve ambiguity between UnityEditor.PackageManager.PackageInfo and UnityEditor.PackageInfo
using PackageInfo = UnityEditor.PackageManager.PackageInfo;
using System;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;

public class FrameworkInstaller : EditorWindow
{
    // Static flag indicating whether core modules are installed.
    public static bool CoreModulesInstalled = false;
    private const string CoreModulesInstalledKey = "CoreModulesInstalled";

    // --- State flags to Check if DOTween exists ---
    private bool initialDotweenExists = false;
    private bool isDotweenDetected = false;
    private bool isRemovingDotween = false;
    private const string DOTWEEN_ASSET_PATH = "Assets/Plugins/Demigiant/DOTween";
    private bool allPackagesInstalled = false;

    // Installation-related fields.
    private Queue<string> packageQueue = new Queue<string>();
    private AddRequest currentRequest;
    private int totalPackages = 0;
    private int installedCount = 0;
    private string currentInstalling = "";
    private bool isInstalling = false;
    private string downloadStatus = "";
    private string installStatus = "";
    private const float REQUEST_TIMEOUT = 30f;
    private float currentRequestStartTime = 0f;

    // Removal-related fields.
    private Queue<string> removalQueue = new Queue<string>();
    private RemoveRequest currentRemovalRequest;
    private int totalRemovals = 0;
    private int removedCount = 0;
    private string removalStatus = "";
    private bool isRemoving = false;
    private const float REMOVAL_REQUEST_TIMEOUT = 30f;
    private float currentRemovalRequestStartTime = 0f;
    private string currentRemovingPackage = "";

    // Path to the local VE2 package.
    private string ve2Path = "";
    private const string VE2PathKey = "VE2_LocalPath";
    private bool isValidVE2LocalPath = false;
    private enum VE2InstallLocation
    {
        Remote,
        Local
    }

    // Selecting Remote or Local VE2
    private VE2InstallLocation ve2InstallLocation = VE2InstallLocation.Remote;
    private const string UseRemoteVE2Key = "UseRemoteVE2";
    private const string VE2RemoteUrl = "https://github.com/ImperialCollegeLondon/VE2-Distribution.git?path=VE2#main";


    // Known core module repository names in installation order.
    // To remove in reverse order, we will iterate over this array backwards.
    private static readonly string[] _repoNames = new string[]
    {
        "Unity-Editor-Toolbox",
        "Unity3D-NSubstitute",
        "NuGetForUnity",
        "VE2-Distribution",
        "ParrelSync",
    };

    // Cached list of installed packages.
    private ListRequest listRequest;
    private List<PackageInfo> installedPackages;

    // --- Menu Items ---

    [MenuItem("VE2/Install VE2", priority = 100)]
    public static void ShowInstallerWindow()
    {
        FrameworkInstaller window = GetWindow<FrameworkInstaller>("VE2 Framework Installer");
        window.position = new Rect(Screen.width / 2, Screen.height / 2, 400, 350);
    }

    // The removal menu item is only enabled after core modules are installed.
    // [MenuItem("VE2 Framework/Remove Core Modules", true)]
    // private static bool ValidateRemoveCoreModules()
    // {
    //     return CoreModulesInstalled;
    // }

    // [MenuItem("VE2 Framework/Remove Core Modules")]
    // private static void RemoveCoreModulesMenuItem()
    // {
    //     FrameworkInstaller window = GetWindow<FrameworkInstaller>("VE2 Framework Installer");
    //     window.StartRemoval();
    // }

    // --- EditorWindow Lifecycle ---
    void OnEnable()
    {
        // Restore persisted state.
        CoreModulesInstalled = EditorPrefs.GetBool(CoreModulesInstalledKey, false);

        // Fetch installed packages so we can detect DOTween
        RefreshDotweenDetection();

        listRequest = Client.List(true, true);
        EditorApplication.update += UpdateInstalledPackages;

        ve2InstallLocation = VE2InstallLocation.Remote;

        // load last‐used VE2 path
        ve2Path = EditorPrefs.GetString(VE2PathKey, "");
    }

        // --- UI ---
    void OnGUI()
    {
        GUILayout.Space(10);

        // ——— REMOTE vs LOCAL toggle ——————————————————————————
        VE2InstallLocation newVE2InstallLocation = (VE2InstallLocation)EditorGUILayout.EnumPopup(
            new GUIContent("VE2 Install Location", "Select whether to install VE2 from Remote or Local folder"),
            ve2InstallLocation);

        if (newVE2InstallLocation != ve2InstallLocation)
        {
            ve2InstallLocation = newVE2InstallLocation;
            EditorPrefs.SetInt(UseRemoteVE2Key, (int)ve2InstallLocation);
        }
        GUILayout.Space(8);


        if (ve2InstallLocation == VE2InstallLocation.Local)
        {
            // — Validate if path exists ———————————————————————
            isValidVE2LocalPath = Directory.Exists(ve2Path) && File.Exists(Path.Combine(ve2Path, "package.json"));

            if (!string.IsNullOrEmpty(ve2Path) && !isValidVE2LocalPath)
                EditorGUILayout.HelpBox("Selected folder does not contain a VE2 package.json", MessageType.Error);

            // Show path picker, but disable it when in Remote mode
            EditorGUILayout.LabelField("Local VE2 Folder", EditorStyles.boldLabel);
            //EditorGUI.BeginDisabledGroup(useRemoteVE2);
            EditorGUILayout.BeginHorizontal();
            ve2Path = EditorGUILayout.TextField(ve2Path, GUILayout.ExpandWidth(true));
            if (GUILayout.Button("…", GUILayout.Width(30)))
            {
                var picked = EditorUtility.OpenFolderPanel("Select VE2 Package Folder", ve2Path, "");
                if (!string.IsNullOrEmpty(picked))
                {
                    ve2Path = picked;
                    EditorPrefs.SetString(VE2PathKey, ve2Path);
                }
            }
            EditorGUILayout.EndHorizontal();
            //EditorGUI.EndDisabledGroup();
            GUILayout.Space(8);
        }

        if (ve2InstallLocation == VE2InstallLocation.Local && !isValidVE2LocalPath)
        {
            EditorGUILayout.HelpBox("\ufe0f Select a Path To VE2 Framework before you begin installation", MessageType.Warning);
        }
        else if (isInstalling)
        {
            EditorGUILayout.HelpBox("\ufe0f Please do not close this window while packages are installing.", MessageType.Warning);
        }
        else if (isRemoving)
        {
            EditorGUILayout.HelpBox("\ufe0f Removing core modules. Please wait...", MessageType.Warning);
        }
        else if(initialDotweenExists)
        {
            EditorGUILayout.HelpBox("\ufe0f It is highly recommended that you remove DOTween first before installing.", MessageType.Warning);
        }
        else
        {
            EditorGUILayout.HelpBox("\u2705 Ready to install VE2. Click button below to continue", MessageType.Info);
        }

        GUILayout.Space(15);
        GUILayout.Label($"Install Progress: {installedCount} of {totalPackages} processed", EditorStyles.boldLabel);
        GUILayout.Label("Current Operation: " + (string.IsNullOrEmpty(downloadStatus) ? installStatus : downloadStatus), EditorStyles.wordWrappedLabel);

        GUILayout.Space(10);
        float installProgress = totalPackages > 0 ? (float)installedCount / totalPackages : 0f;
        EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(false, 20), installProgress, $"{installedCount}/{totalPackages}");

        GUILayout.Space(10);
        GUILayout.Label("Download Status: " + downloadStatus, EditorStyles.wordWrappedLabel);
        GUILayout.Label("Installation Status: " + installStatus, EditorStyles.wordWrappedLabel);

        float removeProgress = totalPackages > 0 ? (float)installedCount / totalPackages : 0f;
        EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(false, 20), removeProgress, $"{installedCount}/{totalPackages}");

        GUILayout.Space(20);
        if (initialDotweenExists)
        {
            if (GUILayout.Button("Remove DOTween"))
                StartRemoveDotween();
        }

        GUILayout.Space(8);

        // — Install button, only enabled when we have a good path —
        EditorGUI.BeginDisabledGroup((ve2InstallLocation == VE2InstallLocation.Local && !isValidVE2LocalPath) || isInstalling || isRemoving);
        if (GUILayout.Button(initialDotweenExists ? "Install VE2 Anyway" : "Install VE2"))
        {
            // stash into the queue
            StartInstallation();
        }
        EditorGUI.EndDisabledGroup();
        GUILayout.FlexibleSpace();
    }


    // --- Installation Process ---
    void StartInstallation()
    {
        // Reset installation state.
        packageQueue.Clear();
        CoreModulesInstalled = false;
        EditorPrefs.SetBool(CoreModulesInstalledKey, false);
        isInstalling = true;
        installedCount = 0;
        totalPackages = 0;
        downloadStatus = "";
        installStatus = "";
        var fileUrl = "file:" + ve2Path.Replace('\\', '/');
        string ve2Package = ve2InstallLocation == VE2InstallLocation.Remote ? VE2RemoteUrl : fileUrl;

        // Enqueue the core module package URLs in installation order.
        packageQueue.Enqueue("https://github.com/arimger/Unity-Editor-Toolbox.git#upm");
        packageQueue.Enqueue("https://github.com/Thundernerd/Unity3D-NSubstitute.git");
        packageQueue.Enqueue("https://github.com/GlitchEnzo/NuGetForUnity.git?path=/src/NuGetForUnity");
        packageQueue.Enqueue(ve2Package);
        packageQueue.Enqueue("https://github.com/VeriorPies/ParrelSync.git?path=/ParrelSync");

        totalPackages = packageQueue.Count;
        // Fetch the list of already installed packages.
        listRequest = Client.List(true, true);
        EditorApplication.update += UpdateInstalledPackages;

        // Start processing package installations.
        EditorApplication.update += InstallNextPackage;
    }

    void UpdateInstalledPackages()
    {
        if (listRequest != null && listRequest.IsCompleted)
        {
            installedPackages = listRequest.Result.ToList();
            listRequest = null;
            EditorApplication.update -= UpdateInstalledPackages;
        }

        // Debug.Log($"Checked installed packages...");
        // foreach (var pkg in installedPackages)
        // {
        //     Debug.Log($"Already Installed Package: {pkg.packageId}");
        // }
    }

    void RefreshDotweenDetection()
    {
        if (initialDotweenExists)
        {
            if (allPackagesInstalled)
            {
                initialDotweenExists = false;
                return;
            }
        }

        if (AssetDatabase.IsValidFolder(DOTWEEN_ASSET_PATH))
        {
            initialDotweenExists = true;
            Debug.Log("✅ DOTween detected in Assets folder.");
            return;
        }

        if (AppDomain.CurrentDomain.GetAssemblies().Any(a =>
               a.GetName().Name.IndexOf("dotween", StringComparison.OrdinalIgnoreCase) >= 0))
        {
            Debug.Log("✅ DOTween detected in assemblies.");
            initialDotweenExists = true;
            return;
        }

        if (Type.GetType("DG.Tweening.Tween") != null)
        {
            Debug.Log("✅ DOTween detected via reflection.");
            initialDotweenExists = true;
            return;
        }

        // nothing detected
        initialDotweenExists = false;
    }

    void StartRemoveDotween()
    {
        isRemovingDotween = true;

        // 1) Find and delete any folder asset named "DOTween".
        string[] guids = AssetDatabase.FindAssets("DOTween t:Folder");
        foreach (var guid in guids)
        {
            string folderPath = AssetDatabase.GUIDToAssetPath(guid);
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                Debug.Log($"Deleting DOTween folder: {folderPath}");
                AssetDatabase.DeleteAsset(folderPath);
            }
        }

        // 2) (Redundancy) Delete any leftover DLL whose assembly name contains "DOTween"
        var asm = AppDomain.CurrentDomain
                           .GetAssemblies()
                           .FirstOrDefault(a => a.GetName().Name
                               .IndexOf("DOTween", StringComparison.OrdinalIgnoreCase) >= 0);
        if (asm != null)
        {
            var asmPath = new Uri(asm.CodeBase).LocalPath;
            if (asmPath.StartsWith(Application.dataPath, StringComparison.OrdinalIgnoreCase))
            {
                string rel = "Assets" + asmPath.Substring(Application.dataPath.Length);
                if (AssetDatabase.DeleteAsset(rel))
                    Debug.Log($"Deleted DOTween DLL: {rel}");
            }
        }

        // 3) (Redundancy) Delete any MonoScript in DG.Tweening namespace
        foreach (var ms in MonoImporter.GetAllRuntimeMonoScripts())
        {
            var cls = ms.GetClass();
            if (cls != null && cls.Namespace != null && cls.Namespace.StartsWith("DG.Tweening"))
            {
                string scriptPath = AssetDatabase.GetAssetPath(ms);
                if (AssetDatabase.DeleteAsset(scriptPath))
                    Debug.Log($"Deleted DOTween script: {scriptPath}");
            }
        }

        // Refresh the AssetDatabase so the Editor UI updates immediately
        AssetDatabase.Refresh();
        Debug.Log("✅ DOTween asset scripts/DLL removed.");
        isRemovingDotween = false;
        initialDotweenExists = false;
        Repaint();
    }

    void InstallNextPackage()
    {
        if (installedPackages == null)
            return;

        // Queue next package if available.
        if (packageQueue.Count > 0)
        {
            string nextPackage = packageQueue.Peek();
            if (IsPackageInstalled(nextPackage))
            {
                installStatus = "Skipped (already installed): " + nextPackage;
                Debug.Log($"\u26a0 Package already installed: {nextPackage}");
                packageQueue.Dequeue();
                installedCount++;
                return; // Let the update loop call InstallNextPackage next frame.
            }
            Debug.Log($"Installing: {nextPackage}");
            currentInstalling = packageQueue.Dequeue();
            downloadStatus = "Downloading: " + currentInstalling;
            currentRequest = Client.Add(currentInstalling);
            currentRequestStartTime = Time.realtimeSinceStartup;
        }
        else
        {
            // All installations complete.
            EditorApplication.update -= InstallNextPackage;
            Debug.Log("\ud83c\udf89 All packages installed.");
            currentInstalling = "Installation complete.";
            isInstalling = false;
            allPackagesInstalled = true;
            downloadStatus = "";
            installStatus = "Installation complete.";
            installedCount = totalPackages;

            if (!PlayerSettings.runInBackground)
            {
                PlayerSettings.runInBackground = true;
                Debug.Log("\ud83d\udd3a 'Run In Background' has been enabled in Player Settings for VE2.");
            }
               
            // Set the static flag to enable removal.
            CoreModulesInstalled = true;
            EditorPrefs.SetBool(CoreModulesInstalledKey, true);
        }
    }

    void AbortInstallation(string errorMessage)
    {
        Debug.LogError(errorMessage);
        installStatus = errorMessage;
        isInstalling = false;
        EditorUtility.DisplayDialog("Installation Failed", errorMessage, "OK");
        packageQueue.Clear();
        CoreModulesInstalled = false;
        EditorPrefs.SetBool(CoreModulesInstalledKey, false);
        EditorApplication.update -= InstallNextPackage;
    }

    bool IsPackageInstalled(string packageUrl)
    {
        // Assuming packageUrl contains the package name or can be used to extract it
        string packageName = ExtractPackageNameAndPath(packageUrl); // This would extract the unique packageName from packageUrl

        if (string.IsNullOrEmpty(packageName))
            return false;

        foreach (var pkg in installedPackages)
        {
            if (pkg.name != null && pkg.name.Equals(packageName, System.StringComparison.OrdinalIgnoreCase))
            {
                return true; // Package with the same name is already installed
            }
        }
        return false; // No matching package name found
    }
    string ExtractPackageNameAndPath(string packageUrl)
    {
        // Extract the base URL (repo URL without query and fragment)
        Uri uri;
        try
        {
            uri = new Uri(packageUrl.Split('?')[0]); // Remove query parameters if present
        }
        catch (UriFormatException)
        {
            return string.Empty;
        }

        // Extract the last part of the URL path (i.e., the repo name, e.g., Unity-Editor-Toolbox)
        string repoName = uri.AbsolutePath.Split('/').LastOrDefault()?.Replace(".git", "");

        // Check if there's a "path" parameter in the URL (after '?')
        string path = string.Empty;
        var queryParams = Uri.UnescapeDataString(packageUrl.Split('?').Skip(1).FirstOrDefault() ?? "");
        if (queryParams.Contains("path="))
        {
            path = queryParams.Split('&')
                            .FirstOrDefault(p => p.StartsWith("path="))?
                            .Substring(5); // Remove "path=" prefix
        }

        // If path is empty or just the root, return "root"
        if (string.IsNullOrEmpty(path))
        {
            path = "root";
        }

        return $"{repoName} (path: {path})";  // Example: "Unity-Editor-Toolbox (path: root)"
    }

    string ExtractRepositoryName(string packageUrl)
    {
        int queryIndex = packageUrl.IndexOf('?');
        if (queryIndex != -1)
            packageUrl = packageUrl.Substring(0, queryIndex);
        int hashIndex = packageUrl.IndexOf('#');
        if (hashIndex != -1)
            packageUrl = packageUrl.Substring(0, hashIndex);
        int lastSlash = packageUrl.LastIndexOf('/');
        int gitIndex = packageUrl.LastIndexOf(".git");
        if (lastSlash == -1 || gitIndex == -1 || gitIndex <= lastSlash)
            return "";
        return packageUrl.Substring(lastSlash + 1, gitIndex - lastSlash - 1);
    }

    // --- Removal Process (unchanged) ---
    void StartRemoval()
    {
        if (installedPackages == null)
        {
            listRequest = Client.List(true, true);
            EditorApplication.update += WaitForInstalledPackagesForRemoval;
            return;
        }

        removalQueue.Clear();
        isRemoving = true;
        removedCount = 0;
        totalRemovals = 0;
        removalStatus = "Starting removal...";

        for (int i = _repoNames.Length - 1; i >= 0; i--)
        {
            string repoName = _repoNames[i];
            foreach (var pkg in installedPackages)
            {
                if ((pkg.packageId != null && pkg.packageId.IndexOf(repoName, System.StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (pkg.name != null && pkg.name.IndexOf(repoName, System.StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    string validPackageId = pkg.packageId;
                    int atIndex = validPackageId.IndexOf('@');
                    if (atIndex != -1)
                    {
                        validPackageId = validPackageId.Substring(0, atIndex);
                    }
                    removalQueue.Enqueue(validPackageId);
                }
            }
        }
        totalRemovals = removalQueue.Count;
        EditorApplication.update += RemoveNextPackage;
    }

    void WaitForInstalledPackagesForRemoval()
    {
        if (listRequest != null && listRequest.IsCompleted)
        {
            installedPackages = new List<PackageInfo>();
            foreach (var pkg in listRequest.Result)
            {
                installedPackages.Add(pkg);
            }
            listRequest = null;
            EditorApplication.update -= WaitForInstalledPackagesForRemoval;
            StartRemoval();
        }
    }

    void RemoveNextPackage()
    {
        if (currentRemovalRequest != null && !currentRemovalRequest.IsCompleted)
        {
            if (Time.realtimeSinceStartup - currentRemovalRequestStartTime > REMOVAL_REQUEST_TIMEOUT)
            {
                Debug.LogError($"Removal Timeout: {currentRemovingPackage} took longer than {REMOVAL_REQUEST_TIMEOUT} seconds.");
                removalStatus = "Timeout: " + currentRemovingPackage;
                currentRemovalRequest = null;
                removedCount++;
                EditorApplication.delayCall += RemoveNextPackage;
                return;
            }
            removalStatus = "Removing: " + currentRemovingPackage;
            return;
        }

        if (currentRemovalRequest != null && currentRemovalRequest.IsCompleted)
        {
            if (currentRemovalRequest.Status == StatusCode.Success)
            {
                removalStatus = "Removed: " + currentRemovingPackage;
                Debug.Log($"\u2705 Removed: {currentRemovingPackage}");
            }
            else
            {
                removalStatus = "Failed to remove: " + currentRemovingPackage;
                Debug.LogError($"\u274C Removal failed: {currentRemovalRequest.Error.message}");
            }
            removedCount++;
            currentRemovalRequest = null;
        }

        if (removalQueue.Count > 0)
        {
            string nextRemoval = removalQueue.Dequeue();
            currentRemovingPackage = nextRemoval;
            removalStatus = "Removing: " + nextRemoval;
            currentRemovalRequest = Client.Remove(nextRemoval);
            currentRemovalRequestStartTime = Time.realtimeSinceStartup;
        }
        else
        {
            EditorApplication.update -= RemoveNextPackage;
            removalStatus = "Removal complete.";
            isRemoving = false;
            removedCount = totalRemovals;
            CoreModulesInstalled = false;
            EditorPrefs.SetBool(CoreModulesInstalledKey, false);
        }
    }
}
