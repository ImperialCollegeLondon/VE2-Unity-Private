using System.Collections.Generic;
using UnityEngine;
using ViRSE.Core.Player;
using ViRSE.Core.Shared;
using ViRSE.Core;
using static InstanceSyncSerializables;

namespace ViRSE.InstanceNetworking
{
    public static class LocalPlayerSyncerFactory 
    {
        public static LocalPlayerSyncer Create(InstanceService instanceService)
        {
            return new LocalPlayerSyncer(instanceService, ViRSECoreServiceLocator.Instance.ViRSEPlayerContainer);
        }
    }

    public class LocalPlayerSyncer 
    {
        private int _cycleNumber = 0;
        private readonly InstanceService _instanceService;
        private readonly ViRSEPlayerStateModuleContainer _virsePlayerContainer;
        private IViRSEPlayerRig _localPlayerRig;
        private AvatarAppearanceWrapper InstancedPlayerPresentation
        {
            get
            {
                bool usingViRSEAvatar = _virsePlayerContainer.LocalPlayerRig != null && _virsePlayerContainer.LocalPlayerRig.IsNetworked;
                if (usingViRSEAvatar)
                    return new AvatarAppearanceWrapper(true, _virsePlayerContainer.LocalPlayerRig.AvatarAppearance);
                else 
                    return new AvatarAppearanceWrapper(false, null);
            }
        }

        public LocalPlayerSyncer(InstanceService instanceSevice, ViRSEPlayerStateModuleContainer virsePlayerContainer)
        {
            _instanceService = instanceSevice;

            _virsePlayerContainer = virsePlayerContainer;
            _virsePlayerContainer.OnLocalPlayerRigRegistered += RegisterLocalPlayer;
            _virsePlayerContainer.OnLocalPlayerRigDeregistered += DeregisterLocalPlayer;

            if (_virsePlayerContainer.LocalPlayerRig != null)
                RegisterLocalPlayer();
        }

        private void HandleLocalAppearanceChanged()
        {
            _instanceService.SendAvatarAppearanceUpdate(InstancedPlayerPresentation.Bytes);
        }

        public void RegisterLocalPlayer(IViRSEPlayerRig playerRig)
        {
            _localPlayerRig = playerRig;
            _localPlayerRig.OnAppearanceChanged += HandleLocalAppearanceChanged;
            HandleLocalAppearanceChanged();
        }

        public void DeregisterLocalPlayer(IViRSEPlayerRig playerRig)
        {
            playerRig.OnAppearanceChanged -= HandleLocalAppearanceChanged;
            _localPlayerRig = null;
            HandleLocalAppearanceChanged();
        }

        public void NetworkUpdate() 
        {
            if (_localPlayerRig == null)
                return;

            _cycleNumber++;

            bool onTransmissionFrame = _cycleNumber % (int)(50 / _localPlayerRig.TransmissionFrequency) == 0;
            if (onTransmissionFrame)
            {
                PlayerState playerState = new(_localPlayerRig.RootPosition, _localPlayerRig.RootRotation, _localPlayerRig.HeadPosition, _localPlayerRig.HeadRotation);
                PlayerStateWrapper playerStateWrapper = new(_instanceService.LocalClientID, playerState.Bytes);

                _instanceService.SendPlayerState(playerStateWrapper.Bytes, _localPlayerRig.TransmissionProtocol);
            }
        }

        public void TearDown() 
        {

            _virsePlayerContainer.OnPlayerStateModuleRegistered -= RegisterLocalPlayer;
            _virsePlayerContainer.OnPlayerStateModuleDeregistered -= DeregisterLocalPlayer;
            

            if (_localPlayerRig != null)
                _localPlayerRig.OnAppearanceChanged -= HandleLocalAppearanceChanged;
        }
    }
}
