using UnityEngine;
using VE2.Common;
using VE2.Core.VComponents.InteractableInterfaces;

namespace VE2.InstanceNetworking
{
    public class RemoteInteractor : MonoBehaviour, IInteractor
    {
        public Transform Transform => transform;

        public Transform ConfirmGrab(IRangedGrabInteractionModule rangedGrabInteractionModule)
        {
            return transform;
            //TODO: hide
        }

        public void ConfirmDrop()
        {
            //TODO: Show 
        }

        void IInteractor.ConfirmGrab(IRangedGrabInteractionModule rangedGrabInteractionModule)
        {
            throw new System.NotImplementedException();
        }
    }
}
