using System;
using System.Net;
using VE2.Common.Shared;

namespace VE2.NonCore.Platform.Internal
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

