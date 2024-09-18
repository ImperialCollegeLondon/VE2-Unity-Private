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

namespace ViRSE.PluginRuntime
{
    public static class PluginSyncServiceFactory
    {
        /// <summary>
        /// Pass a null config if the virse avatar isn't being used 
        /// </summary>
        /// <param name="playerPresentationConfig"></param>
        /// <returns></returns>
        public static PluginSyncService Create(InstanceNetworkSettings instanceConnectionDetails, PlayerPresentationConfig playerPresentationConfig)
        {
            InstanceNetworkingCommsHandler commsHandler = new(new DarkRift.Client.DarkRiftClient());

            InstancedAvatarAppearance instancedAvatarAppearance; //TODO, maybe this should be a wrapper instead of a child class?
            if (playerPresentationConfig != null)
            {
                instancedAvatarAppearance = new(
                    playerPresentationConfig.PlayerName,
                    playerPresentationConfig.AvatarHeadType,
                    playerPresentationConfig.AvatarBodyType,
                    playerPresentationConfig.AvatarRed,
                    playerPresentationConfig.AvatarGreen,
                    playerPresentationConfig.AvatarBlue,
                    true,
                    "",
                    "",
                    false);
            }
            else
            {
                instancedAvatarAppearance = new(
                    "N/A",
                    "N/A",
                    "N/A",
                    0,
                    0,
                    0,
                    false, //NOT using ViRSE avatar
                    "N/A",
                    "N/A",
                    false);
            }

            return new PluginSyncService(commsHandler, instanceConnectionDetails, instancedAvatarAppearance);
        }
    }

    /// <summary>
    /// In charge of all sync management for the instance. Orchestrates WorldStateSyncer, PlayerSyncers, and InstantMessageRouter 
    /// </summary>
    public class PluginSyncService : INetworkManager
    {
        //TODO, should take some config for events like "OnBecomeHost", "OnLoseHost", maybe also sync frequencies

        private bool _readyToSync = false;

        private InstanceNetworkSettings _instanceConnectionDetails;
        private ushort _localClientID;
        private InstancedInstanceInfo _instanceInfo;

        public bool IsHost => _instanceInfo.HostID == _localClientID;

        public int WorldStateHistoryQueueSize { get; private set; }
        public UnityEvent<int> OnWorldStateHistoryQueueSizeChange { get; private set; } = new();

        public bool IsEnabled => true; //Bodge, the mono proxy for this needs this, and both currently use the same interface... maybe consider using different interfaces?

        private IPluginSyncCommsHandler _commsHandler;
        private InstancedAvatarAppearance _instancedAvatarAppearance;

        private WorldStateSyncer _worldStateSyncer;

        private const int WORLD_STATE_SYNC_INTERVAL_MS = 20;

        #region Core Facing Interfaces
        public void RegisterStateModule(IStateModule stateModule, string stateType, string goName)
        {
            _worldStateSyncer.RegisterWithSyncer(stateModule, stateType, goName);
        }
        #endregion

        //TODO, consider constructing PluginSyncService using factory pattern to inject the WorldStateSyncer
        public PluginSyncService(IPluginSyncCommsHandler commsHandler, InstanceNetworkSettings instanceConnectionDetails, InstancedAvatarAppearance instancedAvatarAppearance)
        {
            WorldStateHistoryQueueSize = 10; //TODO, automate this, currently 10 is more than enough though, 200ms

            _commsHandler = commsHandler;
            _instanceConnectionDetails = instanceConnectionDetails;
            _instancedAvatarAppearance = instancedAvatarAppearance;
            //_commsHandler.OnReadyToSyncPlugin += HandleReadyToSyncPlugin;

            //TODO - maybe don't give the world state syncer the comms handler, we can just pull straight out of it here and send to comms
            _worldStateSyncer = new WorldStateSyncer();

            commsHandler.OnReceiveNetcodeConfirmation += HandleReceiveNetcodeVersion;
            commsHandler.OnReceiveServerRegistrationConfirmation += HandleReceiveServerRegistrationConfirmation;
            commsHandler.OnReceiveInstanceInfoUpdate += HandleReceiveInstanceInfoUpdate;
            commsHandler.OnReceiveWorldStateSyncableBundle += _worldStateSyncer.HandleReceiveWorldStateBundle;
        }

        public void ConnectToServer()
        {
            if (IPAddress.TryParse(_instanceConnectionDetails.IP, out IPAddress ipAddress))
                _commsHandler.ConnectToServer(ipAddress, _instanceConnectionDetails.Port);
            else
                Debug.LogError("Could not connect to server, invalid IP address");
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
                //Debug.Log("Rec instance server nv, sending reg");

                InstancedAvatarAppearance instancedAvatarAppearance; //TODO, this dependency should come from the factory?
                if (_instancedAvatarAppearance != null)
                {
                    instancedAvatarAppearance = new(
                        _instancedAvatarAppearance.PlayerName,
                        _instancedAvatarAppearance.AvatarHeadType,
                        _instancedAvatarAppearance.AvatarBodyType,
                        _instancedAvatarAppearance.AvatarRed,
                        _instancedAvatarAppearance.AvatarGreen,
                        _instancedAvatarAppearance.AvatarBlue,
                        true,
                        "",
                        "",
                        false);
                }
                else
                {
                    instancedAvatarAppearance = new(
                        "N/A",
                        "N/A",
                        "N/A",
                        0,
                        0,
                        0,
                        false, //NOT using ViRSE avatar
                        "N/A",
                        "N/A",
                        false);
                }

                Debug.Log("<color=green> Try connect to server with instance code - " + _instanceConnectionDetails.InstanceCode + "</color>");
                ServerRegistrationRequest serverRegistrationRequest = new(instancedAvatarAppearance, _instanceConnectionDetails.InstanceCode);
                _commsHandler.SendServerRegistrationRequest(serverRegistrationRequest.Bytes);
            }
        }

        private void HandleReceiveServerRegistrationConfirmation(byte[] bytes)
        {
            ServerRegistrationConfirmation serverRegistrationConfirmation = new(bytes);

           // Debug.Log("Rec instanceserver reg conf");

            _localClientID = serverRegistrationConfirmation.LocalClientID;
            _instanceInfo = serverRegistrationConfirmation.InstanceInfo;
            _readyToSync = true;
        }

        private void HandleReceiveInstanceInfoUpdate(byte[] bytes)
        {
            InstancedInstanceInfo newInstanceInfo = new(bytes);

            //TODO - check against _instanceInfo, detect clients that have joined, and clients that have left 
            //TODO - also check for hostship changes, emit events if we gain or lose hostship 

            _instanceInfo = newInstanceInfo;
            //Debug.Log("Rec inst info update - " + IsHost);
        }

        //TODO, not a fan of this anymore. This is just a service, only meant for sending and receiving data. Think the actual syncers themselves should be the ones pushing data, and listening to received messages
        //The worldstate syncer shouldn't be a dependency of the SyncService, that should be the other way around!
        //The question the becomes, "where does the syncer get made"?
        //Maybe by the mono? And maybe the mono should be called V_InstanceIntegration
        //Whenever a syncmodule (player sync module, 
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
            _commsHandler.DisconnectFromServer();
        }
    }
}
