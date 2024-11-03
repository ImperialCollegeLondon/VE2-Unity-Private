
namespace VIRSE.Core.VComponents.InteractableInterfaces
{
    public interface IRangedInteractionModule : IGeneralInteractionModule
    {
        public float InteractRange { get; set; }
    }
}
/*
    Ideally, we want the player to only be able to see the player interactables 
    Right, so what we need, is a layer of interfaces that derive from the interactable state modules

*/