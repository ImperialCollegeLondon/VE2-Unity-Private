using DarkRift;
using DarkRift.Client;
using System;
using System.Net;
using UnityEngine;
using ViRSE.Core.Shared;
using ViRSE;
using DRMessageReader = DarkRift.DarkRiftReader;
using DRMessageWrapper = DarkRift.Message;
using UnityEditor.Experimental.GraphView;

namespace ViRSE.FrameworkRuntime
{
    public class InstanceNetworkingCommsHandler : IPluginSyncCommsHandler
    {
        private DarkRiftClient _drClient;

        #region PluginSyncService interface
        public bool IsReadyToTransmit { get; private set; }
        public event Action<byte[]> OnReceiveWorldStateSyncableBundle;
        public event Action<byte[]> OnReceiveNetcodeConfirmation;
        public event Action<byte[]> OnReceiveServerRegistrationConfirmation;
        public event Action<byte[]> OnReceiveInstanceInfoUpdate;
        public event Action OnDisconnectedFromServer;
        public event Action<byte[]> OnReceiveRemotePlayerState;
        public event Action<byte[]> OnReceiveInstantMessage;

        public void ConnectToServer(IPAddress ipAddress, int port)
        {
            Debug.Log($"Try connect to {ipAddress}:{port}");
            _drClient.Connect(ipAddress, port, false);
        }

        public void SendServerRegistrationRequest(byte[] bytes)
        {
            RawBytesMessage message = new(bytes);

            using (DRMessageWrapper messageWrapper = DRMessageWrapper.Create((ushort)InstanceSyncNetworkObjects.InstanceNetworkingMessageCodes.ServerRegistrationRequest, message))
            {
                _drClient.SendMessage(messageWrapper, SendMode.Reliable);
            }
        }

        public void SendWorldStateBundle(byte[] bundleAsBytes, TransmissionProtocol transmissionProtocol)
        {
            //Debug.Log("Send world state");

            RawBytesMessage message = new(bundleAsBytes);
            SendMode sendMode = transmissionProtocol == TransmissionProtocol.TCP ?
                SendMode.Reliable :
                SendMode.Unreliable;

            using (DRMessageWrapper messageWrapper = DRMessageWrapper.Create((ushort)InstanceSyncNetworkObjects.InstanceNetworkingMessageCodes.WorldstateSyncableBundle, message))
            {
                _drClient.SendMessage(messageWrapper, sendMode);
            }
        }

        public void SendWorldStateSnapshot(byte[] bytes)
        {
            throw new NotImplementedException();
        }

        public void SendLocalPlayerState(byte[] bytes)
        {

        }

        public void SendInstantMessage(byte[] bytes)
        {

        }

        public void SendPingToHost()
        {

        }

        public void SendPingReplyToNonHost()
        {

        }
        #endregion

        public InstanceNetworkingCommsHandler(DarkRiftClient drClient)
        {
            _drClient = drClient;
            _drClient.MessageReceived += OnMessageReceived;
        }

        private void OnMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            Message messageWrapper = e.GetMessage();
            InstanceSyncNetworkObjects.InstanceNetworkingMessageCodes receivedMessageCode = (InstanceSyncNetworkObjects.InstanceNetworkingMessageCodes)messageWrapper.Tag;

            using DRMessageReader reader = e.GetMessage().GetReader();
            byte[] bytes = reader.ReadBytes();

            switch (receivedMessageCode)
            {
                case InstanceSyncNetworkObjects.InstanceNetworkingMessageCodes.NetcodeVersionConfirmation:
                    OnReceiveNetcodeConfirmation?.Invoke(bytes);
                    break;
                case InstanceSyncNetworkObjects.InstanceNetworkingMessageCodes.ServerRegistrationConfirmation:
                    IsReadyToTransmit = true;
                    OnReceiveServerRegistrationConfirmation?.Invoke(bytes);
                    break;
                case InstanceSyncNetworkObjects.InstanceNetworkingMessageCodes.WorldstateSyncableBundle:
                    //Debug.Log("rec worldstate");
                    OnReceiveWorldStateSyncableBundle?.Invoke(bytes);
                    break;
                case InstanceSyncNetworkObjects.InstanceNetworkingMessageCodes.InstanceInfo:
                    OnReceiveInstanceInfoUpdate?.Invoke(bytes);
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

