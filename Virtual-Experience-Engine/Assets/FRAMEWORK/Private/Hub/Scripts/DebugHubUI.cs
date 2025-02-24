using TMPro;
using UnityEngine;
using VE2.NonCore.Platform.API;

public class DebugHubUI : MonoBehaviour
{
    [SerializeField] private TMP_Text instancesListText;
    private IPlatformServiceInternal _platformService => (IPlatformServiceInternal)PlatformAPI.PlatformService;

    void OnEnable()
    {
        // _platformService = (PlatformService)FindFirstObjectByType<PlatformServiceProvider>().PlatformService;

        // if (_platformService.IsConnectedToServer)
        // {
        //     HandlePlatformReady();
        // }
        // else
        // {
        //     _platformService.OnConnectedToServer += HandlePlatformReady;
        // }
    }

    private void HandlePlatformReady()
    {
        string availableWorldList = "";

        foreach ((string, int) activeWorldNamesAndVersions in _platformService.ActiveWorldsNamesAndVersions)
            availableWorldList += $"{activeWorldNamesAndVersions.Item1} - <i>V<color=yellow>{activeWorldNamesAndVersions.Item2}</color></i>\n";

        instancesListText.text = availableWorldList;
    }

    public void OnGoToDevButtonPressed()
    {
        if (_platformService == null)
        {
            Debug.LogError("Platform service not found");
            return;
        }

        _platformService.RequestInstanceAllocation("Misc-Dev", "test", "NoVersion");
    }

    public void OnGoToDev2ButtonPressed()
    {
        if (_platformService == null)
        {
            Debug.LogError("Platform service not found");
            return;
        }

        _platformService.RequestInstanceAllocation("Misc-Dev", "dev2", "NoVersion");
    }

    private void OnDisable()
    {
        if (_platformService != null)
            _platformService.OnConnectedToServer -= HandlePlatformReady;
    }
}
