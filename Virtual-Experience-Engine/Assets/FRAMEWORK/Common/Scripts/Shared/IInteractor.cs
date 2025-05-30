using UnityEngine;

//Note, this lives in the VC API rather than the player API as the VC internals need to pass a VC interface to ConfirmGrab
namespace VE2.Common.Shared
{
    internal interface IInteractor
    {
        public Transform GrabberTransform { get; }
        public void ConfirmGrab(string id);
        public void ConfirmDrop();
    }
}
