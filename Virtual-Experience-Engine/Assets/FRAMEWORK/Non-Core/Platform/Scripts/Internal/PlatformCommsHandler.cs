using DarkRift;
using DarkRift.Client;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using UnityEngine;
using VE2.Common.Shared;
using DRMessageReader = DarkRift.DarkRiftReader;
using DRMessageWrapper = DarkRift.Message;

namespace VE2.NonCore.Platform.Internal
{
    internal class PlatformCommsHandler : IPlatformCommsHandler
    {
        private DarkRiftClient _drClient;
        private readonly Queue<Action> executionQueue = new();

        #region PlatformService interface
        public bool IsReadyToTransmit { get; private set; }
        public event Action<byte[]> OnReceiveNetcodeConfirmation;
        public event Action<byte[]> OnReceiveServerRegistrationConfirmation;
        public event Action<byte[]> OnReceiveGlobalInfoUpdate;
        public event Action OnDisconnectedFromServer;

        public async Task ConnectToServerAsync(IPAddress ipAddress, int port)
        {
            Debug.Log($"Try connect to platform on {ipAddress}:{port}");
            await Task.Run(() => _drClient.Connect(ipAddress, port, false));
        }

        public void SendMessage(byte[] messageAsBytes, PlatformSerializables.PlatformNetworkingMessageCodes messageCode, TransmissionProtocol transmissionProtocol)
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

        public void DisconnectFromServer()
        {
            _drClient.Disconnect();
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

        #endregion

        public PlatformCommsHandler(DarkRiftClient drClient)
        {
            _drClient = drClient;
            _drClient.MessageReceived += OnMessageReceived;
        }

        private void OnMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            Message messageWrapper = e.GetMessage();
            PlatformSerializables.PlatformNetworkingMessageCodes receivedMessageCode = (PlatformSerializables.PlatformNetworkingMessageCodes)messageWrapper.Tag;

            using DRMessageReader reader = e.GetMessage().GetReader();
            byte[] bytes = reader.ReadBytes();

            lock (executionQueue)
            {
                executionQueue.Enqueue(() =>
                {
                    switch (receivedMessageCode)
                    {
                        case PlatformSerializables.PlatformNetworkingMessageCodes.NetcodeVersionConfirmation:
                            OnReceiveNetcodeConfirmation?.Invoke(bytes);
                            break;
                        case PlatformSerializables.PlatformNetworkingMessageCodes.ServerRegistrationResponse:
                            IsReadyToTransmit = true;
                            OnReceiveServerRegistrationConfirmation?.Invoke(bytes);
                            break;
                        case PlatformSerializables.PlatformNetworkingMessageCodes.GlobalInfo:
                            OnReceiveGlobalInfoUpdate?.Invoke(bytes);
                            break;
                    }
                });
            }
        }

        private class RawBytesMessage : IDarkRiftSerializable
        {
            public byte[] Bytes { get; private set; }

            public RawBytesMessage(byte[] bytes) =>  Bytes = bytes;

            public void Serialize(SerializeEvent e) => e.Writer.Write(Bytes);

            public void Deserialize(DeserializeEvent e) => Bytes = e.Reader.ReadBytes();
        }
    }
}

