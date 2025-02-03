using UnityEngine;
using VE2.Common;
using VE2.Core.VComponents.InteractableInterfaces;

namespace VE2.InstanceNetworking
{
    public class RemoteInteractor : MonoBehaviour, IInteractor
    {
        public Transform GrabberTransform => transform;
        private InteractorContainer _interactorContainer;
        private InteractorID _interactorID;

        public void Initialize(ushort clientID, InteractorType interactorType, InteractorContainer interactorContainer)
        {
            _interactorID = new InteractorID(clientID, interactorType);

            _interactorContainer = interactorContainer;
            _interactorContainer.RegisterInteractor(_interactorID.ToString(), this);
        }

        public void TearDown() 
        {
            _interactorContainer.DeregisterInteractor(_interactorID.ToString());
        }

        public void ConfirmGrab(IRangedGrabInteractionModule rangedGrabInteractionModule)
        {
            //TODO: hide
        }

        public void ConfirmDrop()
        {
            //TODO: Show 
        }
    }

    //TODO - need to add to InteractorContainer here 
    //Means we also don't want this being a MB 

}
