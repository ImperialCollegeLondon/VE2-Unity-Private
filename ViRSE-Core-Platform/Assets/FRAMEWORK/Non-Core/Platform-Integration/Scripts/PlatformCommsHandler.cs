using DarkRift;
using DarkRift.Client;
using System;
using System.Net;
using UnityEngine;
using UnityEngine.XR;
using ViRSE.Core.Shared;
using DRMessageReader = DarkRift.DarkRiftReader;
using DRMessageWrapper = DarkRift.Message;

namespace ViRSE.FrameworkRuntime
{
    public class PlatformCommsHandler : IPlatformCommsHandler
    {
        private DarkRiftClient _drClient;

        #region PlatformService interface
        public bool IsReadyToTransmit { get; private set; }
        public event Action<byte[]> OnReceiveNetcodeConfirmation;
        public event Action<byte[]> OnReceiveServerRegistrationConfirmation;
        public event Action<byte[]> OnReceiveGlobalInfoUpdate;
        public event Action OnDisconnectedFromServer;

        public void ConnectToServer(IPAddress ipAddress, int port)
        {
            Debug.Log($"Try connect to {ipAddress}:{port}");
            _drClient.Connect(ipAddress, port, false);
        }

        public void SendServerRegistrationRequest(byte[] bytes)
        {
            RawBytesMessage message = new(bytes);

            using (DRMessageWrapper messageWrapper = DRMessageWrapper.Create((ushort)PlatformNetworkObjects.PlatformNetworkingMessageCodes.ServerRegistrationRequest, message))
            {
                _drClient.SendMessage(messageWrapper, SendMode.Reliable);
            }
        }

        public void SendInstanceAllocationRequest(byte[] bytes)
        {
            RawBytesMessage message = new(bytes);

            using (DRMessageWrapper messageWrapper = DRMessageWrapper.Create((ushort)PlatformNetworkObjects.PlatformNetworkingMessageCodes.InstanceAllocationRequest, message))
            {
                _drClient.SendMessage(messageWrapper, SendMode.Reliable);
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
            PlatformNetworkObjects.PlatformNetworkingMessageCodes receivedMessageCode = (PlatformNetworkObjects.PlatformNetworkingMessageCodes)messageWrapper.Tag;

            using DRMessageReader reader = e.GetMessage().GetReader();
            byte[] bytes = reader.ReadBytes();

            switch (receivedMessageCode)
            {
                case PlatformNetworkObjects.PlatformNetworkingMessageCodes.NetcodeVersionConfirmation:
                    OnReceiveNetcodeConfirmation?.Invoke(bytes);
                    break;
                case PlatformNetworkObjects.PlatformNetworkingMessageCodes.ServerRegistrationConfirmation:
                    IsReadyToTransmit = true;
                    OnReceiveServerRegistrationConfirmation?.Invoke(bytes);
                    break;
                case PlatformNetworkObjects.PlatformNetworkingMessageCodes.GlobalInfo:
                    OnReceiveGlobalInfoUpdate?.Invoke(bytes);
                    break;
            }


            //if (receivedMessageCode == MessageCode.HealthCheck)
            //{
            //    Receive.HealthCheck(messageWrapper);
            //}
            //else if (gameObject.activeSelf) //Lets us test connection drops by turning off this GameObject
            //{
            //    if (Application.isEditor && simLatencyMS > Mathf.Epsilon)
            //    {
            //        DOVirtual.DelayedCall(simLatencyMS / 1000f, () =>
            //        {
            //            RouteMessage(messageWrapper, receivedMessageCode);
            //        });
            //    }
            //    else
            //    {
            //        RouteMessage(messageWrapper, receivedMessageCode);
            //    }
            //}
        }

        private class RawBytesMessage : IDarkRiftSerializable
        {
            public byte[] Bytes { get; private set; }

            public RawBytesMessage(byte[] bytes)
            {
                Bytes = bytes;
            }

            public void Serialize(SerializeEvent e)
            {
                e.Writer.Write(Bytes);
            }

            public void Deserialize(DeserializeEvent e)
            {
                Bytes = e.Reader.ReadBytes();
            }
        }
    }
}

