using UnityEngine;
using VE2.Core.VComponents.API;

//Note, this lives in the VC API rather than the player API as the VC internals need to pass a VC interface to ConfirmGrab
namespace VE2.Core.VComponents.API
{
    internal interface IInteractor
    {
        public Transform GrabberTransform { get; }
        public void ConfirmGrab(IRangedGrabInteractionModule rangedGrabInteractionModule);
        public void ConfirmDrop();
    }
}
