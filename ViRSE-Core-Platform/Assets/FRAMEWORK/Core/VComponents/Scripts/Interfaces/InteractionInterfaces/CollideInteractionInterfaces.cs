
namespace VIRSE.Core.VComponents.InteractableInterfaces
{
    public interface ICollideInteractionModule : IGeneralInteractionModule
    {
        public void InvokeOnCollideEnter(ushort clientID);
    }
}