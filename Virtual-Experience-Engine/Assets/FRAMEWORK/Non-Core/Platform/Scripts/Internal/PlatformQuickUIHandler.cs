using UnityEngine;
using VE2.Common.API;
using VE2.NonCore.Instancing.API;
using VE2.NonCore.Platform.API;

namespace VE2.NonCore.Platform.Internal
{
    internal class PlatformQuickUIHandler
    {
        private float _lastPingUpdateTime = -1;
        private const float PING_UPDATE_INTERVAL = 1f;

        private readonly V_PlatformQuickUIView _quickUIView;
        private readonly IPlatformServiceInternal _platformService;
        private readonly IInstanceService _instanceService;

        internal PlatformQuickUIHandler(V_PlatformQuickUIView quickUIView)
        {
            _quickUIView = quickUIView;

            //These are done here, called by awake from the view, so we only fetch these services once they are ready
            _platformService = PlatformAPI.PlatformService as IPlatformServiceInternal;
            _instanceService = VE2API.InstanceService;

            _quickUIView.SetPlayerNameText(_platformService.PlayerDisplayName);
            _quickUIView.SetPingTextMS(0);

            if (_instanceService != null)
            {
                _instanceService.OnConnectedToInstance += HandlePlatformConnected;
                _instanceService.OnDisconnectedFromInstance += HandlePlatformDisconnected;
                _instanceService.OnBecomeHost += HandleBecomeHost;
                _instanceService.OnLoseHost += HandleBecomeNonHost;

                _quickUIView.SetConnectedText(_instanceService.IsConnectedToServer);

                if (_instanceService.IsHost)
                    _quickUIView.SetHost();
                else
                    _quickUIView.SetNonHost();
            }
            else 
            {
                _quickUIView.SetHostNA();
                _quickUIView.SetConnectionNA();
            }

        }

        internal void HandleUpdate() 
        {
            if (Time.time - _lastPingUpdateTime < PING_UPDATE_INTERVAL)
                return;

            _lastPingUpdateTime = Time.time;

            if (_instanceService != null && _instanceService.IsConnectedToServer)
            {
                if (_instanceService.IsHost)
                    _quickUIView.SetPingTextNA();
                else
                    _quickUIView.SetPingTextMS(_instanceService.SmoothPing);
            }
            else
                _quickUIView.SetPingTextNA();
        }

        private void HandlePlatformConnected() => _quickUIView.SetConnectedText(true);
        private void HandlePlatformDisconnected() 
        {
            _quickUIView.SetConnectedText(false);
            _quickUIView.SetHostNA();
        }

        private void HandleBecomeHost() => _quickUIView.SetHost();
        private void HandleBecomeNonHost() => _quickUIView.SetNonHost();
    }
}
