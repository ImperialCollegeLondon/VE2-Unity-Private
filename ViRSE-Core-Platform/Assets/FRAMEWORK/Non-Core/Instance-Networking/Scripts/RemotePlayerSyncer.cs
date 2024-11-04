using System.Collections.Generic;
using UnityEngine;
using VE2.Common;
using VE2.Common;
using static InstanceSyncSerializables;
using static VE2.Common.CoreCommonSerializables;

namespace VE2.InstanceNetworking
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
                virseAvatarHeadGameObjects,
                virseAvatarTorsoGameObjects,
                VE2CoreServiceLocator.Instance.PlayerAppearanceOverridesProvider.GetHeadOverrideGOs(),
                VE2CoreServiceLocator.Instance.PlayerAppearanceOverridesProvider.GetTorsoOverrideGOs());
        }
    }

    public class RemotePlayerSyncer
    {
        private Dictionary<ushort, RemoteAvatarController> _remoteAvatars = new();

        private readonly InstanceService _instanceService;
        private readonly List<GameObject> _virseAvatarHeadGameObjects;
        private readonly List<GameObject> _virseAvatarTorsoGameObjects;
        private readonly List<GameObject> _avatarHeadOverrideGameObjects;
        private readonly List<GameObject> _avatarTorsoOverrideGameObjects;


        public RemotePlayerSyncer(InstanceService instanceSevice, List<GameObject> virseAvatarHeadGameObjects, List<GameObject> virseAvatarTorsoGameObjects, List<GameObject> avatarHeadOverrideGameObjects, List<GameObject> avatarTorsoOverrideGameObjects)
        {
            _instanceService = instanceSevice;

            _virseAvatarHeadGameObjects = virseAvatarHeadGameObjects;
            _virseAvatarTorsoGameObjects = virseAvatarTorsoGameObjects;
            _avatarHeadOverrideGameObjects = avatarHeadOverrideGameObjects;
            _avatarTorsoOverrideGameObjects = avatarTorsoOverrideGameObjects;

            _instanceService.OnReceiveRemotePlayerState += HandleReceiveRemotePlayerState;
            _instanceService.OnInstanceInfoChanged += HandleInstanceInfoChanged;

            HandleInstanceInfoChanged(_instanceService.InstanceInfo);
        }

        private void HandleInstanceInfoChanged(InstancedInstanceInfo newInstanceInfo)
        {
            Dictionary<ushort, InstancedClientInfo> receivedRemoteClientInfosWithAppearance = new();
            foreach (KeyValuePair<ushort, InstancedClientInfo> kvp in newInstanceInfo.ClientInfos)
            {
                if (kvp.Key != _instanceService.LocalClientID && kvp.Value.AvatarAppearanceWrapper.UsingViRSEPlayer)
                    receivedRemoteClientInfosWithAppearance.Add(kvp.Key, kvp.Value);
            }

            foreach (InstancedClientInfo receivedRemoteClientInfoWithAppearance in receivedRemoteClientInfosWithAppearance.Values)
            {
                if (!_remoteAvatars.ContainsKey(receivedRemoteClientInfoWithAppearance.ClientID))
                {
                    GameObject remotePlayerPrefab = Resources.Load<GameObject>("RemoteAvatar");
                    GameObject remotePlayerGO = GameObject.Instantiate(remotePlayerPrefab);
                    remotePlayerGO.GetComponent<RemoteAvatarController>().Initialize(_virseAvatarHeadGameObjects, _virseAvatarTorsoGameObjects, _avatarHeadOverrideGameObjects, _avatarTorsoOverrideGameObjects);
                    _remoteAvatars.Add(receivedRemoteClientInfoWithAppearance.ClientID, remotePlayerGO.GetComponent<RemoteAvatarController>());
                }

                _remoteAvatars[receivedRemoteClientInfoWithAppearance.ClientID].HandleReceiveAvatarAppearance(receivedRemoteClientInfoWithAppearance.AvatarAppearanceWrapper.ViRSEAvatarAppearance);
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
            PlayerTransformData playerState = new(stateWrapper.StateBytes);

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
