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

namespace ViRSE.PluginRuntime
{

/*
Why don't we just have the Instanced version live on the player?
The instanced version has "using virse avatar", overrides, and transparancy 

Yeah, I think let's make the Instanced version just a wrapper of the platform version 
That way we can still extract the platform-only one to send to the platform... composition over inheritence!


The thing is, the player spawner doesn't want knowledge of any of the instance networking stuff
So, the instance service should be the thing to convert it to the isntance version?
What happens when those settings are changed, though? HOW are they changed? Surely it should be through the player?

What will change at runtime? 
The avatar overrides - that would suggest that should be on the player then? There should be an API on the player itself to change these settings 
Yeah, changing override, should be on the player itself. 
So, we need the instance player config, AND the platform version that fits into it, to be in the common serializables 

unless we just have one set of player settings in the base, and then some mirror object in the platform version, that isn't coupled to the player at all
If we have this split out version of 


Ok, hold on. Currently, We're saying the platform server is different from the instancing server 
Is this really a requirement we want to hold on to?
We want everyone to be able to have their own on-prem platform server, but this should be separate, surely? 
We keep talking about wanting another level of separation above the current platform server... maybe that just means we're doing this separation in the wrong place?? 
We'd be giving both the platform server and the instance relay to the customer institution anyway... so why not just have them be the same thing?
That way, we don't have to send data to two separate servers. 
Right, but we would still have this problem of "people in the same instance as you should get one set of data, people in a different instance should get another"
So we DO still need a version of this data structure that cuts out the instance-specific stuff 
What's a sensible way to deliniate between instance specific and non-instance specific data, in a way that makes sense in the domain of core, off-platform single player? 
There kind of isn't one? That's why I think the platform should just have its own version that isn't coupled to the rest of it 

*/

    public static class PluginSyncServiceFactory
    {
        /// <summary>
        /// Pass a null config if the virse avatar isn't being used 
        /// </summary>
        /// <param name="playerPresentationConfig"></param>
        /// <returns></returns>
        public static PluginSyncService Create() //Pass a reference to the player??
        {
            InstanceNetworkingCommsHandler commsHandler = new(new DarkRift.Client.DarkRiftClient());

            return new PluginSyncService(commsHandler);
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


        public ushort LocalClientID { get; private set; } = ushort.MaxValue;
        public InstancedInstanceInfo InstanceInfo;
        public event Action<InstancedInstanceInfo> OnInstanceInfoChanged;

        public bool IsHost => InstanceInfo.HostID == LocalClientID;

        public int WorldStateHistoryQueueSize { get; private set; }
        public UnityEvent<int> OnWorldStateHistoryQueueSizeChange { get; private set; } = new();

        public bool IsEnabled => true; //Bodge, the mono proxy for this needs this, and both currently use the same interface... maybe consider using different interfaces?

        private IPluginSyncCommsHandler _commsHandler;

        private IInstanceNetworkSettingsProvider _networkSettingsProvider;
        private IPlayerSettingsProvider _playerSettingsProvider; 
        private IPlayerAppearanceOverridesProvider _playerAppearanceOverridesProvider;

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
        public void RegisterStateModule(IStateModule stateModule, string stateType, string goName)
        {
            _worldStateSyncer.RegisterWithSyncer(stateModule, stateType, goName);
        }

        public void RegisterLocalPlayer(ILocalPlayerRig localPlayerRig)
        {
            _playerSyncer.RegisterLocalPlayer(localPlayerRig);
        }
        #endregion

        //TODO, consider constructing PluginSyncService using factory pattern to inject the WorldStateSyncer
        public PluginSyncService(IPluginSyncCommsHandler commsHandler)
        {
            WorldStateHistoryQueueSize = 10; //TODO, automate this, currently 10 is more than enough though, 200ms

            _commsHandler = commsHandler;
            //_commsHandler.OnReadyToSyncPlugin += HandleReadyToSyncPlugin;

            //TODO - maybe don't give the world state syncer the comms handler, we can just pull straight out of it here and send to comms
            _worldStateSyncer = new WorldStateSyncer(); //TODO DI (if this service references these at all, or if should be the other way around)
            _playerSyncer = new PlayerSyncer(); //TODO DI

            commsHandler.OnReceiveNetcodeConfirmation += HandleReceiveNetcodeVersion;
            commsHandler.OnReceiveServerRegistrationConfirmation += HandleReceiveServerRegistrationConfirmation;
            commsHandler.OnReceiveInstanceInfoUpdate += HandleReceiveInstanceInfoUpdate;
            commsHandler.OnReceiveWorldStateSyncableBundle += _worldStateSyncer.HandleReceiveWorldStateBundle;
            commsHandler.OnReceiveRemotePlayerState += _playerSyncer.HandleReceiveRemotePlayerState;
            _commsHandler.OnDisconnectedFromServer += HandleDisconnectFromServer;
        }

        public void ConnectToServer(IInstanceNetworkSettingsProvider networkSettingsProvider, IPlayerSettingsProvider playerSettingsProvider, IPlayerAppearanceOverridesProvider playerAppearanceOverridesProvider)
        {
            _networkSettingsProvider = networkSettingsProvider;
            _playerSettingsProvider = playerSettingsProvider;
            _playerAppearanceOverridesProvider = playerAppearanceOverridesProvider;

            //if (_playerSettingsProvider != null)
                _playerSettingsProvider.OnPlayerSettingsChanged += OnPlayerAppearanceChanged;

            //if (_playerAppearanceOverridesProvider != null)
                _playerAppearanceOverridesProvider.OnAppearanceOverridesChanged += OnPlayerAppearanceChanged;

            // _playerSpawnConfig.OnLocalChangeToPlayerSettings += () => _commsHandler.SendMessage(_instancedPlayerPresentation.Bytes, InstanceNetworkingMessageCodes.UpdateAvatarPresentation, TransmissionProtocol.TCP);
            // _playerSpawnConfig.OnLocalChangeToAvatarOverrides += () => _commsHandler.SendMessage(_instancedPlayerPresentation.Bytes, InstanceNetworkingMessageCodes.UpdateAvatarPresentation, TransmissionProtocol.TCP);
            InstanceNetworkSettings instanceConnectionDetails = _networkSettingsProvider.InstanceNetworkSettings;
            Debug.Log("Try connect... " + instanceConnectionDetails.IP);
            if (IPAddress.TryParse(instanceConnectionDetails.IP, out IPAddress ipAddress))
                _commsHandler.ConnectToServer(ipAddress, instanceConnectionDetails.Port);
            else
                Debug.LogError("Could not connect to server, invalid IP address");
        }

        private void OnPlayerAppearanceChanged() 
        {
            //_commsHandler.
            Debug.Log("InstanceService detected change to player settings"); //TODO!
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

            InstancedPlayerPresentation instancedPlayerPresentation;
            if (_playerSettingsProvider == null || _playerAppearanceOverridesProvider == null)
                instancedPlayerPresentation = new(false, null, null);
            else
                instancedPlayerPresentation = new(true, _playerSettingsProvider.UserSettings.PresentationConfig, _playerAppearanceOverridesProvider.PlayerPresentationOverrides);

            ServerRegistrationRequest serverRegistrationRequest = new(instancedPlayerPresentation, _networkSettingsProvider.InstanceNetworkSettings.InstanceCode, LocalClientID);
            _commsHandler.SendMessage(serverRegistrationRequest.Bytes, InstanceNetworkingMessageCodes.ServerRegistrationRequest, TransmissionProtocol.TCP);

        }

        private void HandleReceiveServerRegistrationConfirmation(byte[] bytes)
        {
            //Debug.Log("ABOUT TO READ reg=================================================");
            ServerRegistrationConfirmation serverRegistrationConfirmation = new(bytes);

            LocalClientID = serverRegistrationConfirmation.LocalClientID;
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
            Debug.Log("Handle rec instane info");
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

                byte[] playerState = _playerSyncer.GetPlayerState();
                if (playerState != null)
                {
                    PlayerStateWrapper playerStateWrapper = new(LocalClientID, playerState);
                    _commsHandler.SendMessage(playerStateWrapper.Bytes, InstanceNetworkingMessageCodes.PlayerState, TransmissionProtocol.UDP); //TODO handle protocol
                }
            }
        }

        private void HandleLocalChangeToPlayerPresentation() 
        {
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
