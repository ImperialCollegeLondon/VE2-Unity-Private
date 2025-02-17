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
            //TODO - maybe we should get avatar prefab GOs here too?

            return new InstanceService(
                commsHandler, 
                localClientIDWrapper, 
                connectionStateDebugWrapper, 
                PlatformServiceLocator.PlatformService as IPlatformServiceInternal,
                PlayerLocator.InteractorContainer,
                PlayerLocator.Player as IPlayerServiceInternal,
                connectAutomatically);
        }
    }

    //TODO: Wire CommsHandler into the syncer directly, less wiring overall, and more consistent with the containers that we inject into the syncers 
    internal class InstanceService : IInstanceService, IInstanceServiceInternal, ILocalClientIDProvider
    {
        //TODO, should take some config for events like "OnBecomeHost", "OnLoseHost", maybe also sync frequencies

        public bool IsConnectedToServer => _connectionStateDebugWrapper.ConnectionState == ConnectionState.Connected;
        // public event Action OnConnectedToInstance;
        // public event Action OnDisconnectedFromInstance;

        #region Temp debug Interfaces //TODO: remove once UI is in 
        public InstancedInstanceInfo InstanceInfo => _instanceInfoContainer.InstanceInfo;
        public event Action<InstancedInstanceInfo> OnInstanceInfoChanged { 
            add => _instanceInfoContainer.OnInstanceInfoChanged += value; 
            remove => _instanceInfoContainer.OnInstanceInfoChanged -= value; 
        }

        //Maybe we shouldn't have an interface for the actual service... nothing is using it anyway?
        //if its all syncronous, then we don't need the StateModuleContainers at all...
        //So we use the normal API system, but the API also has a public bool for "HasSupport" or something 

        //so IBaseMultiplayerSupport has 
        //So BaseMultiplayerSupport lives in VE2 Common 
        //But 
        #endregion

        //TODO: Wire in IntertorContainer
        //CommsManager dependencies
        private readonly IPluginSyncCommsHandler _commsHandler;
        private readonly ConnectionStateDebugWrapper _connectionStateDebugWrapper;

        //Platform dependencies
        private readonly IPlatformServiceInternal _platformService;

        private IPlayerServiceInternal _playerService;

        //Remote player sync dependencies 
        private readonly InteractorContainer _interactorContainer;

        //Common dependencies
        internal readonly InstanceInfoContainer _instanceInfoContainer;

        //internal 

        internal IWorldStateSyncService WorldStateSyncService => _worldStateSyncer;
        internal ILocalClientIDProvider PlayerSyncService => _localPlayerSyncer;

        public ushort LocalClientID => _instanceInfoContainer.LocalClientID;

        public bool IsEnabled => throw new NotImplementedException();

        public string GameObjectName => throw new NotImplementedException();

        public bool IsHost => throw new NotImplementedException();

        internal WorldStateSyncer _worldStateSyncer;
        internal LocalPlayerSyncer _localPlayerSyncer;
        internal RemotePlayerSyncer _remotePlayerSyncer;

        public event Action<ushort> OnClientIDReady;
        public event Action OnConnectedToInstance;
        public event Action OnDisconnectedFromInstance;

        public InstanceService(IPluginSyncCommsHandler commsHandler, LocalClientIdWrapper localClientIDWrapper, 
        ConnectionStateDebugWrapper connectionStateDebugWrapper, IPlatformServiceInternal platformService, 
        InteractorContainer interactorContainer, IPlayerServiceInternal playerServiceInternal,
        bool connectAutomatically)
        {
            _commsHandler = commsHandler;
            _connectionStateDebugWrapper = connectionStateDebugWrapper;
            _platformService = platformService;
            _interactorContainer = interactorContainer;

            _instanceInfoContainer = new(localClientIDWrapper);

            _commsHandler.OnReceiveNetcodeConfirmation += HandleReceiveNetcodeVersion;
            _commsHandler.OnReceiveServerRegistrationConfirmation += HandleReceiveServerRegistrationConfirmation;
            _commsHandler.OnReceiveInstanceInfoUpdate += HandleReceiveInstanceInfoUpdate;
            _commsHandler.OnDisconnectedFromServer += HandleDisconnectFromServer;


            _worldStateSyncer = new(_commsHandler, _instanceInfoContainer); //receives and transmits
            _localPlayerSyncer = new(_commsHandler, _instanceInfoContainer.LocalClientIdWrapper, playerServiceInternal, _instanceInfoContainer); //only transmits

            /*
                Do we want to connect automatically? 
                Well, no, maybe the plugin has some kind of staging scene? 
                So, the connection should be programmatic via an api, but you shouln't be able to pass an isntance code?

                So if not connect automatically, then plugin needs to be able to trigger connection to a certain instance code 
                Do we really care about this???

                BUt then that lets the plugin override the instance code that we get from the hub... which we don't want... 

                We're saying we don't want to have to handshake with platform before we start instance syncing, right?
                So then in the editor, we need to be able to launch right into a plugin, and connect to instancing without connecting to platform 
                How??

                Honestly... I think let's just keep it simple, and assume it always comes from a server 

                The alternative would be to have the platform return some default setting??
                So configure platform service with default instance code, but also with default instance instancing settings?? 
                Ah, idk, we're talking about less than 250 seconds of load time here... let's not worry about it for now I think 
            */

            //TODO: create syncers here

            if (connectAutomatically)
                ConnectToServerWhenReady();
        }

        public void ConnectToServerWhenReady() 
        {
            if (_connectionStateDebugWrapper.ConnectionState != ConnectionState.NotYetConnected &&
                _connectionStateDebugWrapper.ConnectionState != ConnectionState.LostConnection)
                return;

            if (_platformService.IsConnectedToServer)
            {
                ConnectToServer();
            }
            else
            {
                _connectionStateDebugWrapper.ConnectionState = ConnectionState.WaitingForConnectionSettings;
                _platformService.OnConnectedToServer += ConnectToServer;
            }
        }

        private void ConnectToServer() //TODO, encapsulate with the above in a ConnectionManager class 
        {
            _platformService.OnConnectedToServer -= ConnectToServer;
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

            //We also send the LocalClientID here, this will either be maxvalue (if this is our first time connecting, the server will give us a new ID)..
            //..or it'll be the ID we we're given by the server (if we're reconnecting, the server will use the ID we provide)
            ServerRegistrationRequest serverRegistrationRequest = new(_platformService.CurrentInstanceCode, _instanceInfoContainer.LocalClientID);
            _commsHandler.SendMessage(serverRegistrationRequest.Bytes, InstanceNetworkingMessageCodes.ServerRegistrationRequest, TransmissionProtocol.TCP);
        }

        private void HandleReceiveServerRegistrationConfirmation(byte[] bytes)
        {
            ServerRegistrationConfirmation serverRegistrationConfirmation = new(bytes);
            _instanceInfoContainer.LocalClientID = serverRegistrationConfirmation.LocalClientID;
            _instanceInfoContainer.InstanceInfo = serverRegistrationConfirmation.InstanceInfo;

            //Ready for syncing=======================================



            //TODO PlayerSyncService needs to do SendMessage here
            //_localPlayerSyncer.OnAvatarAppearanceUpdatedLocally += (BytesAndProtocol bytesAndProtocol) => _commsHandler.SendMessage(bytesAndProtocol.Bytes, InstanceNetworkingMessageCodes.UpdateAvatarPresentation, bytesAndProtocol.Protocol);
            //_localPlayerSyncer.OnPlayerStateUpdatedLocally += (BytesAndProtocol bytesAndProtocol) => _commsHandler.SendMessage(bytesAndProtocol.Bytes, InstanceNetworkingMessageCodes.PlayerState, bytesAndProtocol.Protocol);

            _remotePlayerSyncer = new(_instanceInfoContainer, _interactorContainer, _playerService); //only receives
            _commsHandler.OnReceiveRemotePlayerState += _remotePlayerSyncer.HandleReceiveRemotePlayerState;

            _connectionStateDebugWrapper.ConnectionState = ConnectionState.Connected;
            OnConnectedToInstance?.Invoke();
        }

        private void HandleReceiveInstanceInfoUpdate(byte[] bytes) =>  _instanceInfoContainer.InstanceInfo = new InstancedInstanceInfo(bytes);

        public void NetworkUpdate() 
        {
            _commsHandler.MainThreadUpdate();

            if (IsConnectedToServer)
            {
                _worldStateSyncer.NetworkUpdate();
                _localPlayerSyncer.NetworkUpdate();
                _remotePlayerSyncer.NetworkUpdate();
            }
        }

        public void ReceivePingFromHost() {} //TODO

        public void DisconnectFromServer() => _commsHandler.DisconnectFromServer();

        private void HandleDisconnectFromServer() 
        {
            _worldStateSyncer.TearDown();
            _localPlayerSyncer.TearDown();
            _remotePlayerSyncer.TearDown();

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
