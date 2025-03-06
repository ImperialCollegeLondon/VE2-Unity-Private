using UnityEngine;
using VE2.Core.Player.API;
using VE2.Core.VComponents.API;

namespace VE2.NonCore.Instancing.Internal
{
    internal class RemoteInteractor : MonoBehaviour, IInteractor //TODO: Maybe doesn't need to be a mononbehaviour?
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
}
