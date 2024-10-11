using UnityEngine;
using UnityEngine.Events;
using ViRSE.FrameworkRuntime;
using ViRSE.Core.Shared;
using ViRSE.Networking;
using System.Net;
using static NonCoreCommonSerializables;
using ViRSE.Core.Player;
using static InstanceSyncSerializables;
using static ViRSE.Core.Shared.CoreCommonSerializables;
using System;
using System.Linq;
using System.Collections.Generic;
using static ViRSE.InstanceNetworking.V_InstanceIntegration;

namespace ViRSE.PluginRuntime
{
    public static class PluginSyncServiceFactory
    {
        /// <summary>
        /// Pass a null config if the virse avatar isn't being used 
        /// </summary>
        /// <param name="playerPresentationConfig"></param>
        /// <returns></returns>
        public static PluginSyncService Create(LocalClientIdWrapper localClientIDWrapper) //Pass a reference to the player??
        {
            InstanceNetworkingCommsHandler commsHandler = new(new DarkRift.Client.DarkRiftClient());

            return new PluginSyncService(commsHandler, localClientIDWrapper);
        }
    }

    /// <summary>
    /// In charge of all sync management for the instance. Orchestrates WorldStateSyncer, PlayerSyncers, and InstantMessageRouter 
    /// </summary>
    public class PluginSyncService //: IInstanceService
    {
        //TODO, should take some config for events like "OnBecomeHost", "OnLoseHost", maybe also sync frequencies

        public bool IsConnectedToServer = false;
        public event Action OnConnectedToServer;
        public event Action OnDisconnectedFromServer;


        public ushort LocalClientID => _localClientIdWrapper.LocalClientID;
        private LocalClientIdWrapper _localClientIdWrapper;
        public InstancedInstanceInfo InstanceInfo;
        public event Action<InstancedInstanceInfo> OnInstanceInfoChanged;

        public bool IsHost => InstanceInfo.HostID == LocalClientID;

        public int WorldStateHistoryQueueSize { get; private set; }
        public UnityEvent<int> OnWorldStateHistoryQueueSizeChange { get; private set; } = new();

        public bool IsEnabled => true; //Bodge, the mono proxy for this needs this, and both currently use the same interface... maybe consider using different interfaces?

        private IPluginSyncCommsHandler _commsHandler;

        private IInstanceNetworkSettingsProvider _networkSettingsProvider;
        private IPlayerSpawner _playerSpawner;
        private IPlayerSettingsProvider _playerSettingsProvider; 
        private IPlayerAppearanceOverridesProvider _playerAppearanceOverridesProvider;
        private InstancedPlayerPresentation _instancedPlayerPresentation { 
            get {
                bool usingViRSEAvatar = _playerSpawner != null && _playerSpawner.IsEnabled;
                PlayerPresentationConfig playerPresentationConfig = _playerSettingsProvider == null ? null : _playerSettingsProvider.UserSettings.PresentationConfig;
                bool applyOverrides = _playerAppearanceOverridesProvider != null;
                PlayerPresentationOverrides playerPresentationOverrides = _playerAppearanceOverridesProvider == null ? null : _playerAppearanceOverridesProvider.PlayerPresentationOverrides;
                
                return new(usingViRSEAvatar, playerPresentationConfig, applyOverrides, playerPresentationOverrides);
                // if (_playerSettingsProvider == null || _playerAppearanceOverridesProvider == null)
                //     return new(false, null, null);
                // else
                //     return new(true, _playerSettingsProvider.UserSettings.PresentationConfig, _playerAppearanceOverridesProvider.PlayerPresentationOverrides);
            }
        }

        /*
            Right, the instance integration needs to be able to see what the player settings are 
            This might be off-platform, so the instance syncer shouldn't care if its platform or non-platform 
            The player spawner SHOULD have its own copy of UserSettings, this is what instance syncer queries 
            Player spawner is then in charge of either routing that from the provider, or from player prefs....

        */

        private WorldStateSyncer _worldStateSyncer;
        private PlayerSyncer _playerSyncer;

        private const int WORLD_STATE_SYNC_INTERVAL_MS = 20;

        #region Core Facing Interfaces
        public void RegisterStateModule(IStateModule stateModule, string stateType, string goName) =>
            _worldStateSyncer.RegisterWithSyncer(stateModule, stateType, goName);
        

        public void RegisterLocalPlayer(ILocalPlayerRig localPlayerRig) =>
            _playerSyncer.RegisterLocalPlayer(localPlayerRig);

        public void DeregisterLocalPlayer() =>
            _playerSyncer.DeregisterLocalPlayer();
        #endregion

        //TODO, consider constructing PluginSyncService using factory pattern to inject the WorldStateSyncer
        public PluginSyncService(IPluginSyncCommsHandler commsHandler, LocalClientIdWrapper localClientIDWrapper)
        {
            WorldStateHistoryQueueSize = 10; //TODO, automate this, currently 10 is more than enough though, 200ms

            _commsHandler = commsHandler;
            _localClientIdWrapper = localClientIDWrapper;
            //_commsHandler.OnReadyToSyncPlugin += HandleReadyToSyncPlugin;

            //TODO - maybe don't give the world state syncer the comms handler, we can just pull straight out of it here and send to comms
            _worldStateSyncer = new WorldStateSyncer(); //TODO DI (if this service references these at all, maybe it should be the other way around)
            _playerSyncer = PlayerSyncerFactory.Create();

            commsHandler.OnReceiveNetcodeConfirmation += HandleReceiveNetcodeVersion;
            commsHandler.OnReceiveServerRegistrationConfirmation += HandleReceiveServerRegistrationConfirmation;
            commsHandler.OnReceiveInstanceInfoUpdate += HandleReceiveInstanceInfoUpdate;
            commsHandler.OnReceiveWorldStateSyncableBundle += _worldStateSyncer.HandleReceiveWorldStateBundle;
            commsHandler.OnReceiveRemotePlayerState += _playerSyncer.HandleReceiveRemotePlayerState;
            _commsHandler.OnDisconnectedFromServer += HandleDisconnectFromServer;
        }

        public void ConnectToServer(IInstanceNetworkSettingsProvider networkSettingsProvider, IPlayerSpawner playerSpawner, IPlayerSettingsProvider playerSettingsProvider, IPlayerAppearanceOverridesProvider playerAppearanceOverridesProvider)
        {
            _networkSettingsProvider = networkSettingsProvider;
            _playerSpawner = playerSpawner;
            _playerSettingsProvider = playerSettingsProvider;
            _playerAppearanceOverridesProvider = playerAppearanceOverridesProvider;

            //TODO, maybe this should move to the PlayerSyncer? It's all about player appearance
            //Then again, we do need to send the player appearance to the server during registration...
            if (_playerSpawner != null) 
                _playerSpawner.OnEnabledStateChanged += OnPlayerAppearanceChanged;

            if (_playerSettingsProvider != null)
                _playerSettingsProvider.OnPlayerSettingsChanged += OnPlayerAppearanceChanged;

            if (_playerAppearanceOverridesProvider != null)
                _playerAppearanceOverridesProvider.OnAppearanceOverridesChanged += OnPlayerAppearanceChanged;

            InstanceNetworkSettings instanceConnectionDetails = _networkSettingsProvider.InstanceNetworkSettings;
            Debug.Log("Try connect... " + instanceConnectionDetails.IP);
            if (IPAddress.TryParse(instanceConnectionDetails.IP, out IPAddress ipAddress))
                _commsHandler.ConnectToServer(ipAddress, instanceConnectionDetails.Port);
            else
                Debug.LogError("Could not connect to server, invalid IP address");
        }

        private void OnPlayerAppearanceChanged() 
        {
            Debug.Log($"InstanceService detected change to player settings using VAvatar? {_instancedPlayerPresentation.UsingViRSEPlayer}"); 
            _commsHandler.SendMessage(_instancedPlayerPresentation.Bytes, InstanceNetworkingMessageCodes.UpdateAvatarPresentation, TransmissionProtocol.TCP);
        }

        private void HandleReceiveNetcodeVersion(byte[] bytes)
        {
            NetcodeVersionConfirmation netcodeVersionConfirmation = new(bytes);

            if (netcodeVersionConfirmation.NetcodeVersion != InstanceNetcodeVersion)
            {
                //TODO - handle bad netcode version
                Debug.LogError($"Bad netcode version, received version {netcodeVersionConfirmation.NetcodeVersion} but we are on {InstanceNetcodeVersion}");
            } 
            else
            {
                SendServerRegistration();
            }
        }

        private void SendServerRegistration() 
        {
            Debug.Log("<color=green> Try connect to server with instance code - " + _networkSettingsProvider.InstanceNetworkSettings.InstanceCode);
            //We also send the LocalClientID here, this will either be maxvalue (if this is our first time connecting, the server will give us a new ID)..
            //..or it'll be the ID we we're given by the server (if we're reconnecting, the server will use the ID we provide)

            ServerRegistrationRequest serverRegistrationRequest = new(_instancedPlayerPresentation, _networkSettingsProvider.InstanceNetworkSettings.InstanceCode, LocalClientID);

            Debug.Log($"Send server reg, using VAvatar? {_instancedPlayerPresentation.UsingViRSEPlayer}");

            _commsHandler.SendMessage(serverRegistrationRequest.Bytes, InstanceNetworkingMessageCodes.ServerRegistrationRequest, TransmissionProtocol.TCP);
        }

        private void HandleReceiveServerRegistrationConfirmation(byte[] bytes)
        {
            //Debug.Log("ABOUT TO READ reg=================================================");
            ServerRegistrationConfirmation serverRegistrationConfirmation = new(bytes);

            _localClientIdWrapper.LocalClientID = serverRegistrationConfirmation.LocalClientID;
            IsConnectedToServer = true;

            HandleReceiveInstanceInfoUpdate(serverRegistrationConfirmation.InstanceInfo);

            OnConnectedToServer?.Invoke();
        }

        private void HandleReceiveInstanceInfoUpdate(byte[] bytes)
        {
            if (bytes.SequenceEqual(InstanceInfo.Bytes))
                return;

            HandleReceiveInstanceInfoUpdate(new InstancedInstanceInfo(bytes));
        }

        private void HandleReceiveInstanceInfoUpdate(InstancedInstanceInfo newInstanceInfo)
        {
            //TODO - also check for hostship changes, emit events if we gain or lose hostship, AND events for players joining and leaving 
            //Debug.Log("Handle rec instane info");
            Dictionary<ushort, InstancedClientInfo> remoteClientInfos = new(newInstanceInfo.ClientInfos);
            remoteClientInfos.Remove(LocalClientID);

            _playerSyncer.HandleReceiveRemoteClientInfos(remoteClientInfos);

            InstanceInfo = newInstanceInfo;
            OnInstanceInfoChanged?.Invoke(InstanceInfo);
        }

        //TODO, not a fan of this anymore. This is just a service, only meant for sending and receiving data. Think the actual syncers themselves should be the ones pushing data, and listening to received messages
        //The worldstate syncer shouldn't be a dependency of the SyncService, that should be the other way around!
        //The question the becomes, "where does the syncer get made"?
        //Maybe by the mono? And maybe the mono should be called V_InstanceIntegration
        //Whenever a syncmodule (player sync module, 
        public void NetworkUpdate() 
        {
            _commsHandler.MainThreadUpdate();

            if (IsConnectedToServer)
            {
                (byte[], byte[]) worldStateBundlesToTransmit = _worldStateSyncer.HandleNetworkUpdate(IsHost);
                if (worldStateBundlesToTransmit.Item1 != null)
                    _commsHandler.SendMessage(worldStateBundlesToTransmit.Item1, InstanceNetworkingMessageCodes.WorldstateSyncableBundle, TransmissionProtocol.TCP);
                if (worldStateBundlesToTransmit.Item2 != null)
                    _commsHandler.SendMessage(worldStateBundlesToTransmit.Item2, InstanceNetworkingMessageCodes.WorldstateSyncableBundle, TransmissionProtocol.UDP);

                if (_playerSyncer.IsPlayerRegistered) 
                {
                    byte[] playerState = _playerSyncer.GetPlayerState();
                    if (playerState != null)
                    {
                        PlayerStateWrapper playerStateWrapper = new(LocalClientID, playerState);
                        _commsHandler.SendMessage(playerStateWrapper.Bytes, InstanceNetworkingMessageCodes.PlayerState, TransmissionProtocol.UDP); //TODO handle protocol
                    }
                }
            }
        }

        public void ReceivePingFromHost()
        {
            //TODO calc buffer size
            _worldStateSyncer.SetNewBufferLength(1);
            OnWorldStateHistoryQueueSizeChange.Invoke(5);
        }

        public void DisconnectFromServer() => _commsHandler.DisconnectFromServer();

        private void HandleDisconnectFromServer() 
        {
            _playerSyncer.TearDown();
            OnDisconnectedFromServer?.Invoke();
            
        }

        //But we want to keep the worldstate syncer active...
        //Think we need to separate "HandleDisconnect" 
        //And "stop playing"
        //Disconnecting should 
        public void TearDown()
        {
            _playerSyncer.TearDown();

            _commsHandler.DisconnectFromServer();
            _commsHandler.OnReceiveNetcodeConfirmation -= HandleReceiveNetcodeVersion;
            _commsHandler.OnReceiveServerRegistrationConfirmation -= HandleReceiveServerRegistrationConfirmation;
            _commsHandler.OnReceiveInstanceInfoUpdate -= HandleReceiveInstanceInfoUpdate;
            _commsHandler.OnReceiveWorldStateSyncableBundle -= _worldStateSyncer.HandleReceiveWorldStateBundle;
            _commsHandler.OnReceiveRemotePlayerState -= _playerSyncer.HandleReceiveRemotePlayerState;
            _commsHandler.OnDisconnectedFromServer -= HandleDisconnectFromServer;
        }
    }
}
