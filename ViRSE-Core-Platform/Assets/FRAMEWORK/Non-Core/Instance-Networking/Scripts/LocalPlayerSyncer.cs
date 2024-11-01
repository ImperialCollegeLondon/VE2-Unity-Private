using static InstanceSyncSerializables;
using ViRSE.Common;
using static ViRSE.Common.CoreCommonSerializables;

namespace ViRSE.InstanceNetworking
{
    public static class LocalPlayerSyncerFactory 
    {
        public static LocalPlayerSyncer Create(InstanceService instanceService)
        {
            return new LocalPlayerSyncer(instanceService, ViRSECoreServiceLocator.Instance.ViRSEPlayerStateModuleContainer);
        }
    }

    public class LocalPlayerSyncer 
    {
        private int _cycleNumber = 0;
        private readonly InstanceService _instanceService;
        private readonly ViRSEPlayerStateModuleContainer _virsePlayerContainer;
        private IPlayerStateModule _playerStateModule => _virsePlayerContainer.PlayerStateModule;

        public LocalPlayerSyncer(InstanceService instanceSevice, ViRSEPlayerStateModuleContainer virsePlayerContainer)
        {
            _instanceService = instanceSevice;

            _virsePlayerContainer = virsePlayerContainer;
            _virsePlayerContainer.OnPlayerStateModuleRegistered += HandlePlayerStateModuleRegistered;
            _virsePlayerContainer.OnPlayerStateModuleDeregistered += HandlePlayerStateModuleDeregistered;

            if (_virsePlayerContainer.PlayerStateModule != null)
                HandlePlayerStateModuleRegistered(_virsePlayerContainer.PlayerStateModule);
        }

        private void HandleLocalAppearanceChanged(ViRSEAvatarAppearance appearance)
        {
            AvatarAppearanceWrapper avatarAppearanceWrapper = new(appearance != null, appearance);
            _instanceService.SendAvatarAppearanceUpdate(avatarAppearanceWrapper.Bytes);
        }

        public void HandlePlayerStateModuleRegistered(IPlayerStateModule stateModule)
        {
            stateModule.OnAvatarAppearanceChanged += HandleLocalAppearanceChanged;
            HandleLocalAppearanceChanged(stateModule.AvatarAppearance);
        }

        public void HandlePlayerStateModuleDeregistered(IPlayerStateModule stateModule)
        {
            stateModule.OnAvatarAppearanceChanged -= HandleLocalAppearanceChanged;
            HandleLocalAppearanceChanged(null);
        }

        public void NetworkUpdate() 
        {
            if (_playerStateModule == null)
                return;

            _cycleNumber++;

            bool onTransmissionFrame = _cycleNumber % (int)(50 / _playerStateModule.TransmissionFrequency) == 0;
            if (onTransmissionFrame)
            {
                ViRSESerializable playerState = _playerStateModule.State; //Doesn't include appearance
                PlayerStateWrapper playerStateWrapper = new(_instanceService.LocalClientID, playerState.Bytes);

                _instanceService.SendPlayerState(playerStateWrapper.Bytes, _playerStateModule.TransmissionProtocol);
            }
        }

        public void TearDown() 
        {
            _virsePlayerContainer.OnPlayerStateModuleRegistered -= HandlePlayerStateModuleRegistered;
            _virsePlayerContainer.OnPlayerStateModuleDeregistered -= HandlePlayerStateModuleDeregistered;
            

            if (_playerStateModule != null)
                _playerStateModule.OnAvatarAppearanceChanged -= HandleLocalAppearanceChanged;
        }
    }
}
