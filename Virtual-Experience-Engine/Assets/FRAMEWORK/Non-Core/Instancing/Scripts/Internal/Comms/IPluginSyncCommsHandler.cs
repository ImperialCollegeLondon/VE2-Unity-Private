using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using VE2.Core.Common;

namespace VE2.NonCore.Instancing.Internal
{
    internal interface IPluginSyncCommsHandler
    {
        public bool IsReadyToTransmit { get; }

        public event Action<byte[]> OnReceiveNetcodeConfirmation;
        public event Action<byte[]> OnReceiveServerRegistrationConfirmation;
        public event Action<byte[]> OnReceiveInstanceInfoUpdate;
        public event Action OnDisconnectedFromServer;
        public event Action<byte[]> OnReceiveWorldStateSyncableBundle;
        public event Action<byte[]> OnReceiveRemotePlayerState;
        public event Action<byte[]> OnReceiveInstantMessage;
        public event Action<byte[]> OnReceivePingMessage;

        public Task ConnectToServerAsync(IPAddress ipAddress, int portNumber);
        public void SendMessage(byte[] messageAsBytes, InstanceSyncSerializables.InstanceNetworkingMessageCodes messageCode, TransmissionProtocol transmissionProtocol);
        public void MainThreadUpdate();
        public void DisconnectFromServer();
        public InstanceCommsHandlerConfig InstanceConfig { get; set; }
    }
}
