using System;
using static VE2.Common.CommonSerializables;

namespace VE2.Common //TODO break into different files
{
    public interface IInstanceService
    {
        public ushort LocalClientID { get; }
        public bool IsHost { get; }
        public bool IsConnectedToServer { get; }
        public event Action OnConnectedToInstance;
        public event Action OnDisconnectedFromInstance;
    }
}
