using UnityEngine;
using System.Net;
using static NonCoreCommonSerializables;
using static InstanceSyncSerializables;
using System;
using static VE2.InstanceNetworking.V_InstanceIntegration;
using VE2.Common;
using static VE2.Platform.API.PlatformPublicSerializables;

namespace VE2.InstanceNetworking
{
    internal static class InstanceServiceFactory
    {
        internal static InstanceService Create(LocalClientIdWrapper localClientIDWrapper, bool connectAutomatically, ConnectionStateDebugWrapper connectionStateDebugWrapper) 
        {
            InstanceNetworkingCommsHandler commsHandler = new(new DarkRift.Client.DarkRiftClient());

            return new InstanceService(
                commsHandler, 
                localClientIDWrapper, 
                connectionStateDebugWrapper, 
                PlatformAPI.PlatformService as IPlatformServiceInternal,
                PlayerAPI.InteractorContainer,
                PlayerAPI.Player as IPlayerServiceInternal,
                connectAutomatically);
        }
    }

    internal class InstanceService : IInstanceService, IInstanceServiceInternal
    {
        //TODO, should take some config for events like "OnBecomeHost", "OnLoseHost"

        #region Public interfaces
        public bool IsConnectedToServer => _connectionStateDebugWrapper.ConnectionState == ConnectionState.Connected;
        public event Action OnConnectedToInstance;
        public event Action OnDisconnectedFromInstance;
        public void ConnectToInstance() => ConnectToServer();
        public void DisconnectFromInstance() => DisconnectFromServer();
        public bool IsHost => _instanceInfoContainer.IsHost;
        #endregion


        #region Internal interfaces
        public event Action<ushort> OnClientIDReady;
        internal IWorldStateSyncService WorldStateSyncService => _worldStateSyncer;
        #endregion


        #region Shared interfaces
        public ushort LocalClientID => _instanceInfoContainer.LocalClientID;
        #endregion


        #region Temp debug interfaces //TODO: remove once UI is in 
        public InstancedInstanceInfo InstanceInfo => _instanceInfoContainer.InstanceInfo;
        public event Action<InstancedInstanceInfo> OnInstanceInfoChanged { 
            add => _instanceInfoContainer.OnInstanceInfoChanged += value; 
            remove => _instanceInfoContainer.OnInstanceInfoChanged -= value; 
        }
        #endregion


        private readonly IPluginSyncCommsHandler _commsHandler;
        private readonly ConnectionStateDebugWrapper _connectionStateDebugWrapper;
        private readonly IPlatformServiceInternal _platformService;
        private readonly IPlayerServiceInternal _playerService;
        private readonly InteractorContainer _interactorContainer;
        private readonly InstanceInfoContainer _instanceInfoContainer;

        internal readonly WorldStateSyncer _worldStateSyncer;
        internal readonly LocalPlayerSyncer _localPlayerSyncer;
        internal readonly RemotePlayerSyncer _remotePlayerSyncer;

        public InstanceService(IPluginSyncCommsHandler commsHandler, LocalClientIdWrapper localClientIDWrapper, 
            ConnectionStateDebugWrapper connectionStateDebugWrapper, IPlatformServiceInternal platformService, 
            InteractorContainer interactorContainer, IPlayerServiceInternal playerServiceInternal,
            bool connectAutomatically)
        {
            _commsHandler = commsHandler;
            _connectionStateDebugWrapper = connectionStateDebugWrapper;
            _platformService = platformService;
            _interactorContainer = interactorContainer;
            _playerService = playerServiceInternal;

            _instanceInfoContainer = new(localClientIDWrapper);

            _commsHandler.OnReceiveNetcodeConfirmation += HandleReceiveNetcodeVersion;
            _commsHandler.OnReceiveServerRegistrationConfirmation += HandleReceiveServerRegistrationConfirmation;
            _commsHandler.OnReceiveInstanceInfoUpdate += HandleReceiveInstanceInfoUpdate;
            _commsHandler.OnDisconnectedFromServer += HandleDisconnectFromServer;

            _worldStateSyncer = new(_commsHandler, _instanceInfoContainer); //receives and transmits
            _localPlayerSyncer = new(_commsHandler, playerServiceInternal, _instanceInfoContainer); //only transmits
            _remotePlayerSyncer = new(_commsHandler, _instanceInfoContainer, _interactorContainer, _playerService); //only receives

            if (connectAutomatically)
                ConnectToServer();
        }

        private void ConnectToServer() //TODO: expose to plugin for delayed connection?
        {
            if (_connectionStateDebugWrapper.ConnectionState == ConnectionState.Connecting &&
                _connectionStateDebugWrapper.ConnectionState == ConnectionState.Connected)
            {
                Debug.LogWarning("Attempted to connect to server while already connected or connecting");
                return;
            }

            _connectionStateDebugWrapper.ConnectionState = ConnectionState.Connecting;

            ServerConnectionSettings serverConnectionSettings = _platformService.GetInstanceServerSettingsForCurrentWorld();

            Debug.Log("Try connect... " + serverConnectionSettings.ServerAddress);

            if (IPAddress.TryParse(serverConnectionSettings.ServerAddress, out IPAddress ipAddress))
                _commsHandler.ConnectToServer(ipAddress, serverConnectionSettings.ServerPort);
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
            Debug.Log("<color=green> Try connect to server with instance code - " + _platformService.CurrentInstanceCode);

            bool usingFrameworkAvatar = true; //TODO
            AvatarAppearanceWrapper avatarAppearanceWrapper = new(usingFrameworkAvatar, _playerService.OverridableAvatarAppearance);

            //We also send the LocalClientID here, this will either be maxvalue (if this is our first time connecting, the server will give us a new ID)..
            //..or it'll be the ID we we're restored after a disconnect (if we're reconnecting, the server will use the ID we provide)
            ServerRegistrationRequest serverRegistrationRequest = new(_platformService.CurrentInstanceCode, _instanceInfoContainer.LocalClientID, avatarAppearanceWrapper);
            _commsHandler.SendMessage(serverRegistrationRequest.Bytes, InstanceNetworkingMessageCodes.ServerRegistrationRequest, TransmissionProtocol.TCP);
        }

        private void HandleReceiveServerRegistrationConfirmation(byte[] bytes)
        {
            ServerRegistrationConfirmation serverRegistrationConfirmation = new(bytes);
            _instanceInfoContainer.LocalClientID = serverRegistrationConfirmation.LocalClientID;
            _instanceInfoContainer.InstanceInfo = serverRegistrationConfirmation.InstanceInfo;

            //Ready for syncing=======================================

            _connectionStateDebugWrapper.ConnectionState = ConnectionState.Connected;
            OnConnectedToInstance?.Invoke();
        }

        private void HandleReceiveInstanceInfoUpdate(byte[] bytes) =>  _instanceInfoContainer.InstanceInfo = new InstancedInstanceInfo(bytes);

        internal void NetworkUpdate() 
        {
            _commsHandler.MainThreadUpdate();

            if (IsConnectedToServer)
            {
                _worldStateSyncer.NetworkUpdate();
                _localPlayerSyncer.NetworkUpdate();
                _remotePlayerSyncer.NetworkUpdate();
            }
        }

        private void ReceivePingFromHost() {} //TODO

        private void DisconnectFromServer() 
        {
            if (_connectionStateDebugWrapper.ConnectionState != ConnectionState.Connected)
            {
                Debug.LogWarning("Attempted to disconnect to server while not connected");
                return;
            }
            _commsHandler.DisconnectFromServer();
        } 

        private void HandleDisconnectFromServer() 
        {
            _worldStateSyncer.TearDown();
            _localPlayerSyncer.TearDown();
            _remotePlayerSyncer.TearDown();

            _connectionStateDebugWrapper.ConnectionState = ConnectionState.LostConnection;
            OnDisconnectedFromInstance?.Invoke();
        }

        internal void TearDown()
        {
            if (IsConnectedToServer)
            {
                _worldStateSyncer.TearDown();
                _localPlayerSyncer.TearDown();
                _remotePlayerSyncer.TearDown();
            }

            _commsHandler.DisconnectFromServer();
            _commsHandler.OnReceiveNetcodeConfirmation -= HandleReceiveNetcodeVersion;
            _commsHandler.OnReceiveServerRegistrationConfirmation -= HandleReceiveServerRegistrationConfirmation;
            _commsHandler.OnReceiveInstanceInfoUpdate -= HandleReceiveInstanceInfoUpdate;
            _commsHandler.OnDisconnectedFromServer -= HandleDisconnectFromServer;

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
        public readonly LocalClientIdWrapper LocalClientIdWrapper;
        public ushort LocalClientID { get => LocalClientIdWrapper.LocalClientID; set => LocalClientIdWrapper.LocalClientID = value; }

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
            LocalClientIdWrapper = localClientIdWrapper;
        }
    }
}
