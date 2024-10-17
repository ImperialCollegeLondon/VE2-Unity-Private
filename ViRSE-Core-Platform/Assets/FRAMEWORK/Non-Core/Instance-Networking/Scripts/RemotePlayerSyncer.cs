using System.Collections.Generic;
using UnityEngine;
using ViRSE.Core;
using static InstanceSyncSerializables;

namespace ViRSE.InstanceNetworking
{
    public static class RemotePlayerSyncerFactory
    {
        public static RemotePlayerSyncer Create(InstanceService instanceService)
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

            return new RemotePlayerSyncer(
                instanceService,
                ViRSECoreServiceLocator.Instance.PlayerAppearanceOverridesProvider,
                virseAvatarHeadGameObjects,
                virseAvatarTorsoGameObjects);
        }
    }

    public class RemotePlayerSyncer
    {
        private Dictionary<ushort, RemoteAvatarController> _remoteAvatars = new();

        private List<GameObject> _virseAvatarHeadGameObjects;
        private List<GameObject> _virseAvatarTorsoGameObjects;

        private InstanceService _instanceService;
        private IPlayerAppearanceOverridesProvider _playerAppearanceOverridesProvider;

        public RemotePlayerSyncer(InstanceService instanceSevice, IPlayerAppearanceOverridesProvider playerAppearanceOverridesProvider, List<GameObject> virseAvatarHeadGameObjects, List<GameObject> virseAvatarTorsoGameObjects)
        {
            _instanceService = instanceSevice;

            _playerAppearanceOverridesProvider = playerAppearanceOverridesProvider;

            _virseAvatarHeadGameObjects = virseAvatarHeadGameObjects;
            _virseAvatarTorsoGameObjects = virseAvatarTorsoGameObjects;

            _instanceService.OnReceiveRemotePlayerState += HandleReceiveRemotePlayerState;
            _instanceService.OnInstanceInfoChanged += HandleInstanceInfoChanged;
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

        public void HandleReceiveRemotePlayerState(byte[] stateAsBytes)
        {
            PlayerStateWrapper stateWrapper = new(stateAsBytes);
            PlayerState playerState = new(stateWrapper.StateBytes);

            if (_remoteAvatars.TryGetValue(stateWrapper.ID, out RemoteAvatarController remotePlayerController))
                remotePlayerController.HandleReceiveRemotePlayerState(playerState);
        }

        public void NetworkUpdate() {}

        public void TearDown()
        {
            foreach (RemoteAvatarController remotePlayerController in _remoteAvatars.Values)
                if (remotePlayerController != null && remotePlayerController.gameObject != null)
                    GameObject.Destroy(remotePlayerController.gameObject);

            _remoteAvatars.Clear();
            _instanceService.OnReceiveRemotePlayerState -= HandleReceiveRemotePlayerState;
        }
    }
}
