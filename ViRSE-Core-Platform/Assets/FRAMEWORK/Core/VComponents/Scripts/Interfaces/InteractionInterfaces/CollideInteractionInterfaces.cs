
namespace VE2.Core.VComponents.InteractableInterfaces
{
    public interface ICollideInteractionModule : IGeneralInteractionModule
    {
        public void InvokeOnCollideEnter(ushort clientID);
    }
}