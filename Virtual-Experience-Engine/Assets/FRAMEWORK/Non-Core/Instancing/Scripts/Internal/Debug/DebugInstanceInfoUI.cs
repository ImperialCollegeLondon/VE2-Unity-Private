using TMPro;
using UnityEngine;
using System.Collections;
using static VE2.NonCore.Instancing.Internal.InstanceSyncSerializables;

namespace VE2.NonCore.Instancing.Internal
{
    internal class DebugInstanceInfoUI : MonoBehaviour
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

            V_InstanceIntegration instancingProvider = FindFirstObjectByType<V_InstanceIntegration>();
            if (instancingProvider != null)
            {
                instanceService = (InstanceService)instancingProvider.InstanceService;

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
            string instanceInfoString = $"<b>INSTANCE</b> {instanceInfo.FullInstanceCode} \nLocal ID = <color=green>{instanceService.LocalClientID}</color>\n";

            //Debug.Log("NUM PLAYERS IN ISNTANCE = " + instanceInfo.ClientInfos.Values.Count + "=============");
            foreach (InstancedClientInfo clientInfo in instanceInfo.ClientInfos.Values)
            {
                if (clientInfo.ClientID.Equals(instanceService.LocalClientID))
                    instanceInfoString += $"<color=green>";

                instanceInfoString += $"{clientInfo.ClientID}";
                if (clientInfo.InstancedAvatarAppearance.UsingFrameworkPlayer)
                    instanceInfoString += $"({ clientInfo.InstancedAvatarAppearance.OverridableAvatarAppearance.PresentationConfig.PlayerName}): ";
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
}
