using System.Collections.Generic;
using UnityEngine;
using VE2.Core.Player.API;
using VE2.Core.VComponents.API;
using static VE2.Core.Player.API.PlayerSerializables;
using static VE2.NonCore.Instancing.Internal.InstanceSyncSerializables;

namespace VE2.NonCore.Instancing.Internal
{
    internal class RemotePlayerSyncer
    {
       private readonly IPluginSyncCommsHandler _commsHandler;
        private readonly InstanceInfoContainer _instanceInfoContainer;
        private readonly HandInteractorContainer _interactorContainer;

        private readonly List<GameObject> _virseAvatarHeadGameObjects;
        private readonly List<GameObject> _virseAvatarTorsoGameObjects;
        private readonly List<GameObject> _avatarHeadOverrideGameObjects;
        private readonly List<GameObject> _avatarTorsoOverrideGameObjects;

        private Dictionary<ushort, RemoteAvatarController> _remoteAvatars = new();

        public RemotePlayerSyncer(IPluginSyncCommsHandler commsHandler, InstanceInfoContainer instanceInfoContainer, HandInteractorContainer interactorContainer, IPlayerServiceInternal playerService)
        {
            _commsHandler = commsHandler;
            _commsHandler.OnReceiveRemotePlayerState += HandleReceiveRemotePlayerState;

            _instanceInfoContainer = instanceInfoContainer;
            _instanceInfoContainer.OnInstanceInfoChanged += HandleInstanceInfoChanged;

            _interactorContainer = interactorContainer;

            _virseAvatarHeadGameObjects = new List<GameObject>()
            {
                Resources.Load<GameObject>("Avatars/Heads/V_Avatar_Head_Default_1"),
                Resources.Load<GameObject>("Avatars/Heads/V_Avatar_Head_Default_2"),
            };

            _virseAvatarTorsoGameObjects = new List<GameObject>()
            {
                Resources.Load<GameObject>("Avatars/Torsos/V_Avatar_Torso_Default_1"),
            };

            _avatarHeadOverrideGameObjects = playerService.HeadOverrideGOs;
            _avatarTorsoOverrideGameObjects = playerService.HeadOverrideGOs;
        }

        private void HandleInstanceInfoChanged(InstancedInstanceInfo newInstanceInfo)
        {
            //Debug.Log("RemotePlayerSyncer: HandleInstanceInfoChanged");
            Dictionary<ushort, InstancedClientInfo> receivedRemoteClientInfosWithAppearance = new();
            foreach (KeyValuePair<ushort, InstancedClientInfo> kvp in newInstanceInfo.ClientInfos)
            {
                //Debug.Log("Checking client: " + kvp.Key + " - " + kvp.Value.InstancedAvatarAppearance.UsingFrameworkPlayer);
                if (kvp.Key != _instanceInfoContainer.LocalClientID && kvp.Value.InstancedAvatarAppearance.UsingFrameworkPlayer)
                    receivedRemoteClientInfosWithAppearance.Add(kvp.Key, kvp.Value);
            }

            foreach (InstancedClientInfo receivedRemoteClientInfoWithAppearance in receivedRemoteClientInfosWithAppearance.Values)
            {
                if (!_remoteAvatars.ContainsKey(receivedRemoteClientInfoWithAppearance.ClientID))
                {
                    GameObject remotePlayerPrefab = Resources.Load<GameObject>("RemoteAvatar");
                    GameObject remotePlayerGO = GameObject.Instantiate(remotePlayerPrefab);
                    remotePlayerGO.GetComponent<RemoteAvatarController>().Initialize(
                        receivedRemoteClientInfoWithAppearance.ClientID,
                        _interactorContainer,
                        _virseAvatarHeadGameObjects, 
                        _virseAvatarTorsoGameObjects, 
                        _avatarHeadOverrideGameObjects, 
                        _avatarTorsoOverrideGameObjects);

                    _remoteAvatars.Add(receivedRemoteClientInfoWithAppearance.ClientID, remotePlayerGO.GetComponent<RemoteAvatarController>());
                }

                _remoteAvatars[receivedRemoteClientInfoWithAppearance.ClientID].HandleReceiveAvatarAppearance(receivedRemoteClientInfoWithAppearance.InstancedAvatarAppearance.OverridableAvatarAppearance);
            }

            List<ushort> remoteClientIDsToDespawn = new(_remoteAvatars.Keys);
            remoteClientIDsToDespawn.RemoveAll(id => receivedRemoteClientInfosWithAppearance.ContainsKey(id));

            foreach (ushort idToDespawn in remoteClientIDsToDespawn)
            {
                GameObject.Destroy(_remoteAvatars[idToDespawn].gameObject);
                _remoteAvatars.Remove(idToDespawn);
            }
        }

        private void HandleReceiveRemotePlayerState(byte[] stateAsBytes)
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
            _instanceInfoContainer.OnInstanceInfoChanged -= HandleInstanceInfoChanged;
        }
    }
}
