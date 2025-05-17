
using UnityEngine;
using VE2.Common.API;
using VE2.Common.Shared;
using VE2.Core.Player.API;
using static VE2.Core.Player.API.PlayerSerializables;
using static VE2.NonCore.Instancing.Internal.InstanceSyncSerializables;

namespace VE2.NonCore.Instancing.Internal
{
    internal class LocalPlayerSyncer 
    {
        private IPlayerServiceInternal _playerSyncable => _localPlayerSyncableContainer.LocalPlayerSyncable;

        private readonly IPluginSyncCommsHandler _commsHandler;
        private readonly InstanceInfoContainer _instanceInfoContainer;
        private readonly ILocalPlayerSyncableContainer _localPlayerSyncableContainer;

        private int _cycleNumber = 0;

        public LocalPlayerSyncer(IPluginSyncCommsHandler commsHandler, InstanceInfoContainer instanceInfoContainer, ILocalPlayerSyncableContainer localPlayerSyncableContainer)
        {
            _commsHandler = commsHandler;
            _instanceInfoContainer = instanceInfoContainer;
            _localPlayerSyncableContainer = localPlayerSyncableContainer;

            _localPlayerSyncableContainer.OnPlayerRegistered += HandleLocalPlayerRegistered;
            _localPlayerSyncableContainer.OnPlayerDeregistered += HandleLocalPlayerDeregistered;

            if (_localPlayerSyncableContainer.LocalPlayerSyncable != null)
                HandleLocalPlayerRegistered(_localPlayerSyncableContainer.LocalPlayerSyncable);
        }

        private void HandleLocalPlayerRegistered(IPlayerServiceInternal playerServiceInternal)
        {
            playerServiceInternal.OnOverridableAvatarAppearanceChanged += HandleLocalAppearanceChanged;
        }

        private void HandleLocalPlayerDeregistered(IPlayerServiceInternal playerServiceInternal)
        {
            if (playerServiceInternal == null) //Null if deregistrations happen when leaving play mode
                return;

            playerServiceInternal.OnOverridableAvatarAppearanceChanged -= HandleLocalAppearanceChanged;
        }

        private void HandleLocalAppearanceChanged(OverridableAvatarAppearance appearance)
        {
            AvatarAppearanceWrapper avatarAppearanceWrapper = new(appearance != null, appearance);
            _commsHandler.SendMessage(avatarAppearanceWrapper.Bytes, InstanceNetworkingMessageCodes.UpdateAvatarPresentation, TransmissionProtocol.TCP);
        }

        public void NetworkUpdate() 
        {
            if (_playerSyncable == null)
                return;

            _cycleNumber++;

            bool onTransmissionFrame = _cycleNumber % (int)(50 / _playerSyncable.TransmissionFrequency) == 0;
            if (onTransmissionFrame)
            {
                PlayerStateWrapper playerStateWrapper = new(_instanceInfoContainer.LocalClientID, _playerSyncable.PlayerTransformData.Bytes);

                _commsHandler.SendMessage(playerStateWrapper.Bytes, InstanceNetworkingMessageCodes.PlayerState, _playerSyncable.TransmissionProtocol);
            }
        }

        public void TearDown() 
        {
            if (_playerSyncable != null)
                _playerSyncable.OnOverridableAvatarAppearanceChanged -= HandleLocalAppearanceChanged;
        }
    }
}
