
using UnityEngine;
using VE2.Core.Common;
using VE2.Core.Player.API;
using static VE2.Core.Common.CommonSerializables;
using static VE2.Core.Player.API.PlayerSerializables;
using static VE2.NonCore.Instancing.Internal.InstanceSyncSerializables;

namespace VE2.NonCore.Instancing.Internal
{
    internal class LocalPlayerSyncer 
    {
        private readonly IPlayerServiceInternal _playerServiceInternal;
        private readonly IPluginSyncCommsHandler _commsHandler;
        private readonly InstanceInfoContainer _instanceInfoContainer;

        private int _cycleNumber = 0;

        public LocalPlayerSyncer(IPluginSyncCommsHandler commsHandler, IPlayerServiceInternal playerServiceInternal, InstanceInfoContainer instanceInfoContainer)
        {
            _commsHandler = commsHandler;

            _playerServiceInternal = playerServiceInternal;
            _playerServiceInternal.OnOverridableAvatarAppearanceChanged += HandleLocalAppearanceChanged;

            _instanceInfoContainer = instanceInfoContainer;
        }

        private void HandleLocalAppearanceChanged(OverridableAvatarAppearance appearance)
        {
            AvatarAppearanceWrapper avatarAppearanceWrapper = new(appearance != null, appearance);
            _commsHandler.SendMessage(avatarAppearanceWrapper.Bytes, InstanceNetworkingMessageCodes.UpdateAvatarPresentation, TransmissionProtocol.TCP);
        }

        public void NetworkUpdate() //TODO: Don't send state if not using framework avatar
        {
            _cycleNumber++;

            bool onTransmissionFrame = _cycleNumber % (int)(50 / _playerServiceInternal.TransmissionFrequency) == 0;
            if (onTransmissionFrame)
            {
                PlayerTransformData playerState = _playerServiceInternal.PlayerTransformData; //Doesn't include appearance
                PlayerStateWrapper playerStateWrapper = new(_instanceInfoContainer.LocalClientID, playerState.Bytes);

                _commsHandler.SendMessage(playerStateWrapper.Bytes, InstanceNetworkingMessageCodes.PlayerState, _playerServiceInternal.TransmissionProtocol);
            }
        }

        public void TearDown() 
        {
            if (_playerServiceInternal != null)
                _playerServiceInternal.OnOverridableAvatarAppearanceChanged -= HandleLocalAppearanceChanged;
        }
    }
}
