using UnityEngine;
using VE2.Common;
using VE2.Core.VComponents.InteractableInterfaces;

namespace VE2.InstanceNetworking
{
    public class RemoteInteractor : MonoBehaviour, IInteractor
    {
        public Transform ConfirmGrab(IRangedGrabInteractionModule rangedGrabInteractionModule)
        {
            return transform;
            //TODO: hide
        }

        public void ConfirmDrop()
        {
            //TODO: Show 
        }
    }
}
