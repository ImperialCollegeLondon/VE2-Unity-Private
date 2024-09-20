using TMPro;
using UnityEngine;
using ViRSE.InstanceNetworking;
using ViRSE.PluginRuntime;
using static InstanceSyncSerializables;

public class DebugInstanceInfoUI : MonoBehaviour
{
    [SerializeField] private TMP_Text globalInfoText;

    private PluginSyncService _instanceService;

    void OnEnable()
    {
        V_InstanceIntegration provider = FindFirstObjectByType<V_InstanceIntegration>();
        if (provider != null)
        {
            _instanceService = (PluginSyncService)provider.InstanceService;

            //If we're already connected to the server, display initial global info rather than waiting for an update
            if (_instanceService.IsConnectedToServer)
                HandleInstanceInfoChanged(_instanceService.InstanceInfo);

            _instanceService.OnInstanceInfoChanged += HandleInstanceInfoChanged;
        }
        else
        {
            globalInfoText.text = "No instance service provider found";
        }
    }

    private void HandleInstanceInfoChanged(InstancedInstanceInfo instanceInfo)
    {
        string instanceInfoString = $"<b>INSTANCE</b> {instanceInfo.InstanceCode} \nLocal ID = <color=green>{_instanceService.LocalClientID}</color>\n";

        Debug.Log("NUM PLAYERS IN ISNTANCE = " + instanceInfo.ClientInfos.Values.Count + "=============");
        foreach (InstancedClientInfo clientInfo in instanceInfo.ClientInfos.Values)
        {
            if (clientInfo.ClientID.Equals(_instanceService.LocalClientID))
                instanceInfoString += $"<color=green>";

            instanceInfoString += $"{clientInfo.ClientID} ({clientInfo.InstancedAvatarAppearance.PlayerName}): Host = {(clientInfo.ClientID.Equals(instanceInfo.HostID)).ToString()}\n";

            if (clientInfo.ClientID.Equals(_instanceService.LocalClientID))
                instanceInfoString += $"</color>";
        }

        globalInfoText.text = instanceInfoString;
    }   

    private void OnDisable()
    {
        if (_instanceService != null)
            _instanceService.OnInstanceInfoChanged -= HandleInstanceInfoChanged;
    }
}
