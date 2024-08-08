using System;
using System.Collections;
using System.Collections.Generic;


//No config for collider interactions

namespace ViRSE.PluginRuntime.VComponents
{
    public class ColliderInteractionModule : ICollidePlayerInteratable
    {
        public event Action<InteractorID> OnCollideEnter;

        public void InvokeOnCollideEnter(InteractorID id)
        {
            OnCollideEnter.Invoke(id);
        }
    }
}
