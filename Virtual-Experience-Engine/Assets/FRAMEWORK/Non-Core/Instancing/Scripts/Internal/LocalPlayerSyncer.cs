using static InstanceSyncSerializables;
using VE2.Common;
using static VE2.Common.CommonSerializables;
using System;
using UnityEngine;
using static VE2.InstanceNetworking.V_InstanceIntegration;
namespace VE2.InstanceNetworking
{
    internal class LocalPlayerSyncer : ILocalClientIDProvider
    {
        #region SyncService interfaces
        public ushort LocalClientID => _localClientIDWrapper.LocalClientID;
        public event Action<ushort> OnClientIDReady
        {
            add => _localClientIDWrapper.OnLocalClientIDSet += value;
            remove => _localClientIDWrapper.OnLocalClientIDSet -= value;
        }
        #endregion  

        // public event Action<BytesAndProtocol> OnPlayerStateUpdatedLocally;
        // public event Action<BytesAndProtocol> OnAvatarAppearanceUpdatedLocally;
        //private IPlayerStateModule _playerStateModule => _localPlayerContainer.PlayerStateModule;


        private readonly IPlayerServiceInternal _playerServiceInternal;
        private readonly IPluginSyncCommsHandler _commsHandler;
        private readonly LocalClientIdWrapper _localClientIDWrapper;
        //private readonly PlayerStateModuleContainer _localPlayerContainer; //TODO: Remove
        private readonly InstanceInfoContainer _instanceInfoContainer;

        private int _cycleNumber = 0;

        /*
            Tbf, they'll only ever be 1 player, so the player doesn't even really need to register with the syncer 
            Best to do it anyway for consistency, player needs syncer at edit time anyway

            Maybe what we DO do is pass the entire PlayerService? We need to listen to its appearance events...
            Maybe its fine for that to live in the state module though? 
        */

        public LocalPlayerSyncer(IPluginSyncCommsHandler commsHandler, LocalClientIdWrapper localClientIDWrapper, IPlayerServiceInternal playerServiceInternal, InstanceInfoContainer instanceInfoContainer)
        {
            _commsHandler = commsHandler;
            _localClientIDWrapper = localClientIDWrapper;
            _playerServiceInternal = playerServiceInternal;

            //TODO: not sure I like that we're injecting the PlayerService in here
            //Should the playerservice directly invoke PlayerSyncService.MarkOverridableAvatarAppearanceChanged?
            //Unless the instancing might need to be able to talk to the player somehow? Don't think so though...
            _playerServiceInternal.OnOverridableAvatarAppearanceChanged += HandleLocalAppearanceChanged;

            _instanceInfoContainer = instanceInfoContainer;
        }

        private void HandleLocalAppearanceChanged(OverridableAvatarAppearance appearance)
        {
            AvatarAppearanceWrapper avatarAppearanceWrapper = new(appearance != null, appearance);
            _commsHandler.SendMessage(avatarAppearanceWrapper.Bytes, InstanceNetworkingMessageCodes.UpdateAvatarPresentation, TransmissionProtocol.TCP);
            //OnAvatarAppearanceUpdatedLocally?.Invoke(new BytesAndProtocol(avatarAppearanceWrapper.Bytes, TransmissionProtocol.TCP));
        }

        public void NetworkUpdate() 
        {
            // if (_playerStateModule == null)
            //     return;

            _cycleNumber++;

            bool onTransmissionFrame = _cycleNumber % (int)(50 / _playerServiceInternal.TransmissionFrequency) == 0;
            if (onTransmissionFrame)
            {
                VE2Serializable playerState = _playerServiceInternal.PlayerTransformData; //Doesn't include appearance
                PlayerStateWrapper playerStateWrapper = new(_instanceInfoContainer.LocalClientID, playerState.Bytes);
                _commsHandler.SendMessage(playerStateWrapper.Bytes, InstanceNetworkingMessageCodes.PlayerState, _playerServiceInternal.TransmissionProtocol);
                //OnPlayerStateUpdatedLocally?.Invoke(new BytesAndProtocol(playerStateWrapper.Bytes, _playerStateModule.TransmissionProtocol));
            }
        }

        public void TearDown() 
        {
            // _localPlayerContainer.OnPlayerStateModuleRegistered -= HandlePlayerStateModuleRegistered;
            // _localPlayerContainer.OnPlayerStateModuleDeregistered -= HandlePlayerStateModuleDeregistered;
            
            // if (_playerStateModule != null)
            //     _playerStateModule.OnAvatarAppearanceChanged -= HandleLocalAppearanceChanged;
        }
    }
}
