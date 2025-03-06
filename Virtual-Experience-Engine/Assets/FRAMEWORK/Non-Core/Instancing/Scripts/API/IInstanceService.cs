using System;

namespace VE2.NonCore.Instancing.API
{
    public interface IInstanceService
    {
        public ushort LocalClientID { get; }

        public bool IsHost { get; }
        public event Action<ushort> OnHostChanged; //TODO - maybe also want some convenient OnBecomeHost and OnLostHost?

        public bool IsConnectedToServer { get; }
        public event Action OnConnectedToInstance;
        public event Action OnDisconnectedFromInstance;
        public void ConnectToInstance();
        public void DisconnectFromInstance();
    }
}
