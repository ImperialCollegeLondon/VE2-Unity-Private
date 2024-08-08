using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ViRSE.PluginRuntime.VComponents
{
    public interface ICollidePlayerInteratableImplementor
    {
        protected ICollidePlayerInteratable CollidePlayerInteratable { get; }

        public void InvokeOnCollideEnter(InteractorID interactorID)
        {
            CollidePlayerInteratable.InvokeOnCollideEnter(interactorID);
        }
    }

    public interface ICollidePlayerInteratable
    {
        public void InvokeOnCollideEnter(InteractorID interactorID);
    }
}
