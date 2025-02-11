using System;
using static VE2.Common.CommonSerializables;
using VE2.Core.VComponents.InteractableInterfaces;

namespace VE2.Common 
{
    public interface IPlayerSyncer
    {
        public bool IsConnectedToServer { get; }
        public event Action OnConnectedToServer;

        public ushort LocalClientID { get; }

        public void RegisterPlayerStateModule(IBaseStateModule module);
        public void DeregisterPlayerStateModule(IBaseStateModule module);

        public void RegisterInteractor(IInteractor interactor);
        public void DeregisterInteractor(IInteractor interactor);

        public string GameObjectName { get; }
    }
}

/*
    Do we put the PlayerStateModuleContainer into this interface too? 
    this means the container has to live inside V_Instancing

*/
