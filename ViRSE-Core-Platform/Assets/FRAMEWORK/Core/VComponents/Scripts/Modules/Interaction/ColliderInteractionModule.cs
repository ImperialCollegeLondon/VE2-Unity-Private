using System;
using System.Collections;
using System.Collections.Generic;


//No config for collider interactions

namespace ViRSE.Core.VComponents.InternalInterfaces
{
    internal class ColliderInteractionModule : GeneralInteractionModule, ICollideInteractionModule
    {
        public ColliderInteractionModule(GeneralInteractionConfig config) : base(config)
        {
        }

        public event Action<ushort> OnCollideEnter;

        public void InvokeOnCollideEnter(ushort id)
        {
            OnCollideEnter.Invoke(id);
        }
    }
}
