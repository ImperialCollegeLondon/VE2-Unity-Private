
namespace VE2.Core.VComponents.API
{
    public interface ICollideInteractionModule : IGeneralInteractionModule
    {
        public void InvokeOnCollideEnter(ushort clientID);
        public void InvokeOnCollideExit(ushort clientID);
    }
}