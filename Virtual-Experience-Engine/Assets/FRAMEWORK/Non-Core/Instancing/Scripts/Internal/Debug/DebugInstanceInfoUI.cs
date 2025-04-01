using TMPro;
using UnityEngine;
using VE2.NonCore.Instancing.API;
using VE2.Core.Common;

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
            hostIndicatorText.text = "Not Connected";
            hostIndicatorText.color = Color.red;
            hostIndicatorText.rectTransform.ForceUpdateRectTransforms();
        }

        private void HandleBecomeHost()
        {
            hostIndicatorText.text = "Host";
            hostIndicatorText.color = _colorConfig.AccentPrimaryColor;
        }

        private void HandleLoseHost() 
        {
            Debug.Log("handle Lose Host");
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
