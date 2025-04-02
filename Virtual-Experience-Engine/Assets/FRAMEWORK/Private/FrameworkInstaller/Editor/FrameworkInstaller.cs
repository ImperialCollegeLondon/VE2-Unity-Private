using UnityEditor;
using UnityEngine;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using System.Collections.Generic;

// Alias to resolve ambiguity between UnityEditor.PackageManager.PackageInfo and UnityEditor.PackageInfo
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

public class FrameworkInstaller : EditorWindow
{
    // Static flag indicating whether core modules are installed.
    public static bool CoreModulesInstalled = false;
    private const string CoreModulesInstalledKey = "CoreModulesInstalled";


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

    // Known core module repository names in installation order.
    // To remove in reverse order, we will iterate over this array backwards.
    private static readonly string[] coreModuleRepoNames = new string[]
    {
        "Unity-Editor-Toolbox",
        "Unity3D-NSubstitute",
        "NuGetForUnity",
        "com.ic.ve2"
    };

    // Cached list of installed packages.
    private ListRequest listRequest;
    private List<PackageInfo> installedPackages;

    // --- Menu Items ---

    [MenuItem("VE2 Framework/Install Core Modules")]
    public static void ShowInstallerWindow()
    {
        FrameworkInstaller window = GetWindow<FrameworkInstaller>("VE2 Framework Installer");
        window.position = new Rect(Screen.width / 2, Screen.height / 2, 400, 280);
        window.StartInstallation();
    }

    // The removal menu item is only enabled after core modules are installed.
    [MenuItem("VE2 Framework/Remove Core Modules", true)]
    private static bool ValidateRemoveCoreModules()
    {
        return CoreModulesInstalled;
    }

    [MenuItem("VE2 Framework/Remove Core Modules")]
    private static void RemoveCoreModulesMenuItem()
    {
        FrameworkInstaller window = GetWindow<FrameworkInstaller>("VE2 Framework Installer");
        window.StartRemoval();
    }

    // --- EditorWindow Lifecycle ---
    void OnEnable()
    {
        // Restore persisted state.
        CoreModulesInstalled = EditorPrefs.GetBool(CoreModulesInstalledKey, false);
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

        // Enqueue the core module package URLs in installation order.
        packageQueue.Enqueue("https://github.com/arimger/Unity-Editor-Toolbox.git#upm");
        packageQueue.Enqueue("https://github.com/Thundernerd/Unity3D-NSubstitute.git");
        packageQueue.Enqueue("https://github.com/GlitchEnzo/NuGetForUnity.git?path=/src/NuGetForUnity");
        packageQueue.Enqueue("https://github.com/Oluwatosin-Ogunyebi/com.ic.ve2.git");

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
            installedPackages = new List<PackageInfo>();
            foreach (var pkg in listRequest.Result)
            {
                installedPackages.Add(pkg);
            }
            listRequest = null;
            EditorApplication.update -= UpdateInstalledPackages;
        }
    }

    void InstallNextPackage()
    {
        if (installedPackages == null)
            return;

        //// If a package add request is active, check for timeout.
        //if (currentRequest != null && !currentRequest.IsCompleted)
        //{
        //    if (Time.realtimeSinceStartup - currentRequestStartTime > REQUEST_TIMEOUT)
        //    {
        //        // Check if package is installed even though the request didn't complete.
        //        if (IsPackageInstalled(currentInstalling))
        //        {
        //            installStatus = "Installed (detected after timeout): " + currentInstalling;
        //            Debug.Log($"Detected that {currentInstalling} is installed.");
        //            installedCount++;
        //            currentRequest = null;
        //            EditorApplication.delayCall += InstallNextPackage;
        //            return;
        //        }
        //        else
        //        {
        //            string errorMsg = $"Installation failed: {currentInstalling} timed out after {REQUEST_TIMEOUT} seconds.";
        //            AbortInstallation(errorMsg);
        //            return;
        //        }
        //    }
        //    downloadStatus = "Downloading: " + currentInstalling;
        //    return;
        //}

        //// Process a completed add request.
        //if (currentRequest != null && currentRequest.IsCompleted)
        //{
        //    if (currentRequest.Status == StatusCode.Success)
        //    {
        //        if (currentRequest.Result == null)
        //        {
        //            AbortInstallation($"Installation failed: {currentInstalling} returned a null result.");
        //            return;
        //        }
        //        installStatus = "Installed: " + currentRequest.Result.packageId;
        //        Debug.Log($"\u2705 Installed: {currentRequest.Result.packageId}");
        //        installedPackages.Add(currentRequest.Result);
        //    }
        //    else
        //    {
        //        string errorMsg = $"Installation failed: {currentInstalling} failed to install. Error: " +
        //            (currentRequest.Error != null ? currentRequest.Error.message : "Unknown error.");
        //        AbortInstallation(errorMsg);
        //        return;
        //    }
        //    downloadStatus = "";
        //    installedCount++;
        //    currentRequest = null;
        //}

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
            downloadStatus = "";
            installStatus = "Installation complete.";
            installedCount = totalPackages;
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
        string repoName = ExtractRepositoryName(packageUrl);
        if (string.IsNullOrEmpty(repoName))
            return false;
        foreach (var pkg in installedPackages)
        {
            if ((pkg.packageId != null && pkg.packageId.IndexOf(repoName, System.StringComparison.OrdinalIgnoreCase) >= 0) ||
                (pkg.name != null && pkg.name.IndexOf(repoName, System.StringComparison.OrdinalIgnoreCase) >= 0))
            {
                return true;
            }
        }
        return false;
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

        for (int i = coreModuleRepoNames.Length - 1; i >= 0; i--)
        {
            string repoName = coreModuleRepoNames[i];
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

    // --- UI ---
    void OnGUI()
    {
        GUILayout.Space(10);
        if (isInstalling)
        {
            EditorGUILayout.HelpBox("\u26a0\ufe0f Please do not close this window while packages are installing.", MessageType.Warning);
        }
        else if (isRemoving)
        {
            EditorGUILayout.HelpBox("\u26a0\ufe0f Removing core modules. Please wait...", MessageType.Warning);
        }
        else
        {
            EditorGUILayout.HelpBox("\u2705 Installation complete. Use the menu item to remove core modules.", MessageType.Info);
        }

        GUILayout.Space(10);
        GUILayout.Label($"Install Progress: {installedCount} of {totalPackages} processed", EditorStyles.boldLabel);
        GUILayout.Label("Current Operation: " + (string.IsNullOrEmpty(downloadStatus) ? installStatus : downloadStatus), EditorStyles.wordWrappedLabel);

        GUILayout.Space(10);
        float installProgress = totalPackages > 0 ? (float)installedCount / totalPackages : 0f;
        EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(false, 20), installProgress, $"{installedCount}/{totalPackages}");

        GUILayout.Space(10);
        GUILayout.Label("Download Status: " + downloadStatus, EditorStyles.wordWrappedLabel);
        GUILayout.Label("Installation Status: " + installStatus, EditorStyles.wordWrappedLabel);

        if (isRemoving)
        {
            GUILayout.Space(20);
            GUILayout.Label($"Remove Progress: {removedCount} of {totalRemovals} processed", EditorStyles.boldLabel);
            GUILayout.Label("Removal Status: " + removalStatus, EditorStyles.wordWrappedLabel);
            float removeProgress = totalRemovals > 0 ? (float)removedCount / totalRemovals : 0f;
            EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(false, 20), removeProgress, $"{removedCount}/{totalRemovals}");
        }

        GUILayout.FlexibleSpace();
    }
}
