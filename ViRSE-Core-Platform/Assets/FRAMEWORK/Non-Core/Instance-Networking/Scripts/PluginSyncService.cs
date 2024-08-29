using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Timers;
using UnityEngine;
using UnityEngine.Events;
using ViRSE.FrameworkRuntime;
using ViRSE.Core.Shared;
using ViRSE.Networking;
using System.Net;
using static CommonNetworkObjects;
using ViRSE.Core.Player;
using static InstanceSyncNetworkObjects;

namespace ViRSE.PluginRuntime
{
    public static class PluginSyncServiceFactory
    {
        public static PluginSyncService Create(PlayerPresentationConfig playerPresentationConfig)
        {
            InstanceNetworkingCommsHandler commsHandler = new(new DarkRift.Client.DarkRiftClient());
            return new PluginSyncService(commsHandler, playerPresentationConfig);
        }
    }

    /// <summary>
    /// In charge of all sync management for the instance. Orchestrates WorldStateSyncer, PlayerSyncers, and InstantMessageRouter 
    /// </summary>
    public class PluginSyncService : INetworkManager
    {
        //TODO, should take some config for events like "OnBecomeHost", "OnLoseHost", maybe also sync frequencies

        private bool _readyToSync = false;

        private string _instanceCode;
        private ushort _localClientID;
        private InstanceInfo _instanceInfo;

        public bool IsHost => _instanceInfo.HostID == _localClientID;

        public int WorldStateHistoryQueueSize { get; private set; }
        public UnityEvent<int> OnWorldStateHistoryQueueSizeChange { get; private set; } = new();

        private IPluginSyncCommsHandler _commsHandler;
        private PlayerPresentationConfig _playerPresentationConfig;

        private WorldStateSyncer _worldStateSyncer;

        private const int WORLD_STATE_SYNC_INTERVAL_MS = 20;

        #region Core Facing Interfaces
        public void RegisterStateModule(IStateModule stateModule, string stateType, string goName)
        {
            _worldStateSyncer.RegisterWithSyncer(stateModule, stateType, goName);
        }
        #endregion

        //TODO, consider constructing PluginSyncService using factory pattern to inject the WorldStateSyncer
        public PluginSyncService(IPluginSyncCommsHandler commsHandler, PlayerPresentationConfig playerPresentationConfig)
        {
            WorldStateHistoryQueueSize = 10; //TODO, automate this, currently 10 is more than enough though, 200ms

            _commsHandler = commsHandler;
            _playerPresentationConfig = playerPresentationConfig;
            //_commsHandler.OnReadyToSyncPlugin += HandleReadyToSyncPlugin;

            //TODO - maybe don't give the world state syncer the comms handler, we can just pull straight out of it here and send to comms
            _worldStateSyncer = new WorldStateSyncer();

            commsHandler.OnReceiveNetcodeConfirmation += HandleReceiveNetcodeVersion;
            commsHandler.OnReceiveServerRegistrationConfirmation += HandleReceiveServerRegistrationConfirmation;
            commsHandler.OnReceiveWorldStateSyncableBundle += _worldStateSyncer.HandleReceiveWorldStateBundle;
        }

        public void ConnectToServer(IPAddress ipAddress, int portNumber, string instanceCode)
        {
            _instanceCode = instanceCode;
            _commsHandler.ConnectToServer(ipAddress, portNumber);
        }

        private void HandleReceiveNetcodeVersion(byte[] bytes)
        {
            NetcodeVersionConfirmation netcodeVersionConfirmation = new(bytes);

            if (netcodeVersionConfirmation.NetcodeVersion != InstanceSyncNetworkObjects.InstanceNetcodeVersion)
            {
                //TODO - handle bad netcode version
                Debug.LogError($"Bad netcode version, received version {netcodeVersionConfirmation.NetcodeVersion} but we are on {InstanceSyncNetworkObjects.InstanceNetcodeVersion}");
            } 
            else
            {
                Debug.Log("Rec nv, sending reg");

                //TODO, handle non default players
                //TODO, we might also want to see machine name without being on platform?
                AvatarAppearance avatarDetails = new(
                    _playerPresentationConfig == null,
                    _playerPresentationConfig.PlayerName,
                    _playerPresentationConfig.AvatarHeadType,
                    _playerPresentationConfig.AvatarBodyType,
                    _playerPresentationConfig.AvatarColor.r,
                    _playerPresentationConfig.AvatarColor.g,
                    _playerPresentationConfig.AvatarColor.b);

                ServerRegistrationRequest serverRegistrationRequest = new(avatarDetails, _instanceCode);
                _commsHandler.SendServerRegistrationRequest(serverRegistrationRequest.Bytes);
            }
        }

        private void HandleReceiveServerRegistrationConfirmation(byte[] bytes)
        {
            ServerRegistrationConfirmation serverRegistrationConfirmation = new(bytes);

            Debug.Log("Rec reg conf");

            _localClientID = serverRegistrationConfirmation.LocalClientID;
            _instanceInfo = serverRegistrationConfirmation.InstanceInfo;
            _readyToSync = true;
        }

        public void NetworkUpdate()
        {
            if (_readyToSync)
            {
                (byte[], byte[]) worldStateBundlesToTransmit = _worldStateSyncer.HandleNetworkUpdate(IsHost);

                if (worldStateBundlesToTransmit.Item1 != null)
                    _commsHandler.SendWorldStateBundle(worldStateBundlesToTransmit.Item1, TransmissionProtocol.TCP);
                if (worldStateBundlesToTransmit.Item2 != null)
                    _commsHandler.SendWorldStateBundle(worldStateBundlesToTransmit.Item2, TransmissionProtocol.UDP);

                //TODO transmit local player, handle remote players?
            }
        }

        public void ReceivePingFromHost()
        {
            //TODO calc buffer size
            _worldStateSyncer.SetNewBufferLength(1);
            OnWorldStateHistoryQueueSizeChange.Invoke(5);
        }

        public void TearDown()
        {
            //Probably destroy remote players?
        }
    }
}

/*
 * What needs doing here 
 * Connect to server
 * Receive netcode confirmation 
 * 
 * Anything to send to register
 * We need the instance code 
 * Everyone also needs to see the avatar presentation stuff 
 * So send this once, and also on TCP for updates 
 * 
 * After the handshake, we then send the instance code 
 * We connect to the server fresh when we enter the scene? 
 * What about when we just hop between instances? 
 * There's separation between the process of switching instances and switching worlds 
 * Worlds require a scene change, instances require a data flush
 * For changing instance within the same scene, we first request this to the server, and the server responds 
 * We seperate the instance code from the scene name 
 * 
 * After netcode confirmation, we send out scene name, and an instance allocation request 
 * 
 * How do we get the instance code?
 * From the hub ui, we connect to the server before even going to the world?
 * 
 * That would at least confirm it's doable 
 * The launcher could also do this... but the applcation would have to also connect itself too 
 * We could send in a CMD line argument from the launcher to connect to the server
 * 
 * Mm, don't forget this has to work off platform too 
 * 
 * If we're not on platform, what does instance code even do?
 * 
 * Ok, so we need a field for "instance code", but this needs to be overridable by the platform integration 
 * 
 * So the PluginSyncer can have inspector settings for "manual instance code" or "platform instance code"
 * 
 * 
 * So what does the relay server send us??
 * "InstancedClientInfo"? But we need the presentation deets for the platform too 
 * we need a list of people in the instance, as well as something to say who the host it 
 * This list needs to contain the presentation details... I think we're just going to have to accept that these come from both platform and server 
 * 
 * So in ident reg, we send our presentation deets 
 * Then there's some "InstancedClientInfo" that contains the IDs, display names, and presentation deets 
 * 
 */
