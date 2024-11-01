using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ViRSE.Core.VComponents.InternalInterfaces;

namespace ViRSE.Core.VComponents.PlayerInterfaces
{
    public interface ICollidePlayerInteractable : IGeneralPlayerInteractable
    {
        protected ICollideInteractionModuleImplementor _CollideModuleImplementor { get; }

        public void InvokeOnCollideEnter(ushort clientID)
        {
            _CollideModuleImplementor.CollideInteractionModule.InvokeOnCollideEnter(clientID);
        }
    }
}
