using System;
using System.Net;
using VE2.Core.Common;
using VE2.Platform.Internal;

namespace VE2.PlatformNetworking
{
    internal interface IPlatformCommsHandler
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

