using System.Collections.Generic;
using UnityEngine;
using ViRSE.Core.Player;
using ViRSE.Core.Shared;
using ViRSE.PluginRuntime;
using static InstanceSyncSerializables;
using static ViRSE.Core.Shared.CoreCommonSerializables;

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
            ViRSECoreServiceLocator.Instance.PlayerSpawner,
            ViRSECoreServiceLocator.Instance.PlayerSettingsProvider,
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
    private IPlayerSpawner _playerSpawner;
    private IPlayerSettingsProvider _playerSettingsProvider;
    private IPlayerAppearanceOverridesProvider _playerAppearanceOverridesProvider;
    private InstancedPlayerPresentation _instancedPlayerPresentation
    {
        get
        {
            bool usingViRSEAvatar = _playerSpawner != null && _playerSpawner.IsEnabled;
            PlayerPresentationConfig playerPresentationConfig = _playerSettingsProvider == null ? null : _playerSettingsProvider.UserSettings.PresentationConfig;

            bool applyOverrides = _playerAppearanceOverridesProvider != null;
            PlayerPresentationOverrides playerPresentationOverrides = _playerAppearanceOverridesProvider == null ? null : _playerAppearanceOverridesProvider.PlayerPresentationOverrides;

            return new(usingViRSEAvatar, playerPresentationConfig, applyOverrides, playerPresentationOverrides);
        }
    }

    public PlayerSyncer(InstanceService instanceSevice, IPlayerSpawner playerSpawner, IPlayerSettingsProvider playerSettingsProvider, IPlayerAppearanceOverridesProvider playerAppearanceOverridesProvider, List<GameObject> virseAvatarHeadGameObjects, List<GameObject> virseAvatarTorsoGameObjects)
    {
        _instanceService = instanceSevice;

        _playerSpawner = playerSpawner;
        _playerSettingsProvider = playerSettingsProvider;
        _playerAppearanceOverridesProvider = playerAppearanceOverridesProvider;

        _virseAvatarHeadGameObjects = virseAvatarHeadGameObjects;
        _virseAvatarTorsoGameObjects = virseAvatarTorsoGameObjects;

        _instanceService.OnReceiveRemotePlayerState += HandleReceiveRemotePlayerState;
        _instanceService.OnInstanceInfoChanged += HandleInstanceInfoChanged;

        if (_playerSpawner != null)
            _playerSpawner.OnEnabledStateChanged += HandleLocalAppearanceChanged;

        if (_playerSettingsProvider != null)
        {
            _playerSettingsProvider.OnLocalChangeToPlayerSettings += HandleLocalAppearanceChanged;

            //Send an avatar update once the settings are initially ready 
            if (_playerSettingsProvider.ArePlayerSettingsReady)
                HandleInitialAppearanceReady();
            else 
                _playerSettingsProvider.OnPlayerSettingsReady += HandleInitialAppearanceReady;
        }

        if (_playerAppearanceOverridesProvider != null)
            _playerAppearanceOverridesProvider.OnAppearanceOverridesChanged += HandleLocalAppearanceChanged;
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

    private void HandleInitialAppearanceReady()
    {
        _playerSettingsProvider.OnPlayerSettingsReady -= HandleInitialAppearanceReady;

        if (_instanceService.IsConnectedToServer)
            HandleLocalAppearanceChanged();
        else 
            _instanceService.OnConnectedToServer += HandleLocalAppearanceChanged;
    }

    private void HandleLocalAppearanceChanged()
    {
        Debug.Log($"InstanceService detected change to player settings using VAvatar? {_instancedPlayerPresentation.UsingViRSEPlayer}");
        _instanceService.SendAvatarAppearanceUpdate(_instancedPlayerPresentation.Bytes);
    }

    public void RegisterLocalPlayer(ILocalPlayerRig localPlayerRig)
    {
        _localPlayerRig = localPlayerRig;
    }

    public void DeregisterLocalPlayer()
    {
        _localPlayerRig = null;
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

    // private void SpawnNewRemotePlayer(ushort id)
    // {
    //     GameObject remotePlayerPrefab = Resources.Load<GameObject>("RemoteAvatar");
    //     GameObject remotePlayerGO = GameObject.Instantiate(remotePlayerPrefab);
    //     _remotePlayers.Add(id, remotePlayerGO.GetComponent<RemotePlayerController>());
    // }

    // private void DespawnRemotePlayer(ushort id)
    // {
    //     if (!_remotePlayers.TryGetValue(id, out RemotePlayerController remotePlayer))
    //     {
    //         Debug.LogError($"No remote player with id {id} found to despawn");
    //         return;
    //     }

    //     GameObject.Destroy(remotePlayer.gameObject);
    //     _remotePlayers.Remove(id);
    // }

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
