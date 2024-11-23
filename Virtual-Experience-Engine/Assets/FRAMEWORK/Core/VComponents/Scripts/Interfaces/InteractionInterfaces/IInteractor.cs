using UnityEngine;

//TODO: Find a more sensible place for this to live... maybe a bespoke assembly for interactor interfaces 
//Then again, its redudant to even pass the interaction module to remote interactors 
namespace VE2.Core.VComponents.InteractableInterfaces
{
    public interface IInteractor
    {
        public Transform Transform { get; }
        public void ConfirmGrab(IRangedGrabInteractionModule rangedGrabInteractionModule);
        public void ConfirmDrop();  
    }
}
