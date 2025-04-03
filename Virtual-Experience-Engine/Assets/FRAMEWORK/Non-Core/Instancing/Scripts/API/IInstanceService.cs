using System;

namespace VE2.NonCore.Instancing.API
{
    public interface IInstanceService
    {
        public ushort LocalClientID { get; }

        public bool IsHost { get; }

        public event Action OnBecomeHost;
        public event Action OnLoseHost;

        public ushort HostID { get; }

        public bool IsConnectedToServer { get; }
        public event Action OnConnectedToInstance;
        public event Action OnDisconnectedFromInstance;

        //TODO - remove these two?
        public void ConnectToInstance();
        public void DisconnectFromInstance();

        public float Ping { get; }
        public int SmoothPing { get; }
    }
}
