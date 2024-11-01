using TMPro;
using UnityEngine;
using ViRSE.Core;
using ViRSE.NonCore.Platform.Private;
using ViRSE.PlatformNetworking;
using static ViRSE.PlatformNetworking.PlatformSerializables;

public class DebugHubUI : MonoBehaviour
{
    [SerializeField] private TMP_Text instancesListText;
    private PlatformService _platformService;

    void OnEnable()
    {
        _platformService = (PlatformService)FindFirstObjectByType<PlatformServiceProvider>().PlatformService;

        if (_platformService.IsConnectedToServer)
        {
            HandlePlatformReady();
        }
        else
        {
            _platformService.OnConnectedToServer += HandlePlatformReady;
        }
    }

    private void HandlePlatformReady()
    {
        string availableWorldList = "";

        foreach (WorldDetails worldDetails in _platformService.AvailableWorlds.Values)
        {
            availableWorldList += $"{worldDetails.Name} - <i><color=yellow>{worldDetails.IPAddress}:{worldDetails.PortNumber}</color></i>\n";
        }

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
        _platformService.OnConnectedToServer -= HandlePlatformReady;
    }
}
