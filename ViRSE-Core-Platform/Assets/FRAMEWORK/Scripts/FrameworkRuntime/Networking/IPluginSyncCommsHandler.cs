using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using ViRSE;

namespace ViRSE.FrameworkRuntime
{
    public interface IPluginSyncCommsHandler
    {
        public bool IsConnectedToServer { get; }

        public event Action<byte[]> OnWorldStateBundleBytesReceived;
        public event Action<byte[]> OnRemotePlayerStateBytesReceived;
        public event Action<byte[]> OnInstantMessageBytesReceived;

        public void SendPingToHost();

        public void SendPingReplyToNonHost();

        public void SendWorldStateBundleBytes(byte[] bytes, TransmissionProtocol transmissionProtocol);

        public void SendTCPWorldStateSnapshotBytes(byte[] bytes);

        public void SendLocalPlayerStateBytes(byte[] bytes);

        public void SendInstantMessageBytes(byte[] bytes);
    }
}