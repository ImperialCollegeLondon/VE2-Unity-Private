using static InstanceSyncSerializables;
using VE2.Common;
using static VE2.Common.CommonSerializables;
using System;
using UnityEngine;
namespace VE2.InstanceNetworking
{
    internal class LocalPlayerSyncer 
    {
        public event Action<BytesAndProtocol> OnPlayerStateUpdatedLocally;
        public event Action<BytesAndProtocol> OnAvatarAppearanceUpdatedLocally;

        private readonly PlayerStateModuleContainer _localPlayerContainer;
        private readonly InstanceInfoContainer _instanceInfoContainer;

        private int _cycleNumber = 0;
        private IPlayerStateModule _playerStateModule => _localPlayerContainer.PlayerStateModule;

        public LocalPlayerSyncer(PlayerStateModuleContainer localPlayerContainer, InstanceInfoContainer instanceInfoContainer)
        {
            _localPlayerContainer = localPlayerContainer;
            _localPlayerContainer.OnPlayerStateModuleRegistered += HandlePlayerStateModuleRegistered;
            _instanceInfoContainer = instanceInfoContainer;

            _localPlayerContainer.OnPlayerStateModuleDeregistered += HandlePlayerStateModuleDeregistered;
        }

        //TODO: do this in constructor once we wire in the comms handler interface
        public void TempDelayedPlayerReg() 
        {
            if (_localPlayerContainer.PlayerStateModule != null)
                HandlePlayerStateModuleRegistered(_localPlayerContainer.PlayerStateModule);
        }

        public void HandlePlayerStateModuleRegistered(IPlayerStateModule stateModule)
        {
            // stateModule.OnAvatarAppearanceChanged += HandleLocalAppearanceChanged;
            // HandleLocalAppearanceChanged(_localPlayerContainer.PlayerStateModule.AvatarAppearance);
        }

        public void HandlePlayerStateModuleDeregistered(IPlayerStateModule stateModule)
        {
            // stateModule.OnAvatarAppearanceChanged -= HandleLocalAppearanceChanged;
            // HandleLocalAppearanceChanged(null);
        }

        private void HandleLocalAppearanceChanged(AvatarAppearance appearance)
        {
            AvatarAppearanceWrapper avatarAppearanceWrapper = new(appearance != null, appearance);
            OnAvatarAppearanceUpdatedLocally?.Invoke(new BytesAndProtocol(avatarAppearanceWrapper.Bytes, TransmissionProtocol.TCP));
        }

        public void NetworkUpdate() 
        {
            if (_playerStateModule == null)
                return;

            _cycleNumber++;

            bool onTransmissionFrame = _cycleNumber % (int)(50 / _playerStateModule.TransmissionFrequency) == 0;
            if (onTransmissionFrame)
            {
                VE2Serializable playerState = _playerStateModule.State; //Doesn't include appearance
                PlayerStateWrapper playerStateWrapper = new(_instanceInfoContainer.LocalClientID, playerState.Bytes);
                OnPlayerStateUpdatedLocally?.Invoke(new BytesAndProtocol(playerStateWrapper.Bytes, _playerStateModule.TransmissionProtocol));
            }
        }

        public void TearDown() 
        {
            _localPlayerContainer.OnPlayerStateModuleRegistered -= HandlePlayerStateModuleRegistered;
            _localPlayerContainer.OnPlayerStateModuleDeregistered -= HandlePlayerStateModuleDeregistered;
            
            // if (_playerStateModule != null)
            //     _playerStateModule.OnAvatarAppearanceChanged -= HandleLocalAppearanceChanged;
        }
    }
}
