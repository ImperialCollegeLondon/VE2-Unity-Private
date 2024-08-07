using DarkRift;
using DarkRift.Client;
using DarkRift.Client.Unity;
using Sirenix.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using ViRSE.PluginRuntime;
using static ViRSE.NetworkUtils;
using DRMessageReader = DarkRift.DarkRiftReader;
using DRMessageWrapper = DarkRift.Message;

namespace ViRSE.FrameworkRuntime
{
    public class DarkRiftCommsService : MonoBehaviour, IPrimaryServerCommsService, IPluginWorldStateCommsHandler
    {
        public bool IsReadyToTransmit { get; private set; } = false;

        #region PrimaryServerService Interface
        public event Action<byte[]> OnReceiveNetcodeConfirmation;
        public event Action<byte[]> OnReceiveServerRegistrationConfirmation;
        public event Action<byte[]> OnReceivePopulationUpdate;
        public event Action OnDisconnectedFromServer;
        #endregion

        #region PluginSyncService Interface 
        public event Action<byte[]> OnReceiveWorldStateSyncableBundle;
        public event Action<byte[]> OnReceiveRemotePlayerState;
        public event Action<byte[]> OnReceiveInstantMessage;
        #endregion

        private UnityClient _drClient;

        public void ConnectToServer(ServerType serverType, UnityClient drClient) //TODO, serverType should be global
        {
            _drClient = drClient;
            //_drClient = gameObject.AddComponent<UnityClient>();

            string ipAddress = serverType switch
            {
                ServerType.Local => "127.0.0.1",
                ServerType.Test => "127.0.0.2",
                ServerType.Prod => "127.0.0.3",
                _ => throw new ArgumentOutOfRangeException(nameof(serverType), serverType, "Problem when registering with server, check ServerType")
            };

            Debug.Log($"Try connect to {ipAddress}:4296");

            _drClient.MessageReceived += OnMessageReceived;
            _drClient.Connect(ipAddress, 4296, false);
        }

        private void OnMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            Message messageWrapper = e.GetMessage();
            NetworkUtils.MessageCodes receivedMessageCode = (NetworkUtils.MessageCodes)messageWrapper.Tag;

            using DRMessageReader reader = e.GetMessage().GetReader();
            byte[] bytes = reader.ReadBytes();

            switch (receivedMessageCode)
            {
                case NetworkUtils.MessageCodes.NetcodeVersionConfirmation:
                    OnReceiveNetcodeConfirmation?.Invoke(bytes);
                    break;
                case NetworkUtils.MessageCodes.ServerRegistrationConfirmation:
                    IsReadyToTransmit = true;
                    OnReceiveServerRegistrationConfirmation?.Invoke(bytes);
                    break;
                case NetworkUtils.MessageCodes.PopulationInfo:
                    OnReceivePopulationUpdate?.Invoke(bytes);
                    break;
                case NetworkUtils.MessageCodes.WorldstateSyncableBundle:
                    //Debug.Log("rec worldstate");
                    OnReceiveWorldStateSyncableBundle?.Invoke(bytes);
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

        public void SendPingToHost()
        {

        }

        public void SendPingReplyToNonHost()
        {

        }

        public void SendWorldStateBundle(byte[] bundleAsBytes, TransmissionProtocol transmissionProtocol)
        {
            //Debug.Log("Send world state");

            RawBytesMessage message = new(bundleAsBytes);
            SendMode sendMode = transmissionProtocol == TransmissionProtocol.TCP?
                SendMode.Reliable : 
                SendMode.Unreliable;

            using (DRMessageWrapper messageWrapper = DRMessageWrapper.Create((ushort)MessageCodes.WorldstateSyncableBundle, message))
            {
                _drClient.SendMessage(messageWrapper, sendMode);
            }
        }

        public void SendLocalPlayerState(byte[] bytes)
        {

        }

        public void SendInstantMessage(byte[] bytes)
        {

        }

        public void SendWorldStateSnapshot(byte[] bytes)
        {
            throw new NotImplementedException();
        }

        public void SendServerRegistrationRequest(byte[] bytes)
        {
            RawBytesMessage message = new(bytes);

            using (DRMessageWrapper messageWrapper = DRMessageWrapper.Create((ushort)MessageCodes.ServerRegistrationRequest, message))
            {
                _drClient.SendMessage(messageWrapper, SendMode.Reliable);
            }
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

