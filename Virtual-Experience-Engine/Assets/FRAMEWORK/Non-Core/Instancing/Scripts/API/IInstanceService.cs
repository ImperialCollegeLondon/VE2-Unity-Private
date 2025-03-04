using System;

namespace VE2.NonCore.Instancing.API
{
    public interface IInstanceService
    {
        public ushort LocalClientID { get; }
        public bool IsHost { get; }
        public bool IsConnectedToServer { get; }
        public event Action OnConnectedToInstance;
        public event Action OnDisconnectedFromInstance;
        public void ConnectToInstance();
        public void DisconnectFromInstance();
    }
}
