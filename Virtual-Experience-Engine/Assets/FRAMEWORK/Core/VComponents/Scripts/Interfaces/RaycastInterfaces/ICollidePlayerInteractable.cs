using VE2.Core.VComponents.InteractableInterfaces;

namespace VE2.Core.VComponents.InteractableFindables
{
    public interface ICollidePlayerInteractableIntegrator 
    {
        protected ICollideInteractionModule _CollideInteractionModule { get; }

        public void InvokeOnCollideEnter(ushort clientID)
        {
            _CollideInteractionModule.InvokeOnCollideEnter(clientID);
        }
    }
}
