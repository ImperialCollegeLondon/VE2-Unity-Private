using TMPro;
using UnityEngine;
using VE2.InstanceNetworking;
using VE2.Core;
using static InstanceSyncSerializables;
using System.Collections;

public class DebugInstanceInfoUI : MonoBehaviour
{
    [SerializeField] private TMP_Text globalInfoText;

    private InstanceService instanceService;

    void OnEnable()
    {
        StartCoroutine(DelayedOnEnable());
    }

    private IEnumerator DelayedOnEnable()
    {
        yield return new WaitForSeconds(0.1f);

        instanceService = (InstanceService)FindFirstObjectByType<V_InstanceIntegration>().InstanceService;
        if (instanceService != null)
        {
            //If we're already connected to the server, display initial global info rather than waiting for an update
            if (instanceService.IsConnectedToServer)
                HandleInstanceInfoChanged(instanceService.InstanceInfo);

            instanceService.OnInstanceInfoChanged += HandleInstanceInfoChanged;
            instanceService.OnDisconnectedFromInstance += HandleDisconnectFromServer;
        }
        else
        {
            globalInfoText.text = "No instance service provider found";
        }
    }

    private void HandleInstanceInfoChanged(InstancedInstanceInfo instanceInfo)
    {
        string instanceInfoString = $"<b>INSTANCE</b> {instanceInfo.InstanceCode} \nLocal ID = <color=green>{instanceService.LocalClientID}</color>\n";

        //Debug.Log("NUM PLAYERS IN ISNTANCE = " + instanceInfo.ClientInfos.Values.Count + "=============");
        foreach (InstancedClientInfo clientInfo in instanceInfo.ClientInfos.Values)
        {
            if (clientInfo.ClientID.Equals(instanceService.LocalClientID))
                instanceInfoString += $"<color=green>";

            instanceInfoString += $"{clientInfo.ClientID}";
            if (clientInfo.AvatarAppearanceWrapper.UsingViRSEPlayer)
                instanceInfoString += $"({ clientInfo.AvatarAppearanceWrapper.ViRSEAvatarAppearance.PresentationConfig.PlayerName}): ";
            else
                instanceInfoString += $"(Name N/A): "; 
            instanceInfoString += $"Host = { clientInfo.ClientID.Equals(instanceInfo.HostID).ToString()}\n";


            if (clientInfo.ClientID.Equals(instanceService.LocalClientID))
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
        if (instanceService != null)
        {
            if (instanceService.IsConnectedToServer)
            {
                instanceService.OnInstanceInfoChanged -= HandleInstanceInfoChanged;
                instanceService.OnDisconnectedFromInstance -= HandleDisconnectFromServer;
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
