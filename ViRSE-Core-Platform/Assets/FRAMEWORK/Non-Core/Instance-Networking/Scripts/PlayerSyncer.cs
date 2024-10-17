using System.Collections.Generic;
using UnityEngine;
using ViRSE.Core.Player;
using ViRSE.Core.Shared;
using ViRSE.Core;
using static InstanceSyncSerializables;
using static ViRSE.Core.Shared.CoreCommonSerializables;

namespace ViRSE.InstanceNetworking
{
    public static class PlayerSyncerFactory 
    {
        public static PlayerSyncer Create(InstanceService instanceService)
        {
            List<GameObject> virseAvatarHeadGameObjects = new() 
            {
                Resources.Load<GameObject>("Avatars/Heads/ViRSE_Head_Default_1"),
                Resources.Load<GameObject>("Avatars/Heads/ViRSE_Head_Default_2"),
            };

            List<GameObject> virseAvatarTorsoGameObjects = new() 
            {
                Resources.Load<GameObject>("Avatars/Torsos/ViRSE_Torso_Default_1"),
            };

            return new PlayerSyncer(
                instanceService,
                ViRSECoreServiceLocator.Instance.PlayerAppearanceOverridesProvider,
                virseAvatarHeadGameObjects,
                virseAvatarTorsoGameObjects);
        }
    }

    public class PlayerSyncer 
    {
        private ILocalPlayerRig _localPlayerRig;

        private Dictionary<ushort, RemoteAvatarController> _remoteAvatars = new();
        private int _cycleNumber = 0;

        private List<GameObject> _virseAvatarHeadGameObjects;
        private List<GameObject> _virseAvatarTorsoGameObjects;

        private InstanceService _instanceService;
        private IPlayerAppearanceOverridesProvider _playerAppearanceOverridesProvider;
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

        //TODO - wire in lists for avatar override GOs rather than needing them here
        public PlayerSyncer(InstanceService instanceSevice, IPlayerAppearanceOverridesProvider playerAppearanceOverridesProvider, List<GameObject> virseAvatarHeadGameObjects, List<GameObject> virseAvatarTorsoGameObjects)
        {
            _instanceService = instanceSevice;

            _playerAppearanceOverridesProvider = playerAppearanceOverridesProvider;

            _virseAvatarHeadGameObjects = virseAvatarHeadGameObjects;
            _virseAvatarTorsoGameObjects = virseAvatarTorsoGameObjects;

            _instanceService.OnReceiveRemotePlayerState += HandleReceiveRemotePlayerState;
            _instanceService.OnInstanceInfoChanged += HandleInstanceInfoChanged;

            ViRSECoreServiceLocator.Instance.OnLocalPlayerRigRegistered += RegisterLocalPlayer;
            ViRSECoreServiceLocator.Instance.OnLocalPlayerRigDeregistered += DeregisterLocalPlayer;

            if (ViRSECoreServiceLocator.Instance.LocalPlayerRig != null)
                RegisterLocalPlayer(ViRSECoreServiceLocator.Instance.LocalPlayerRig);
        }

        private void HandleInstanceInfoChanged(InstancedInstanceInfo newInstanceInfo)
        {
            Dictionary<ushort, InstancedClientInfo> receivedRemoteClientInfosWithAppearance = new();
            foreach (KeyValuePair<ushort, InstancedClientInfo> kvp in newInstanceInfo.ClientInfos)
            {
                if (kvp.Key != _instanceService.LocalClientID && kvp.Value.InstancedAvatarAppearance.UsingViRSEPlayer)
                    receivedRemoteClientInfosWithAppearance.Add(kvp.Key, kvp.Value);
            }

            foreach (InstancedClientInfo receivedRemoteClientInfoWithAppearance in receivedRemoteClientInfosWithAppearance.Values)
            {
                if (!_remoteAvatars.ContainsKey(receivedRemoteClientInfoWithAppearance.ClientID))
                {
                    GameObject remotePlayerPrefab = Resources.Load<GameObject>("RemoteAvatar");
                    GameObject remotePlayerGO = GameObject.Instantiate(remotePlayerPrefab);
                    remotePlayerGO.GetComponent<RemoteAvatarController>().Initialize(_playerAppearanceOverridesProvider, _virseAvatarHeadGameObjects, _virseAvatarTorsoGameObjects);
                    _remoteAvatars.Add(receivedRemoteClientInfoWithAppearance.ClientID, remotePlayerGO.GetComponent<RemoteAvatarController>());
                }

                _remoteAvatars[receivedRemoteClientInfoWithAppearance.ClientID].HandleReceiveAvatarAppearance(receivedRemoteClientInfoWithAppearance.InstancedAvatarAppearance);
            }

            List<ushort> remoteClientIDsToDespawn = new(_remoteAvatars.Keys);
            remoteClientIDsToDespawn.RemoveAll(id => receivedRemoteClientInfosWithAppearance.ContainsKey(id));

            foreach (ushort idToDespawn in remoteClientIDsToDespawn)
            {
                GameObject.Destroy(_remoteAvatars[idToDespawn].gameObject);
                _remoteAvatars.Remove(idToDespawn);
            }
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

        public void HandleReceiveRemotePlayerState(byte[] stateAsBytes)
        {
            PlayerStateWrapper stateWrapper = new(stateAsBytes);
            PlayerState playerState = new(stateWrapper.StateBytes);

            if (_remoteAvatars.TryGetValue(stateWrapper.ID, out RemoteAvatarController remotePlayerController))
                remotePlayerController.HandleReceiveRemotePlayerState(playerState);
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
            foreach (RemoteAvatarController remotePlayerController in _remoteAvatars.Values)
                if (remotePlayerController != null && remotePlayerController.gameObject != null)
                    GameObject.Destroy(remotePlayerController.gameObject);

            _remoteAvatars.Clear();

            if (_playerAppearanceOverridesProvider != null)
                _playerAppearanceOverridesProvider.OnAppearanceOverridesChanged -= HandleLocalAppearanceChanged;
                
            _instanceService.OnReceiveRemotePlayerState -= HandleReceiveRemotePlayerState;
        }
    }
}
