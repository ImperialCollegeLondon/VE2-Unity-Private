using TMPro;
using UnityEngine;
using VE2.NonCore.Instancing.API;
using VE2.Core.Common;
using UnityEngine.UI;

namespace VE2.NonCore.Instancing.Internal
{
    internal class DebugInstanceInfoUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text hostIndicatorText;

        private IInstanceService _instanceService;
        private ColorConfiguration _colorConfig;

        void OnEnable()
        {
            _instanceService = InstancingAPI.InstanceService;
            _colorConfig = Resources.Load<ColorConfiguration>("ColorConfiguration"); //TODO: think of a centralised place to fetch this from

            _instanceService.OnConnectedToInstance += HandleConnectToServer;
            _instanceService.OnDisconnectedFromInstance += HandleDisconnectFromServer;
            _instanceService.OnLoseHost += HandleLoseHost;
            _instanceService.OnBecomeHost += HandleBecomeHost;

            if (_instanceService.IsConnectedToServer)
                HandleConnectToServer();
            else
                HandleDisconnectFromServer();
        }

        private void HandleConnectToServer()
        {
            if (_instanceService.IsHost)
                HandleBecomeHost();
            else
                HandleLoseHost();
        }

        private void HandleDisconnectFromServer()
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
            _instanceService.OnConnectedToInstance -= HandleConnectToServer;
            _instanceService.OnDisconnectedFromInstance -= HandleDisconnectFromServer;
            _instanceService.OnLoseHost -= HandleLoseHost;
            _instanceService.OnBecomeHost -= HandleBecomeHost;
        }
    }
}
