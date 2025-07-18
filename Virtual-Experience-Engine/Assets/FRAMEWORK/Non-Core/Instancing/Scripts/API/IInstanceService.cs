using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using VE2.Common.Shared;

namespace VE2.NonCore.Instancing.API
{
    public interface IInstanceService
    {
        public bool IsConnectedToServer { get; }
        public UnityEvent<ushort> OnConnectedToInstance { get; }
        public UnityEvent<ushort> OnDisconnectedFromInstance { get; }
        //TODO - Review if we're keeping these 
        public void ConnectToInstance();
        public void DisconnectFromInstance();

        public ushort LocalClientID { get; }

        public bool IsHost { get; }
        public ushort HostID { get; }

        public UnityEvent OnBecomeHost { get; }
        public UnityEvent OnBecomeNonHost { get; }

        public int NumberOfClientsInCurrentInstance { get; }
        public List<ushort> ClientIDsInCurrentInstance { get; }
        public UnityEvent<ushort> OnRemoteClientJoinedInstance { get; }
        public UnityEvent<ushort> OnRemoteClientLeftInstance { get; }

        public float Ping { get; }
        public int SmoothPing { get; }

        public IClientIDWrapper GetClientIDForAvatarGameObject(GameObject avatarGameObject);
    }
}
