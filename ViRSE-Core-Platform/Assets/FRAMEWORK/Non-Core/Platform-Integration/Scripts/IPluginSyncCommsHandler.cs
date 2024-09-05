using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using UnityEngine.Events;
using ViRSE;
using ViRSE.Core.Shared;

namespace ViRSE.FrameworkRuntime
{
    public interface IPlatformCommsHandler
    {
        public bool IsReadyToTransmit { get; }

        public void ConnectToServer(IPAddress ipAddress, int portNumber);
        public void SendServerRegistrationRequest(byte[] serverRegistrationBytes);
        public void SendInstanceAllocationRequest(byte[] instanceAllocationBytes);

        public event Action<byte[]> OnReceiveNetcodeConfirmation;
        public event Action<byte[]> OnReceiveServerRegistrationConfirmation;
        public event Action<byte[]> OnReceiveGlobalInfoUpdate;
        public event Action OnDisconnectedFromServer;

    }

}

