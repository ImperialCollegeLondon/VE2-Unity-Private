using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using UnityEngine.Events;
using ViRSE;
using ViRSE.Core.Shared;

namespace ViRSE.PlatformNetworking
{
    public interface IPlatformCommsHandler
    {
        public bool IsReadyToTransmit { get; }

        public void ConnectToServer(IPAddress ipAddress, int portNumber);
        public void SendMessage(byte[] messageAsBytes, PlatformSerializables.PlatformNetworkingMessageCodes messageCode, TransmissionProtocol transmissionProtocol);
        public void MainThreadUpdate();
        public void DisconnectFromServer();

        public event Action<byte[]> OnReceiveNetcodeConfirmation;
        public event Action<byte[]> OnReceiveServerRegistrationConfirmation;
        public event Action<byte[]> OnReceiveGlobalInfoUpdate;
        public event Action OnDisconnectedFromServer;
    }
}

