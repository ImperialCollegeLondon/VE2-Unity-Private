using DarkRift.Client;
using DarkRift.Client.Unity;
using Sirenix.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using ViRSE.PluginRuntime;

namespace ViRSE.FrameworkRuntime
{
    public class DarkRiftCommsService : MonoBehaviour, IPrimaryServerCommsService, IPluginSyncCommsHandler
    {
        public bool IsConnectedToServer { get; private set; } = false;

        //TODO review
        public UnityEvent<ServerRegistration> OnRegisterWithServer { get; private set; } = new();
        public UnityEvent OnDisconnectedFromServer { get; private set; } = new();
        public UnityEvent<Version> OnNetcodeVersionMismatch { get; private set; } = new();

        #region PluginSyncService Interface 
        //public delegate void BytesEventHandler(byte[] bytes);
        public event Action<byte[]> OnWorldStateBundleBytesReceived;
        public event Action<byte[]> OnRemotePlayerStateBytesReceived;
        public event Action<byte[]> OnInstantMessageBytesReceived;
        public event Action<byte[]> OnReceivePopulationUpdate;
        #endregion

        private UnityClient _drClient;

        public void RegisterWithServer(ServerType serverType) //TODO, serverType should be global
        {
            _drClient = gameObject.AddComponent<UnityClient>();

            string ipAddress = serverType switch
            {
                ServerType.Local => "127.0.0.1",
                ServerType.Test => "127.0.0.2",
                ServerType.Prod => "127.0.0.3",
                _ => throw new ArgumentOutOfRangeException(nameof(serverType), serverType, "Problem when registering with server, check ServerType")
            };

            _drClient.Connect(ipAddress, 4296, false);
        }

        private void OnMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            DarkRift.Message messageWrapper = e.GetMessage();
            MessageCode receivedMessageCode = (MessageCode)messageWrapper.Tag;

            if (receivedMessageCode == MessageCode.WorldStateBundle)
            {
                byte[] receivedBytes = { }; //TODO extract from message...
                OnWorldStateBundleBytesReceived?.Invoke(receivedBytes);
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

        public void SendWorldStateBundleBytes(byte[] bundleAsBytes, TransmissionProtocol transmissionProtocol)
        {

        }

        public void SendLocalPlayerStateBytes(byte[] bytes)
        {

        }

        public void SendInstantMessageBytes(byte[] bytes)
        {

        }

        public void SendTCPWorldStateSnapshotBytes(byte[] bytes)
        {
            throw new NotImplementedException();
        }

        private enum MessageCode
        {
            WorldStateBundle,
            PlayerState
        }
    }
}
