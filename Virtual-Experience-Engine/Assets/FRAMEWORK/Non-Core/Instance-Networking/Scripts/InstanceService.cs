using UnityEngine;
using System.Net;
using static NonCoreCommonSerializables;
using static InstanceSyncSerializables;
using System;
using static VE2.InstanceNetworking.V_InstanceIntegration;
using VE2.Common;

namespace VE2.InstanceNetworking
{
    internal static class InstanceServiceFactory
    {
        internal static InstanceService Create(LocalClientIdWrapper localClientIDWrapper, bool connectAutomatically, ConnectionStateDebugWrapper connectionStateDebugWrapper) 
        {
            InstanceNetworkingCommsHandler commsHandler = new(new DarkRift.Client.DarkRiftClient());
            //TODO - maybe we should get avatar prefab GOs here too?

            return new InstanceService(commsHandler, localClientIDWrapper, 
            connectionStateDebugWrapper, VE2NonCoreServiceLocator.Instance.InstanceNetworkSettingsProvider,
            VE2CoreServiceLocator.Instance.WorldStateModulesContainer, VE2CoreServiceLocator.Instance.PlayerStateModuleContainer, 
            VE2CoreServiceLocator.Instance.InteractorContainer, VE2CoreServiceLocator.Instance.PlayerAppearanceOverridesProvider,
            connectAutomatically);
        }
    }

    //TODO: Wire CommsHandler into the syncer directly, less wiring overall, and more consistent with the containers that we inject into the syncers 
    internal class InstanceService 
    {
        //TODO, should take some config for events like "OnBecomeHost", "OnLoseHost", maybe also sync frequencies

        public bool IsConnectedToServer => _connectionStateDebugWrapper.ConnectionState == ConnectionState.Connected;
        public event Action OnConnectedToInstance;
        public event Action OnDisconnectedFromInstance;
        public event Action<ushort> OnHostChanged;

        //TODO: Wire in IntertorContainer
        //CommsManager dependencies
        private readonly IPluginSyncCommsHandler _commsHandler;
        private readonly ConnectionStateDebugWrapper _connectionStateDebugWrapper;
        private readonly IInstanceNetworkSettingsProvider _networkSettingsProvider;

        //WorldState sync dependencies
        private readonly WorldStateModulesContainer _worldStateModulesContainer;

        //Local PlayerSync dependencies
        private readonly PlayerStateModuleContainer _playerStateModuleContainer;
        private readonly IPlayerAppearanceOverridesProvider _playerAppearanceOverridesProvider;

        //Remote player sync dependencies 
        private readonly InteractorContainer _interactorContainer;

        //Common dependencies
        internal readonly InstanceInfoContainer _instanceInfoContainer;

        internal WorldStateSyncer _worldStateSyncer;
        internal LocalPlayerSyncer _localPlayerSyncer;
        internal RemotePlayerSyncer _remotePlayerSyncer;
        internal PingSyncer _pingSyncer;

        public InstanceService(IPluginSyncCommsHandler commsHandler, LocalClientIdWrapper localClientIDWrapper, 
        ConnectionStateDebugWrapper connectionStateDebugWrapper, IInstanceNetworkSettingsProvider instanceNetworkSettingsProvider, 
        WorldStateModulesContainer worldStateModulesContainer, PlayerStateModuleContainer playerStateModuleContainer,
        InteractorContainer interactorContainer, IPlayerAppearanceOverridesProvider playerAppearanceOverridesProvider, 
        bool connectAutomatically)
        {
            _commsHandler = commsHandler;
            _connectionStateDebugWrapper = connectionStateDebugWrapper;
            _networkSettingsProvider = instanceNetworkSettingsProvider;
            _worldStateModulesContainer = worldStateModulesContainer;
            _playerStateModuleContainer = playerStateModuleContainer;
            _interactorContainer = interactorContainer;
            _playerAppearanceOverridesProvider = playerAppearanceOverridesProvider;

            _instanceInfoContainer = new(localClientIDWrapper);

            _commsHandler.OnReceiveNetcodeConfirmation += HandleReceiveNetcodeVersion;
            _commsHandler.OnReceiveServerRegistrationConfirmation += HandleReceiveServerRegistrationConfirmation;
            _commsHandler.OnReceiveInstanceInfoUpdate += HandleReceiveInstanceInfoUpdate;
            _commsHandler.OnDisconnectedFromServer += HandleDisconnectFromServer;
            _commsHandler.OnReceivePingMessage += HandleReceivePingMessage;

            if (connectAutomatically)
                ConnectToServerWhenReady();
        }

        public void ConnectToServerWhenReady() 
        {
            if (_connectionStateDebugWrapper.ConnectionState != ConnectionState.NotYetConnected &&
                _connectionStateDebugWrapper.ConnectionState != ConnectionState.LostConnection)
                return;

            if (_networkSettingsProvider.AreInstanceNetworkingSettingsReady)
            {
                ConnectToServer();
            }
            else
            {
                _connectionStateDebugWrapper.ConnectionState = ConnectionState.WaitingForConnectionSettings;
                _networkSettingsProvider.OnInstanceNetworkSettingsReady += ConnectToServer;
            }
        }

        private void ConnectToServer() //TODO, encapsulate with the above in a ConnectionManager class 
        {
            _networkSettingsProvider.OnInstanceNetworkSettingsReady -= ConnectToServer;
            _connectionStateDebugWrapper.ConnectionState = ConnectionState.Connecting;

            InstanceNetworkSettings instanceConnectionDetails = _networkSettingsProvider.InstanceNetworkSettings;
            Debug.Log("Try connect... " + instanceConnectionDetails.IP);

            if (IPAddress.TryParse(instanceConnectionDetails.IP, out IPAddress ipAddress))
                _commsHandler.ConnectToServer(ipAddress, instanceConnectionDetails.Port);
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
                SendServerRegistration();
            }
        }

        private void SendServerRegistration() 
        {
            Debug.Log("<color=green> Try connect to server with instance code - " + _networkSettingsProvider.InstanceNetworkSettings.InstanceCode);

            //We also send the LocalClientID here, this will either be maxvalue (if this is our first time connecting, the server will give us a new ID)..
            //..or it'll be the ID we we're given by the server (if we're reconnecting, the server will use the ID we provide)
            ServerRegistrationRequest serverRegistrationRequest = new(_networkSettingsProvider.InstanceNetworkSettings.InstanceCode, _instanceInfoContainer.LocalClientID);
            _commsHandler.SendMessage(serverRegistrationRequest.Bytes, InstanceNetworkingMessageCodes.ServerRegistrationRequest, TransmissionProtocol.TCP);
        }

        private void HandleReceiveServerRegistrationConfirmation(byte[] bytes)
        {
            ServerRegistrationConfirmation serverRegistrationConfirmation = new(bytes);
            _instanceInfoContainer.LocalClientID = serverRegistrationConfirmation.LocalClientID;
            _instanceInfoContainer.InstanceInfo = serverRegistrationConfirmation.InstanceInfo;

            //Ready for syncing=======================================

            _worldStateSyncer = new(_worldStateModulesContainer, _instanceInfoContainer); //receives and transmits
            _worldStateSyncer.OnLocalChangeOrHostBroadcastWorldStateData += (BytesAndProtocol bytesAndProtocol) => _commsHandler.SendMessage(bytesAndProtocol.Bytes, InstanceNetworkingMessageCodes.WorldstateSyncableBundle, bytesAndProtocol.Protocol);
            _commsHandler.OnReceiveWorldStateSyncableBundle += _worldStateSyncer.HandleReceiveWorldStateBundle;

            _localPlayerSyncer = new(_playerStateModuleContainer, _instanceInfoContainer); //only transmits
            _localPlayerSyncer.OnAvatarAppearanceUpdatedLocally += (BytesAndProtocol bytesAndProtocol) => _commsHandler.SendMessage(bytesAndProtocol.Bytes, InstanceNetworkingMessageCodes.UpdateAvatarPresentation, bytesAndProtocol.Protocol);
            _localPlayerSyncer.OnPlayerStateUpdatedLocally += (BytesAndProtocol bytesAndProtocol) => _commsHandler.SendMessage(bytesAndProtocol.Bytes, InstanceNetworkingMessageCodes.PlayerState, bytesAndProtocol.Protocol);
            _localPlayerSyncer.TempDelayedPlayerReg(); //TODO: remove

            _remotePlayerSyncer = new(_instanceInfoContainer, _interactorContainer, _playerAppearanceOverridesProvider); //only receives
            _commsHandler.OnReceiveRemotePlayerState += _remotePlayerSyncer.HandleReceiveRemotePlayerState;

            _pingSyncer = new(_instanceInfoContainer);

            _connectionStateDebugWrapper.ConnectionState = ConnectionState.Connected;
            OnConnectedToInstance?.Invoke();
        }

        private void HandleReceiveInstanceInfoUpdate(byte[] bytes)
        {
            ushort previousHostID = _instanceInfoContainer.InstanceInfo.HostID;

            _instanceInfoContainer.InstanceInfo = new InstancedInstanceInfo(bytes);

            if (_instanceInfoContainer.InstanceInfo.HostID != previousHostID)
                OnHostChanged?.Invoke(_instanceInfoContainer.InstanceInfo.HostID);
        }

        public void NetworkUpdate() 
        {
            _commsHandler.MainThreadUpdate();

            if (IsConnectedToServer)
            {
                _worldStateSyncer.NetworkUpdate();
                _localPlayerSyncer.NetworkUpdate();
                _remotePlayerSyncer.NetworkUpdate();
                _pingSyncer.NetworkUpdate();
            }
        }

        public void HandleReceivePingMessage(byte[] bytes)
        {
            _pingSyncer.HandleReceivePingMessage(bytes);
        }

        public void DisconnectFromServer() => _commsHandler.DisconnectFromServer();

        private void HandleDisconnectFromServer() 
        {
            _worldStateSyncer.TearDown();
            _localPlayerSyncer.TearDown();
            _remotePlayerSyncer.TearDown();
            _pingSyncer.TearDown();

            _connectionStateDebugWrapper.ConnectionState = ConnectionState.LostConnection;
            OnDisconnectedFromInstance?.Invoke();
        }

        public void TearDown()
        {
            if (IsConnectedToServer)
            {
                _worldStateSyncer.TearDown();
                _localPlayerSyncer.TearDown();
                _remotePlayerSyncer.TearDown();
                _pingSyncer.TearDown();
            }

            _commsHandler.DisconnectFromServer();
            _commsHandler.OnReceiveNetcodeConfirmation -= HandleReceiveNetcodeVersion;
            _commsHandler.OnReceiveServerRegistrationConfirmation -= HandleReceiveServerRegistrationConfirmation;
            _commsHandler.OnReceiveInstanceInfoUpdate -= HandleReceiveInstanceInfoUpdate;
            _commsHandler.OnDisconnectedFromServer -= HandleDisconnectFromServer;
            _commsHandler.OnReceivePingMessage -= HandleReceivePingMessage;

            _connectionStateDebugWrapper.ConnectionState = ConnectionState.NotYetConnected;
        }
    }

    internal struct BytesAndProtocol
    {
        public byte[] Bytes;
        public TransmissionProtocol Protocol;

        public BytesAndProtocol(byte[] bytes, TransmissionProtocol protocol)
        {
            Bytes = bytes;
            Protocol = protocol;
        }
    }

    internal class InstanceInfoContainer
    {
        private readonly LocalClientIdWrapper _localClientIdWrapper;
        public ushort LocalClientID { get => _localClientIdWrapper.LocalClientID; set => _localClientIdWrapper.LocalClientID = value; }

        public bool IsHost => InstanceInfo.HostID == LocalClientID;

        public event Action<InstancedInstanceInfo> OnInstanceInfoChanged;
        private InstancedInstanceInfo _instanceInfo = null;
        public InstancedInstanceInfo InstanceInfo {
             get => _instanceInfo; 
             set {
                if (_instanceInfo != null && _instanceInfo.Equals(value))
                    return; 

                _instanceInfo = value;
                OnInstanceInfoChanged?.Invoke(_instanceInfo);
             } 
        }

        public InstanceInfoContainer(LocalClientIdWrapper localClientIdWrapper)
        {
            _localClientIdWrapper = localClientIdWrapper;
        }
    }
}
