using DarkRift;
using DarkRift.Client;
using System;
using System.Net;
using DRMessageReader = DarkRift.DarkRiftReader;
using DRMessageWrapper = DarkRift.Message;
using System.Collections.Generic;
using VE2.Common;

namespace VE2.InstanceNetworking
{
    public class InstanceNetworkingCommsHandler : IPluginSyncCommsHandler
    {
        private DarkRiftClient _drClient;
        private readonly Queue<Action> executionQueue = new();

        #region PluginSyncService interface
        public bool IsReadyToTransmit { get; private set; }
        public event Action<byte[]> OnReceiveWorldStateSyncableBundle;
        public event Action<byte[]> OnReceiveNetcodeConfirmation;
        public event Action<byte[]> OnReceiveServerRegistrationConfirmation;
        public event Action<byte[]> OnReceiveInstanceInfoUpdate;
        public event Action OnDisconnectedFromServer;
        public event Action<byte[]> OnReceiveRemotePlayerState;
        public event Action<byte[]> OnReceivePingMessage;
        public event Action<byte[]> OnReceiveInstantMessage;

        public void ConnectToServer(IPAddress ipAddress, int port) => _drClient.Connect(ipAddress, port, false);

        public void SendMessage(byte[] messageAsBytes, InstanceSyncSerializables.InstanceNetworkingMessageCodes messageCode, TransmissionProtocol transmissionProtocol)
        {
            RawBytesMessage message = new(messageAsBytes);
            SendMode sendMode = transmissionProtocol == TransmissionProtocol.TCP ?
                SendMode.Reliable :
                SendMode.Unreliable;

            using (DRMessageWrapper messageWrapper = DRMessageWrapper.Create((ushort)messageCode, message))
            {
                _drClient.SendMessage(messageWrapper, sendMode);
            }
        }

        public void MainThreadUpdate()
        {
            while (executionQueue.Count > 0)
            {
                Action action;
                lock (executionQueue)
                    action = executionQueue.Dequeue();

                action?.Invoke();
            }
        }

        public void DisconnectFromServer() => _drClient.Disconnect();
        #endregion

        public InstanceNetworkingCommsHandler(DarkRiftClient drClient)
        {
            _drClient = drClient;
            _drClient.MessageReceived += HandleMessageReceived;
            _drClient.Disconnected += HandleDisconnected;
        }

        private void HandleMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            Message messageWrapper = e.GetMessage();
            InstanceSyncSerializables.InstanceNetworkingMessageCodes receivedMessageCode = (InstanceSyncSerializables.InstanceNetworkingMessageCodes)messageWrapper.Tag;

            using DRMessageReader reader = e.GetMessage().GetReader();
            byte[] bytes = reader.ReadBytes();

            lock (executionQueue)
            {
                executionQueue.Enqueue(() =>
                {
                    switch (receivedMessageCode)
                    {
                        case InstanceSyncSerializables.InstanceNetworkingMessageCodes.NetcodeVersionConfirmation:
                            OnReceiveNetcodeConfirmation?.Invoke(bytes);
                            break;
                        case InstanceSyncSerializables.InstanceNetworkingMessageCodes.ServerRegistrationConfirmation:
                            IsReadyToTransmit = true;
                            OnReceiveServerRegistrationConfirmation?.Invoke(bytes);
                            break;
                        case InstanceSyncSerializables.InstanceNetworkingMessageCodes.WorldstateSyncableBundle:
                            OnReceiveWorldStateSyncableBundle?.Invoke(bytes);
                            break;
                        case InstanceSyncSerializables.InstanceNetworkingMessageCodes.InstanceInfo:
                            OnReceiveInstanceInfoUpdate?.Invoke(bytes);
                            break;
                        case InstanceSyncSerializables.InstanceNetworkingMessageCodes.PlayerState:
                            OnReceiveRemotePlayerState?.Invoke(bytes);
                            break;
                        case InstanceSyncSerializables.InstanceNetworkingMessageCodes.PingMessage:
                            OnReceivePingMessage?.Invoke(bytes);
                            break;
                    }
                });
            }
        }

        private void HandleDisconnected(object sender, DisconnectedEventArgs e) => OnDisconnectedFromServer?.Invoke();


        private class RawBytesMessage : IDarkRiftSerializable
        {
            public byte[] Bytes { get; private set; }
            public RawBytesMessage(byte[] bytes) => Bytes = bytes;
            public void Serialize(SerializeEvent e) => e.Writer.Write(Bytes);
            public void Deserialize(DeserializeEvent e) => Bytes = e.Reader.ReadBytes();
        }
    }
}

