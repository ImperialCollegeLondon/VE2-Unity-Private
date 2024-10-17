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
            return new LocalPlayerSyncer(instanceService, ViRSECoreServiceLocator.Instance);
        }
    }

    public class LocalPlayerSyncer 
    {
        private ILocalPlayerRig _localPlayerRig;
        private int _cycleNumber = 0;
        private InstanceService _instanceService;
        private ViRSECoreServiceLocator _coreServiceLocator;
        private ViRSEAvatarAppearanceWrapper _instancedPlayerPresentation
        {
            get
            {
                bool usingViRSEAvatar = ViRSECoreServiceLocator.Instance.LocalPlayerRig != null && ViRSECoreServiceLocator.Instance.LocalPlayerRig.IsNetworked;
                if (usingViRSEAvatar)
                    return new ViRSEAvatarAppearanceWrapper(true, ViRSECoreServiceLocator.Instance.LocalPlayerRig.AvatarAppearance);
                else 
                    return new ViRSEAvatarAppearanceWrapper(false, null);
            }
        }

        public LocalPlayerSyncer(InstanceService instanceSevice, ViRSECoreServiceLocator coreServiceLocator)
        {
            _instanceService = instanceSevice;
            _coreServiceLocator = coreServiceLocator;

            _coreServiceLocator.OnLocalPlayerRigRegistered += RegisterLocalPlayer;
            _coreServiceLocator.OnLocalPlayerRigDeregistered += DeregisterLocalPlayer;

            if (_coreServiceLocator.LocalPlayerRig != null)
                RegisterLocalPlayer(_coreServiceLocator.LocalPlayerRig);
        }

        private void HandleLocalAppearanceChanged()
        {
            _instanceService.SendAvatarAppearanceUpdate(_instancedPlayerPresentation.Bytes);
        }

        public void RegisterLocalPlayer(ILocalPlayerRig localPlayerRig)
        {
            _localPlayerRig = localPlayerRig;
            _localPlayerRig.OnAppearanceChanged += HandleLocalAppearanceChanged;
            HandleLocalAppearanceChanged();
        }

        public void DeregisterLocalPlayer(ILocalPlayerRig localPlayerRig)
        {
            _localPlayerRig = null;
            if (localPlayerRig != null)
                localPlayerRig.OnAppearanceChanged -= HandleLocalAppearanceChanged;
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
            if (_coreServiceLocator != null)
            {
                _coreServiceLocator.OnLocalPlayerRigRegistered -= RegisterLocalPlayer;
                _coreServiceLocator.OnLocalPlayerRigDeregistered -= DeregisterLocalPlayer;
            }

            if (_localPlayerRig != null)
                _localPlayerRig.OnAppearanceChanged -= HandleLocalAppearanceChanged;
        }
    }
}
