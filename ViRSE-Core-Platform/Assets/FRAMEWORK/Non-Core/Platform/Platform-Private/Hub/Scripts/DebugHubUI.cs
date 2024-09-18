using TMPro;
using UnityEngine;
using ViRSE.InstanceNetworking;
using ViRSE.PluginRuntime;
using static PlatformSerializables;

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
        string availableWorldList = "<b>AVAILABLE WORLDS</b>\n";

        foreach (WorldDetails worldDetails in _platformService.AvailableWorlds.Values)
        {
            availableWorldList += $"{worldDetails.Name} - {worldDetails.IPAddress}:{worldDetails.PortNumber}\n";
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

    /*
     *   How would this work for the main UI?
     *   mm, this is the trade off to not persisting things between scene
     *   We'll NEED to create this UI within the scene, and attach it 
     *   So, where does that UI come from? What instantiated it?
     *   It has to be something with in platform integration, really. 
     *   V_PlatformIntegration will have to instantiate it, either when 
     *   
     *   
     *   Where does the actual code for this UI live?
     *   If it lives in PlatformIntegration, then it gets shipped out to customers... that's not really what we want 
     *   But, if it lives directly in 
     */
}
