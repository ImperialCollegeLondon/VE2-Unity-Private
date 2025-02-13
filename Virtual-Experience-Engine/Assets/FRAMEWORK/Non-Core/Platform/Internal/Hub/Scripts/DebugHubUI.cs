using TMPro;
using UnityEngine;
using VE2.Core;
using VE2.NonCore.Platform.Private;
using VE2.PlatformNetworking;
using static VE2.Platform.Internal.PlatformSerializables;

public class DebugHubUI : MonoBehaviour
{
    [SerializeField] private TMP_Text instancesListText;
    private PlatformService _platformService;

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

        foreach (WorldDetails worldDetails in _platformService.ActiveWorlds.Values)
            availableWorldList += $"{worldDetails.Name} - <i><color=yellow>{_platformService.GetInstanceServerSettingsForWorld(worldDetails.Name)}</color></i>\n";

        instancesListText.text = availableWorldList;
    }

    public void OnGoToDevButtonPressed()
    {
        if (_platformService == null)
        {
            Debug.LogError("Platform service not found");
            return;
        }

        _platformService.RequestInstanceAllocation("Dev", "dev");
    }

    public void OnGoToDev2ButtonPressed()
    {
        if (_platformService == null)
        {
            Debug.LogError("Platform service not found");
            return;
        }

        _platformService.RequestInstanceAllocation("Dev", "dev2");
    }

    private void OnDisable()
    {
        if (_platformService != null)
            _platformService.OnConnectedToServer -= HandlePlatformReady;
    }
}
