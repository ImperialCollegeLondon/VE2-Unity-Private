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
using static DarkRift.Server.DarkRiftInfo;

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

            //if (_serverType == ServerType.Local)
            //{
            //    //TODO - start local server
            //}
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
