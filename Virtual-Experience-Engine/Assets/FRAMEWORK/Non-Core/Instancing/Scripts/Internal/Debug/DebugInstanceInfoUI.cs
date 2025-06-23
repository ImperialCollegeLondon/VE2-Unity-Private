using TMPro;
using UnityEngine;
using VE2.NonCore.Instancing.API;
using VE2.Common.Shared;
using VE2.Common.API;

namespace VE2.NonCore.Instancing.Internal
{
    [AddComponentMenu("")] // Prevents this MonoBehaviour from showing in the Add Component menu
    internal class DebugInstanceInfoUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text hostIndicatorText;

        private IInstanceService _instanceService;
        private ColorConfiguration _colorConfig => ColorConfiguration.Instance;

        void OnEnable()
        {
            _instanceService = VE2API.InstanceService;

            _instanceService.OnConnectedToInstance.AddListener(HandleConnectToServer);
            _instanceService.OnDisconnectedFromInstance.AddListener(HandleDisconnectFromServer);
            _instanceService.OnBecomeNonHost.AddListener(HandleLoseHost);
            _instanceService.OnBecomeHost.AddListener(HandleBecomeHost);

            if (_instanceService.IsConnectedToServer)
                HandleConnectToServer(_instanceService.LocalClientID);
            else
                HandleDisconnectFromServer(_instanceService.LocalClientID);
        }

        private void HandleConnectToServer(ushort localID)
        {
            if (_instanceService.IsHost)
                HandleBecomeHost();
            else
                HandleLoseHost();
        }

        private void HandleDisconnectFromServer(ushort localID)
        {
            //Debug.Log("handle Disconnect from Server DEBUG UI");
            hostIndicatorText.text = "Not Connected";
            hostIndicatorText.color = Color.red;
        }

        private void HandleBecomeHost()
        {
            //Debug.Log("handle become Host");
            hostIndicatorText.text = "Host";
            hostIndicatorText.color = _colorConfig.AccentPrimaryColor;
        }

        private void HandleLoseHost() 
        {
            //Debug.Log("handle Lose Host");
            hostIndicatorText.text = "Non-Host";
            hostIndicatorText.color = _colorConfig.AccentSecondaryColor;
        }

        private void OnDisable()
        {
            _instanceService.OnConnectedToInstance.RemoveListener(HandleConnectToServer);
            _instanceService.OnDisconnectedFromInstance.RemoveListener(HandleDisconnectFromServer);
            _instanceService.OnBecomeNonHost.RemoveListener(HandleLoseHost);
            _instanceService.OnBecomeHost.RemoveListener(HandleBecomeHost);
        }
    }
}
