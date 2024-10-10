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
            _instanceService.OnDisconnectedFromServer += HandleDisconnectFromServer;
        }
        else
        {
            globalInfoText.text = "No instance service provider found";
        }
    }

    private void HandleInstanceInfoChanged(InstancedInstanceInfo instanceInfo)
    {
        string instanceInfoString = $"<b>INSTANCE</b> {instanceInfo.InstanceCode} \nLocal ID = <color=green>{_instanceService.LocalClientID}</color>\n";

        //Debug.Log("NUM PLAYERS IN ISNTANCE = " + instanceInfo.ClientInfos.Values.Count + "=============");
        foreach (InstancedClientInfo clientInfo in instanceInfo.ClientInfos.Values)
        {
            if (clientInfo.ClientID.Equals(_instanceService.LocalClientID))
                instanceInfoString += $"<color=green>";

            instanceInfoString += $"{clientInfo.ClientID}";
            if (clientInfo.InstancedAvatarAppearance.UsingViRSEAvatar)
                instanceInfoString += $"({ clientInfo.InstancedAvatarAppearance.PlayerPresentationConfig.PlayerName}): ";
            else
                instanceInfoString += $"(Name N/A): "; 
            instanceInfoString += $"Host = { clientInfo.ClientID.Equals(instanceInfo.HostID).ToString()}\n";


            if (clientInfo.ClientID.Equals(_instanceService.LocalClientID))
                instanceInfoString += $"</color>";
        }

        globalInfoText.text = instanceInfoString;
    }   

    private void HandleDisconnectFromServer()
    {
        globalInfoText.text = "Disconnected";
    }

    private void OnDisable()
    {
        if (_instanceService != null)
        {
            if (_instanceService.IsConnectedToServer)
            {
                _instanceService.OnInstanceInfoChanged -= HandleInstanceInfoChanged;
                _instanceService.OnDisconnectedFromServer -= HandleDisconnectFromServer;
            }
        }
    }
}

/*

    Name is currently part of the appearance, that means if there IS no appearance, there is no name 
    The player spawner should really just show the overrides, right?
    Maybe "overrides" is the wrong term to use here 
    It's something that's needed alongside the user settings
    What does it actually DO?
    It bolts on to the UserSettings to provide the additional settings needed for appearance 
    They're the Scene-Specific appearance settings
    The DynamicAppearanceSettings? 

    If we're on platform, there will ALWAYS be a UserSettingsProvider, so there will always be a name 
    We only hide the player in the instance if the ""overrides"" are null 

*/
