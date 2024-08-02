using DarkRift.Client;
using DarkRift.Client.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace ViRSE
{
    public class DarkRiftCommsService : MonoBehaviour, IPrimaryServiceCommsService
    {
        public UnityEvent<ServerRegistration> OnRegisterWithServer { get; private set; } = new();
        public UnityEvent OnDisconnectedFromServer { get; private set; } = new();
        public UnityEvent<Version> OnNetcodeVersionMismatch { get; private set; } = new();

        private UnityClient drClient;

        public void RegisterWithServer(ServerType serverType)
        {
            drClient = gameObject.AddComponent<UnityClient>();

            string ipAddress = serverType switch
            {
                ServerType.Local => "127.0.0.1",
                ServerType.Test => "127.0.0.2",
                ServerType.Prod => "127.0.0.3",
                _ => throw new ArgumentOutOfRangeException(nameof(serverType), serverType, "Problem when registering with server, check ServerType")
            };

            drClient.Connect(ipAddress, 4296, false);
        }

        private void OnMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            DarkRift.Message messageWrapper = e.GetMessage();
            MessageCode receivedMessageCode = (MessageCode)messageWrapper.Tag;



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

        private class Send
        {

        }

        private class Receive
        {

        }

        private enum MessageCode
        {

        }
    }
}
