
namespace VE2.Core.VComponents.API
{
    internal interface ICollideInteractionModule : IGeneralInteractionModule
    {
        public void InvokeOnCollideEnter(InteractorID interactorID);
        public void InvokeOnCollideExit(InteractorID interactorID);
        public string ID { get; }
    }
}