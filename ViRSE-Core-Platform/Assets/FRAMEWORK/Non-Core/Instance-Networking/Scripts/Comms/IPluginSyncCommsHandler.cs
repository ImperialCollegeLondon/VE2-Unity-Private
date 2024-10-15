using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using UnityEngine.Events;
using ViRSE;
using ViRSE.Core.Shared;

namespace ViRSE.InstanceNetworking
{
    public interface IPluginSyncCommsHandler
    {
        public bool IsReadyToTransmit { get; }

        public event Action<byte[]> OnReceiveNetcodeConfirmation;
        public event Action<byte[]> OnReceiveServerRegistrationConfirmation;
        public event Action<byte[]> OnReceiveInstanceInfoUpdate;
        public event Action OnDisconnectedFromServer;
        public event Action<byte[]> OnReceiveWorldStateSyncableBundle;
        public event Action<byte[]> OnReceiveRemotePlayerState;
        public event Action<byte[]> OnReceiveInstantMessage;

        public void ConnectToServer(IPAddress ipAddress, int portNumber);
        public void SendMessage(byte[] messageAsBytes, InstanceSyncSerializables.InstanceNetworkingMessageCodes messageCode, TransmissionProtocol transmissionProtocol);
        public void MainThreadUpdate();
        public void DisconnectFromServer();
    }
}
