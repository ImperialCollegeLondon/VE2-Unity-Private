using UnityEngine;
using System.Net;
using static NonCoreCommonSerializables;
using static InstanceSyncSerializables;
using System;
using System.Linq;
using static ViRSE.InstanceNetworking.V_InstanceIntegration;
using ViRSE.Common;

namespace ViRSE.InstanceNetworking
{
    public static class InstanceServiceFactory
    {
        public static InstanceService Create(LocalClientIdWrapper localClientIDWrapper, bool connectAutomatically, ConnectionStateDebugWrapper connectionStateDebugWrapper) 
        {
            InstanceNetworkingCommsHandler commsHandler = new(new DarkRift.Client.DarkRiftClient());
            return new InstanceService(commsHandler, localClientIDWrapper, connectionStateDebugWrapper, ViRSENonCoreServiceLocator.Instance.InstanceNetworkSettingsProvider, connectAutomatically);
        }
    }

    public class InstanceService 
    {
        //TODO, should take some config for events like "OnBecomeHost", "OnLoseHost", maybe also sync frequencies

        public bool IsConnectedToServer => _connectionStateDebugWrapper.ConnectionState == ConnectionState.Connected;
        public event Action OnConnectedToServer;
        public event Action OnDisconnectedFromServer;

        //====================================================================================================
        public ushort LocalClientID => _localClientIdWrapper.LocalClientID;
        public InstancedInstanceInfo InstanceInfo;
        public event Action<InstancedInstanceInfo> OnInstanceInfoChanged;

        public bool IsHost => InstanceInfo.HostID == LocalClientID;

        public event Action<byte[]> OnReceiveWorldStateSyncableBundle 
        {
            add {_commsHandler.OnReceiveWorldStateSyncableBundle += value;} 
            remove {_commsHandler.OnReceiveWorldStateSyncableBundle -= value;}
        }

        public event Action<byte[]> OnReceiveRemotePlayerState 
        {
            add {_commsHandler.OnReceiveRemotePlayerState += value;}
            remove {_commsHandler.OnReceiveRemotePlayerState -= value;}
        }

        public void SendAvatarAppearanceUpdate(byte[] bytes) => 
            _commsHandler.SendMessage(bytes, InstanceNetworkingMessageCodes.UpdateAvatarPresentation, TransmissionProtocol.TCP);

        public void SendPlayerState(byte[] bytes, TransmissionProtocol protocol) =>
            _commsHandler.SendMessage(bytes, InstanceNetworkingMessageCodes.PlayerState, protocol);

        public void SendWorldStateBundle(byte[] bytes, TransmissionProtocol protocol) =>
            _commsHandler.SendMessage(bytes, InstanceNetworkingMessageCodes.WorldstateSyncableBundle, protocol);

        //====================================================================================================

        private readonly IPluginSyncCommsHandler _commsHandler;
        private readonly LocalClientIdWrapper _localClientIdWrapper;
        private readonly ConnectionStateDebugWrapper _connectionStateDebugWrapper;
        private readonly IInstanceNetworkSettingsProvider _networkSettingsProvider;

        public InstanceService(IPluginSyncCommsHandler commsHandler, LocalClientIdWrapper localClientIDWrapper, ConnectionStateDebugWrapper connectionStateDebugWrapper, IInstanceNetworkSettingsProvider instanceNetworkSettingsProvider, bool connectAutomatically)
        {
            _commsHandler = commsHandler;
            _localClientIdWrapper = localClientIDWrapper;
            _connectionStateDebugWrapper = connectionStateDebugWrapper;
            _networkSettingsProvider = instanceNetworkSettingsProvider;

            commsHandler.OnReceiveNetcodeConfirmation += HandleReceiveNetcodeVersion;
            commsHandler.OnReceiveServerRegistrationConfirmation += HandleReceiveServerRegistrationConfirmation;
            commsHandler.OnReceiveInstanceInfoUpdate += HandleReceiveInstanceInfoUpdate;
            _commsHandler.OnDisconnectedFromServer += HandleDisconnectFromServer;

            if (connectAutomatically)
                ConnectToServerOnceDetailsReady();
        }

        public void ConnectToServerOnceDetailsReady() 
        {
            _connectionStateDebugWrapper.ConnectionState = ConnectionState.FetchingConnectionSettings;

            if (_networkSettingsProvider.AreInstanceNetworkingSettingsReady)
                ConnectToServer();
            else
                _networkSettingsProvider.OnInstanceNetworkSettingsReady += ConnectToServer;
        }

        public void ConnectToServer()
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
            ServerRegistrationRequest serverRegistrationRequest = new(_networkSettingsProvider.InstanceNetworkSettings.InstanceCode, LocalClientID);
            _commsHandler.SendMessage(serverRegistrationRequest.Bytes, InstanceNetworkingMessageCodes.ServerRegistrationRequest, TransmissionProtocol.TCP);
        }

        private void HandleReceiveServerRegistrationConfirmation(byte[] bytes)
        {
            ServerRegistrationConfirmation serverRegistrationConfirmation = new(bytes);

            _localClientIdWrapper.LocalClientID = serverRegistrationConfirmation.LocalClientID;
            _connectionStateDebugWrapper.ConnectionState = ConnectionState.Connected;

            HandleReceiveInstanceInfoUpdate(serverRegistrationConfirmation.InstanceInfo);

            OnConnectedToServer?.Invoke();
        }

        private void HandleReceiveInstanceInfoUpdate(byte[] bytes)
        {
            if (!bytes.SequenceEqual(InstanceInfo.Bytes))
                HandleReceiveInstanceInfoUpdate(new InstancedInstanceInfo(bytes));
        }

        private void HandleReceiveInstanceInfoUpdate(InstancedInstanceInfo newInstanceInfo)
        {
            InstanceInfo = newInstanceInfo;
            OnInstanceInfoChanged?.Invoke(InstanceInfo);
        }

        public void NetworkUpdate() 
        {
            _commsHandler.MainThreadUpdate();
        }

        public void ReceivePingFromHost() {} //TODO

        public void DisconnectFromServer() => _commsHandler.DisconnectFromServer();

        private void HandleDisconnectFromServer() 
        {
            _connectionStateDebugWrapper.ConnectionState = ConnectionState.LostConnection;
            OnDisconnectedFromServer?.Invoke();
        }

        public void TearDown()
        {
            _commsHandler.DisconnectFromServer();
            _commsHandler.OnReceiveNetcodeConfirmation -= HandleReceiveNetcodeVersion;
            _commsHandler.OnReceiveServerRegistrationConfirmation -= HandleReceiveServerRegistrationConfirmation;
            _commsHandler.OnReceiveInstanceInfoUpdate -= HandleReceiveInstanceInfoUpdate;
            _commsHandler.OnDisconnectedFromServer -= HandleDisconnectFromServer;
        }
    }
}
