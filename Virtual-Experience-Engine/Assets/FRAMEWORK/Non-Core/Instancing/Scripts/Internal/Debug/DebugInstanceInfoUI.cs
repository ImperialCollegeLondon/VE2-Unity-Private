using TMPro;
using UnityEngine;
using System.Collections;
using static VE2.NonCore.Instancing.Internal.InstanceSyncSerializables;

namespace VE2.NonCore.Instancing.Internal
{
    internal class DebugInstanceInfoUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text globalInfoText;
        [SerializeField] private TMP_Text pingText;

        private InstanceService _instanceService;

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
                _instanceService = (InstanceService)instancingProvider.InstanceService;

                //If we're already connected to the server, display initial global info rather than waiting for an update
                if (_instanceService.IsConnectedToServer)
                    HandleInstanceInfoChanged(_instanceService.InstanceInfo);

                _instanceService.OnInstanceInfoChanged += HandleInstanceInfoChanged;
                _instanceService.OnDisconnectedFromInstance += HandleDisconnectFromServer;
                _instanceService.OnPingUpdate += HandlePingUpdate;
            }
            else
            {
                globalInfoText.text = "No instance service provider found";
            }
        }

        private void HandleInstanceInfoChanged(InstancedInstanceInfo instanceInfo)
        {
            string instanceInfoString = $"<b>INSTANCE</b> {instanceInfo.FullInstanceCode} \nLocal ID = <color=green>{_instanceService.LocalClientID}</color>\n";

            //Debug.Log("NUM PLAYERS IN ISNTANCE = " + instanceInfo.ClientInfos.Values.Count + "=============");
            foreach (InstancedClientInfo clientInfo in instanceInfo.ClientInfos.Values)
            {
                if (clientInfo.ClientID.Equals(_instanceService.LocalClientID))
                    instanceInfoString += $"<color=green>";

                instanceInfoString += $"{clientInfo.ClientID}";
                if (clientInfo.InstancedAvatarAppearance.UsingFrameworkPlayer)
                    instanceInfoString += $"({ clientInfo.InstancedAvatarAppearance.OverridableAvatarAppearance.PresentationConfig.PlayerName}): ";
                else
                    instanceInfoString += $"(Name N/A): "; 
                instanceInfoString += $"Host = { clientInfo.ClientID.Equals(instanceInfo.HostID).ToString()}\n";


                if (clientInfo.ClientID.Equals(_instanceService.LocalClientID))
                    instanceInfoString += $"</color>";
            }

            globalInfoText.text = instanceInfoString;

            if (_instanceService.IsHost)
                pingText.text = $"Ping: 0ms (as host)";
        }   

        private void HandlePingUpdate (int smoothPing)
        {
            pingText.text = $"Ping: {smoothPing}ms";
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
                    _instanceService.OnDisconnectedFromInstance -= HandleDisconnectFromServer;
                    _instanceService.OnPingUpdate -= HandlePingUpdate;
                }
            }
        }
    }
}
