using System;
using TMPro;
using UnityEngine;
using VE2.NonCore.Platform.Private;
using VE2.PlatformNetworking;
using static VE2.PlatformNetworking.PlatformSerializables;

public class DebugGlobalInfoUI : MonoBehaviour
{
    [SerializeField] private TMP_Text globalInfoText;

    private V_PlatformIntegration _platformIntegration;

    void OnEnable()
    {
        //PlatformServiceProvider provider = FindFirstObjectByType<PlatformServiceProvider>();
        _platformIntegration = FindObjectOfType<V_PlatformIntegration>();
        if (_platformIntegration != null)
        {

            //If we're already connected to the server, display initial global info rather than waiting for an update
            if (_platformIntegration.IsConnectedToServer)
                HandleGlobalInfoChanged(_platformIntegration.GlobalInfo);

            _platformIntegration.OnGlobalInfoChanged += HandleGlobalInfoChanged;
        }
        else
        {
            globalInfoText.text = "No platform service provider found";
        }
    }

    private void HandleGlobalInfoChanged(GlobalInfo globalInfo)
    {
        string globalInfoString = $"<b>PLATFORM</b> \nLocal ID = {_platformIntegration.LocalClientID}\n";

        foreach (PlatformInstanceInfo platformInstanceInfo in globalInfo.InstanceInfos.Values)
        {
            globalInfoString += $"{platformInstanceInfo.InstanceCode}_____";
            foreach (PlatformClientInfo clientInfo in platformInstanceInfo.ClientInfos.Values)
            {
                if (clientInfo.ClientID.Equals(_platformIntegration.LocalClientID))
                    globalInfoString += $"<color=green>";

                globalInfoString += $"\n   {clientInfo.ClientID}";
                globalInfoString += $"({clientInfo.PlayerPresentationConfig.PlayerName}): ";

                if (clientInfo.ClientID.Equals(_platformIntegration.LocalClientID))
                    globalInfoString += $"</color>";
            }
            globalInfoString += "\n";
        }

        globalInfoText.text = globalInfoString;
    }

    private void OnDisable()
    {
        if (_platformIntegration != null)
            _platformIntegration.OnGlobalInfoChanged -= HandleGlobalInfoChanged;
    }
}
