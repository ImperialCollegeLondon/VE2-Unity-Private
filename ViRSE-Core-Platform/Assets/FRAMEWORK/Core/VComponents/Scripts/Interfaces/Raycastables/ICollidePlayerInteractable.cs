using VIRSE.Core.VComponents.InteractableInterfaces;

namespace ViRSE.Core.VComponents.RaycastInterfaces
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
