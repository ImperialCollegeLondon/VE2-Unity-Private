using TMPro;
using UnityEngine;
using ViRSE.InstanceNetworking;
using ViRSE.PluginRuntime;
using static PlatformSerializables;

public class DebugGlobalInfoUI : MonoBehaviour
{
    [SerializeField] private TMP_Text globalInfoText;

    private PlatformService _platformService;

    void OnEnable()
    {
        PlatformServiceProvider provider = FindFirstObjectByType<PlatformServiceProvider>();
        if (provider != null)
        {
            _platformService = (PlatformService)provider.PlatformService;

            //If we're already connected to the server, display initial global info rather than waiting for an update
            if (_platformService.IsConnectedToServer)
                HandleGlobalInfoChanged(_platformService.GlobalInfo);

            _platformService.OnGlobalInfoChanged += HandleGlobalInfoChanged;
        }
        else
        {
            globalInfoText.text = "No platform service provider found";
        }
    }

    private void HandleGlobalInfoChanged(GlobalInfo globalInfo)
    {
        string globalInfoString = $"<b>PLATFORM</b> \nLocal ID = {_platformService.LocalClientID}\n";

        foreach (PlatformInstanceInfo platformInstanceInfo in globalInfo.InstanceInfos.Values)
        {
            globalInfoString += $"{platformInstanceInfo.InstanceCode}_____";
            foreach (PlatformClientInfo clientInfo in platformInstanceInfo.ClientInfos.Values)
            {
                if (clientInfo.ClientID.Equals(_platformService.LocalClientID))
                    globalInfoString += $"<color=green>";

                globalInfoString += $"\n   {clientInfo.ClientID}";
                globalInfoString += $"({clientInfo.PlayerPresentationConfig.PlayerName}): ";

                if (clientInfo.ClientID.Equals(_platformService.LocalClientID))
                    globalInfoString += $"</color>";
            }
            globalInfoString += "\n";
        }

        globalInfoText.text = globalInfoString;
    }   

    private void OnDisable()
    {
        if (_platformService != null)
            _platformService.OnGlobalInfoChanged -= HandleGlobalInfoChanged;
    }
}
