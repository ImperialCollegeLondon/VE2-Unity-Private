
namespace VE2.Core.VComponents.API
{
    internal interface ICollideInteractionModule : IGeneralInteractionModule
    {
        public void InvokeOnCollideEnter(ushort clientID);
        public void InvokeOnCollideExit(ushort clientID);
    }
}