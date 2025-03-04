using System;
using VE2.Core.VComponents.API;

//No config for collider interactions
namespace VE2.Core.VComponents.Internal
{
    internal class ColliderInteractionModule : GeneralInteractionModule, ICollideInteractionModule
    {
        public ColliderInteractionModule(GeneralInteractionConfig config) : base(config) { }

        public event Action<ushort> OnCollideEnter;
        public event Action<ushort> OnCollideExit;

        public void InvokeOnCollideEnter(ushort id) => OnCollideEnter?.Invoke(id);
        public void InvokeOnCollideExit(ushort id) => OnCollideExit?.Invoke(id);
    }
}
