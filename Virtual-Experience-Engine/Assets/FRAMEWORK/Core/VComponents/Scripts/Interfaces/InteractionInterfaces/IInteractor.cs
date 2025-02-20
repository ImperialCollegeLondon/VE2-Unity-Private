using UnityEngine;

//TODO: Find a more sensible place for this to live... maybe a bespoke assembly for interactor interfaces 
//Then again, its redudant to even pass the interaction module to remote interactors 
namespace VE2.Core.VComponents.InteractableInterfaces
{
    public interface IInteractor
    {
        public Transform GrabberTransform { get; }
        public void ConfirmGrab(IRangedFreeGrabInteractionModule rangedGrabInteractionModule);
        public void ConfirmDrop();

    /*

        IInteractor needs to reference IIM for the ConfirmGrab
        IIM needs to reference IInteractor.. but only the interactorID!! 
        So why don't we put the interactorID in its own interface 
        That way, IInteractor can go into core, which can reference IMI

        The core locator needs to be able to reference IInteractor, so we have to separate IInteractor from InteractorID
    */
    }
}
