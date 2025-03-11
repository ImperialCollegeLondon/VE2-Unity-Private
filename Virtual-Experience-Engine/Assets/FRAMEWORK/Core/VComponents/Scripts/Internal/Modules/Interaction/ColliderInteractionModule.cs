using System;
using VE2.Core.VComponents.API;

//No config for collider interactions
namespace VE2.Core.VComponents.Internal
{
    internal class ColliderInteractionModule : GeneralInteractionModule, ICollideInteractionModule
    {
        public ColliderInteractionModule(GeneralInteractionConfig config) : base(config) { }

        public event Action<InteractorID> OnCollideEnter;
        public event Action<InteractorID> OnCollideExit;

        public void InvokeOnCollideEnter(InteractorID id) => OnCollideEnter?.Invoke(id);
        public void InvokeOnCollideExit(InteractorID id) => OnCollideExit?.Invoke(id);
    }
}
