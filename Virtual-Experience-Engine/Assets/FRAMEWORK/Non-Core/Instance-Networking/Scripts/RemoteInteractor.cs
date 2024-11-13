using UnityEngine;
using VE2.Common;

namespace VE2.InstanceNetworking
{
    public class RemoteInteractor : MonoBehaviour, IInteractor
    {
        public Transform ConfirmGrab()
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
