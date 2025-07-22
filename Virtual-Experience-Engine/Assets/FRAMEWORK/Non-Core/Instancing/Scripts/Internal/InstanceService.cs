using System;
using UnityEngine;
using System.Net;
using static VE2.NonCore.Platform.API.PlatformPublicSerializables;
using VE2.Core.Player.API;
using VE2.NonCore.Instancing.API;
using static VE2.NonCore.Instancing.Internal.InstanceSyncSerializables;
using VE2.Common.Shared;
using VE2.Core.UI.API;
using VE2.Common.API;
using static VE2.Core.Player.API.PlayerSerializables;
using UnityEngine.Events;
using System.Collections.Generic;

namespace VE2.NonCore.Instancing.Internal
{
    internal static class InstanceServiceFactory
    {
        internal static InstanceService Create(bool connectAutomatically, 
            ConnectionStateWrapper connectionStateDebugWrapper, ServerConnectionSettings serverConnectionSettings, InstanceServiceConfig serviceConfig, InstanceCode instanceCode, ArtificialLatencySettings artificialLatencySettings) 
        {
            InstanceNetworkingCommsHandler commsHandler = new(new DarkRift.Client.DarkRiftClient());

            return new InstanceService(
                commsHandler, 
                VE2API.LocalClientIdWrapper as ILocalClientIDWrapperWritable, 
                VE2API.LocalAdminIndicator,
                connectionStateDebugWrapper,
                VE2API.InteractorContainer,
                VE2API.Player as IPlayerServiceInternal,
                VE2API.PrimaryUIService as IPrimaryUIServiceInternal,
                connectAutomatically,
                serverConnectionSettings,
                instanceCode,
                serviceConfig,
                artificialLatencySettings,
                VE2API.WorldStateSyncableContainer,
                VE2API.LocalPlayerSyncableContainer);
        }
    }

    internal class InstanceService : IInstanceServiceInternal
    {
        //TODO, should take some config for events like "OnBecomeHost", "OnLoseHost"

        #region Public interfaces
        public bool IsConnectedToServer => _connectionStateWrapper.ConnectionState == ConnectionState.Connected;
        public UnityEvent<ushort> OnConnectedToInstance => _config.OnConnectedToInstance;
        public UnityEvent<ushort> OnDisconnectedFromInstance => _config.OnDisconnectedFromInstance;
        public void ConnectToInstance() => ConnectToServer();
        public void DisconnectFromInstance() => DisconnectFromServer();

        public ushort LocalClientID => _instanceInfoContainer.LocalClientID;

        public bool IsHost => _instanceInfoContainer.IsHost;
        public ushort HostID => _instanceInfoContainer.HostID;

        public UnityEvent OnBecomeHost => _config.OnBecomeHost;
        public UnityEvent OnBecomeNonHost => _config.OnBecomeNonHost;

        public int NumberOfClientsInCurrentInstance => ClientIDsInCurrentInstance.Count;
        public List<ushort> ClientIDsInCurrentInstance => _instanceInfoContainer.ClientIDsInCurrentInstance;
        public UnityEvent<ushort> OnRemoteClientJoinedInstance => _config.OnRemoteClientJoinedInstance;
        public UnityEvent<ushort> OnRemoteClientLeftInstance => _config.OnRemoteClientLeftInstance;

        public float Ping => _pingSyncer.Ping;
        public int SmoothPing => _pingSyncer.SmoothPing;
        //public event Action<int> OnPingUpdate { add => _pingSyncer.OnPingUpdate += value; remove => _pingSyncer.OnPingUpdate -= value; }

        public IClientIDWrapper GetClientIDForAvatarGameObject(GameObject avatarGameObject)
        {
            if (avatarGameObject == null)
            {
                Debug.LogError("GetClientIDForAvatarGameObject: GameObject is null.");
                return null;
            }

            //We could search upwards, or we could just look at the name?

            //gameobject names will end in _clientID
            if (avatarGameObject.name.LastIndexOf('_') >= 0 &&
                ushort.TryParse(avatarGameObject.name[(avatarGameObject.name.LastIndexOf('_') + 1)..], out ushort clientID))
            {
                return new ClientIDWrapper(clientID, clientID == LocalClientID);
            }
            else
            {
                Debug.LogError($"Tried to get client ID for avatar GameObject, but given GameObject ({avatarGameObject.name}) does not appear to be an avatar. Please note, VE2 must control the avatar GameObject names, please do not change them.");
                return null;
            }
        }
        #endregion

        #region Internal interfaces
        public void SendInstantMessage(string id, object message) => _instantMessageRouter.SendInstantMessage(id, message);
        public void RegisterInstantMessageHandler(string id, IInstantMessageHandlerInternal instantMessageHandler) => _instantMessageRouter.RegisterInstantMessageHandler(id, instantMessageHandler);
        public void DeregisterInstantMessageHandler(string id) => _instantMessageRouter.DeregisterInstantMessageHandler(id);
        public event Action OnBecomeHostInternal;
        public event Action OnBecomeNonHostInternal;
        #endregion

        #region Temp debug interfaces //TODO: remove once UI is in 
        public InstancedInstanceInfo InstanceInfo => _instanceInfoContainer.InstanceInfo;

        public event Action<InstancedInstanceInfo> OnInstanceInfoChanged { 
            add => _instanceInfoContainer.OnInstanceInfoChanged += value; 
            remove => _instanceInfoContainer.OnInstanceInfoChanged -= value; 
        }
        #endregion


        private bool _shouldConnectToServer = false;
        private float _timeOfLastConnectionAttempt = 0f;
        private float _timeBetweenConnectionAttempts = 5f;

        private readonly InstanceServiceConfig _config;
        private readonly IPluginSyncCommsHandler _commsHandler;
        private readonly ConnectionStateWrapper _connectionStateWrapper;
        private readonly IPlayerServiceInternal _playerService;
        private readonly IPrimaryUIServiceInternal _primaryUIService;
        private readonly HandInteractorContainer _interactorContainer;
        private readonly InstanceInfoContainer _instanceInfoContainer;
        private readonly ServerConnectionSettings _serverSettings;
        private readonly InstanceCode _instanceCode;

        internal readonly WorldStateSyncer _worldStateSyncer;
        internal readonly LocalPlayerSyncer _localPlayerSyncer;
        internal readonly RemotePlayerSyncer _remotePlayerSyncer;
        internal PingSyncer _pingSyncer;
        internal InstantMessageRouter _instantMessageRouter;

        public InstanceService(IPluginSyncCommsHandler commsHandler, ILocalClientIDWrapperWritable localClientIDWrapper, ILocalAdminIndicator localAdminIndicatorWrapper, ConnectionStateWrapper connectionStateDebugWrapper,
            HandInteractorContainer interactorContainer, IPlayerServiceInternal playerServiceInternal, IPrimaryUIServiceInternal primaryUIService,
            bool connectAutomatically, ServerConnectionSettings serverSettings, InstanceCode instanceCode, InstanceServiceConfig config, ArtificialLatencySettings artificialLatencySettings, 
            IWorldStateSyncableContainer worldStateSyncableContainer, ILocalPlayerSyncableContainer localPlayerSyncableContainer)

        {
            _config = config;
            _commsHandler = commsHandler;
            _connectionStateWrapper = connectionStateDebugWrapper;
            _interactorContainer = interactorContainer;
            _playerService = playerServiceInternal;
            _primaryUIService = primaryUIService;
            _serverSettings = serverSettings;
            _instanceCode = instanceCode;

            _commsHandler.ArtificialLatencySettings = artificialLatencySettings;

            _instanceInfoContainer = new(localClientIDWrapper);

            _commsHandler.OnReceiveNetcodeConfirmation += HandleReceiveNetcodeVersion;
            _commsHandler.OnReceiveServerRegistrationConfirmation += HandleReceiveServerRegistrationConfirmation;
            _commsHandler.OnReceiveInstanceInfoUpdate += HandleReceiveInstanceInfoUpdate;
            _commsHandler.OnDisconnectedFromServer += HandleDisconnectFromServer;

            _worldStateSyncer = new(_commsHandler, _instanceInfoContainer, worldStateSyncableContainer); //receives and transmits
            _localPlayerSyncer = new(_commsHandler, _instanceInfoContainer, localPlayerSyncableContainer, localAdminIndicatorWrapper); //only transmits
            _remotePlayerSyncer = new(_commsHandler, _instanceInfoContainer, _interactorContainer, _playerService); //only receives
            _pingSyncer = new(_commsHandler, _instanceInfoContainer); //receives and transmits
            _instantMessageRouter = new(_commsHandler);

            _primaryUIService?.SetInstanceCodeText(_instanceCode.ToString());

            if (connectAutomatically)
                ConnectToServer();
        }

        private void ConnectToServer() //TODO: expose to plugin for delayed connection?
        {
            if (_connectionStateWrapper.ConnectionState == ConnectionState.Connecting &&
                _connectionStateWrapper.ConnectionState == ConnectionState.Connected)
            {
                Debug.LogWarning("Attempted to connect to server while already connected or connecting");
                return;
            }

            _connectionStateWrapper.ConnectionState = ConnectionState.Connecting;
            _remotePlayerSyncer.ToggleAvatarsTransparent(false);

            Debug.Log("Try connect to instance... " + _serverSettings.ServerAddress);
            _shouldConnectToServer = true;
            AttemptConnection();
        }

        private void AttemptConnection()
        {
            if (!IPAddress.TryParse(_serverSettings.ServerAddress, out IPAddress ipAddress))
            {
                Debug.LogError("Could not connect to server, invalid IP address");
                _shouldConnectToServer = false;
            }
            else
            {
                _timeOfLastConnectionAttempt = Time.time;
                _commsHandler.ConnectToServerAsync(ipAddress, _serverSettings.ServerPort);
            }
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
            bool usingFrameworkAvatar = _playerService != null;
            InstancedAvatarAppearance overridableAvatarAppearance = usingFrameworkAvatar ? _playerService.InstancedAvatarAppearance : new();
            AvatarAppearanceWrapper avatarAppearanceWrapper = new(usingFrameworkAvatar, overridableAvatarAppearance);

            //We also send the LocalClientID here, this will either be maxvalue (if this is our first time connecting, the server will give us a new ID)..
            //..or it'll be the ID we we're restored after a disconnect (if we're reconnecting, the server will use the ID we provide)
            ServerRegistrationRequest serverRegistrationRequest = new(_instanceCode, _instanceInfoContainer.LocalClientID, avatarAppearanceWrapper);
            byte[] serverRegistrationRequestBytes = serverRegistrationRequest.Bytes;
            _commsHandler.SendMessage(serverRegistrationRequestBytes, InstanceNetworkingMessageCodes.ServerRegistrationRequest, TransmissionProtocol.TCP);
        }

        private void HandleReceiveServerRegistrationConfirmation(byte[] bytes)
        {
            ServerRegistrationConfirmation serverRegistrationConfirmation = new(bytes);
            _instanceInfoContainer.LocalClientID = serverRegistrationConfirmation.LocalClientID;
            _connectionStateWrapper.ConnectionState = ConnectionState.Connected;

            try
            {
                OnConnectedToInstance?.Invoke(_instanceInfoContainer.LocalClientID);
            }
            catch (Exception e)
            {
                Debug.LogError("ERROR when emitting OnConnectedToInstance: " + e.Message + " - " + e.StackTrace);
            }

            HandleReceiveInstanceInfoUpdate(serverRegistrationConfirmation.InstanceInfo.Bytes);
        }

        private void HandleReceiveInstanceInfoUpdate(byte[] bytes)
        {
            bool infoNullBeforeUpdate = _instanceInfoContainer.InstanceInfo == null;

            bool wasHostBeforeUpdate = _instanceInfoContainer.IsHost;
            List<ushort> remoteClientIDsBeforeUpdate = new(ClientIDsInCurrentInstance);
            remoteClientIDsBeforeUpdate.Remove(_instanceInfoContainer.LocalClientID);

            _instanceInfoContainer.InstanceInfo = new InstancedInstanceInfo(bytes);

            bool isHostAfterUpdate = _instanceInfoContainer.IsHost;
            List<ushort> remoteClientIDsAfterUpdate = new(ClientIDsInCurrentInstance);
            remoteClientIDsAfterUpdate.Remove(_instanceInfoContainer.LocalClientID);

            if ((!wasHostBeforeUpdate && isHostAfterUpdate) || (infoNullBeforeUpdate && isHostAfterUpdate))
            {
                OnBecomeHostInternal?.Invoke();
                try
                {
                    OnBecomeHost?.Invoke();
                }
                catch (Exception e)
                {
                    Debug.LogError("ERROR when emitting OnBecomeHost: " + e.Message + " - " + e.StackTrace);
                }
            }
            else if ((wasHostBeforeUpdate && !isHostAfterUpdate) || (infoNullBeforeUpdate && !isHostAfterUpdate))
            {
                OnBecomeNonHostInternal?.Invoke();
                try
                {
                    OnBecomeNonHost?.Invoke();
                }
                catch (Exception e)
                {
                    Debug.LogError("ERROR when emitting OnLoseHost: " + e.Message + " - " + e.StackTrace);
                }
            }

            foreach (ushort remoteClientID in remoteClientIDsAfterUpdate)
            {
                if (!remoteClientIDsBeforeUpdate.Contains(remoteClientID))
                {
                    try
                    {
                        OnRemoteClientJoinedInstance?.Invoke(remoteClientID);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("ERROR when emitting OnRemoteClientJoinedInstance: " + e.Message + " - " + e.StackTrace);
                    }
                }
            }
            
            foreach (ushort remoteClientID in remoteClientIDsBeforeUpdate)
            {
                if (!remoteClientIDsAfterUpdate.Contains(remoteClientID))
                {
                    try
                    {
                        OnRemoteClientLeftInstance?.Invoke(remoteClientID);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("ERROR when emitting OnRemoteClientLeftInstance: " + e.Message + " - " + e.StackTrace);
                    }
                }
            }
        }

        internal void HandleUpdate() 
        {
            _commsHandler.MainThreadUpdate();

            if (!IsConnectedToServer && _shouldConnectToServer && Time.time - _timeOfLastConnectionAttempt > _timeBetweenConnectionAttempts)
            {
                AttemptConnection();
            }
            else if (IsConnectedToServer)
            {
                _worldStateSyncer.NetworkUpdate();
                _localPlayerSyncer.NetworkUpdate();
                _remotePlayerSyncer.NetworkUpdate();
                _pingSyncer.NetworkUpdate();
            }
        }

        //TODO - move to ping syncer? 
        public void HandleReceivePingMessage(byte[] bytes)
        {
            _pingSyncer.HandleReceivePingMessage(bytes);
        }

        private void DisconnectFromServer() 
        {
            if (_connectionStateWrapper.ConnectionState != ConnectionState.Connected)
            {
                Debug.LogWarning("Attempted to disconnect to server while not connected");
                return;
            }

            _shouldConnectToServer = false;
            _commsHandler.DisconnectFromServer();
        }

        private void HandleDisconnectFromServer()
        {
            Debug.Log("Disconnected from server");

            _remotePlayerSyncer.ToggleAvatarsTransparent(true);

            _connectionStateWrapper.ConnectionState = ConnectionState.LostConnection;
            
            try
            {
                OnDisconnectedFromInstance?.Invoke(_instanceInfoContainer.LocalClientID);
            }
            catch (Exception e)
            {
                Debug.LogError("ERROR when emitting OnDisconnectedFromInstance: " + e.Message + " - " + e.StackTrace);
            }
        }

        internal void TearDown()
        {
            if (IsConnectedToServer)
            {
                _worldStateSyncer.TearDown();
                _localPlayerSyncer.TearDown();
                _remotePlayerSyncer.TearDown();
                _pingSyncer.TearDown();
                _commsHandler.DisconnectFromServer();
            }

            _commsHandler.OnReceiveNetcodeConfirmation -= HandleReceiveNetcodeVersion;
            _commsHandler.OnReceiveServerRegistrationConfirmation -= HandleReceiveServerRegistrationConfirmation;
            _commsHandler.OnReceiveInstanceInfoUpdate -= HandleReceiveInstanceInfoUpdate;
            _commsHandler.OnDisconnectedFromServer -= HandleDisconnectFromServer;

            _connectionStateWrapper.ConnectionState = ConnectionState.NotYetConnected;
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
        public readonly ILocalClientIDWrapperWritable LocalClientIdWrapper;
        public ushort LocalClientID { get => LocalClientIdWrapper.Value; set => LocalClientIdWrapper.SetValue(value); }
        public List<ushort> ClientIDsInCurrentInstance => InstanceInfo?.ClientInfos.Keys != null ? new List<ushort>(InstanceInfo.ClientInfos.Keys) : new List<ushort>();

        public event Action OnBecomeHost;
        public event Action OnLoseHost;

        public bool IsHost => InstanceInfo == null || InstanceInfo.HostID == LocalClientID;
        public ushort HostID => InstanceInfo.HostID;

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

        public InstanceInfoContainer(ILocalClientIDWrapperWritable localClientIdWrapper)
        {
            LocalClientIdWrapper = localClientIdWrapper;
        }
    }
}
