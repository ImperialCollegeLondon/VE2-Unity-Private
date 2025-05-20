using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using VE2.Common.API;
using VE2.Common.Shared;
using VE2.Core.Player.API;
using VE2.Core.VComponents.API;

namespace VE2.Core.Player.Internal
{
    internal class Interactor2D : PointerInteractor
    {
        private ColorConfiguration _colorConfig => ColorConfiguration.Instance;
        private readonly Image _reticuleImage;
        private readonly PlayerConnectionPromptHandler _connectionPromptHandler;

        internal Interactor2D(HandInteractorContainer interactorContainer, IGrabInteractablesContainer grabInteractablesContainer, InteractorInputContainer interactorInputContainer, PlayerInteractionConfig interactionConfig,
            InteractorReferences interactorReferences, InteractorType interactorType, IRaycastProvider raycastProvider, ILocalClientIDWrapper localClientIDWrapper) : 
            base(interactorContainer, grabInteractablesContainer, interactorInputContainer, interactionConfig,
                interactorReferences, interactorType, raycastProvider, localClientIDWrapper, null, new HoveringOverScrollableIndicator())   
        {
            Interactor2DReferences interactor2DReferences = interactorReferences as Interactor2DReferences;
            _reticuleImage = interactor2DReferences.ReticuleImage;

            _connectionPromptHandler = interactor2DReferences.ConnectionPromptHandler;

            //TODO: Don't want to do this in constructor, should happen in HandleOnEnable
            if (!localClientIDWrapper.IsClientIDReady)
                _connectionPromptHandler.NotifyWaitingForConnection();
        }

        protected override void SetInteractorState(InteractorState newState)
        {
            _reticuleImage.enabled = newState != InteractorState.Grabbing;

            switch (newState)
            {
                case InteractorState.Idle:
                    _reticuleImage.color = _colorConfig.PointerIdleColor;
                    break;
                case InteractorState.InteractionAvailable:
                    _reticuleImage.color = _colorConfig.PointerHighlightColor;
                    break;
                case InteractorState.InteractionLocked:
                    _reticuleImage.color = Color.red;
                    break;
                case InteractorState.Grabbing:
                    //No colour 
                    break;
            }
        }

        protected override void HandleStartGrabbingAdjustable(IRangedAdjustableInteractionModule rangedAdjustableInteraction)
        {
            //Unlike VR, we should just apply a one-time offset on grab, and have the grabber behave like its on the end of a stick
            //I.E, it's position is affected by the rotation of its parent 
            Vector3 directionToGrabber = rangedAdjustableInteraction.Transform.position - GrabberTransform.position;
            GrabberTransform.position += directionToGrabber;
        }

        protected override void HandleUpdateGrabbingAdjustable() { } //Nothing needed here

        protected override void HandleStopGrabbingAdjustable()
        {
            GrabberTransform.localPosition = Vector3.zero;
        }

        protected override void HandleLocalClientIDReady(ushort clientID)
        {
            base.HandleLocalClientIDReady(clientID);

            _connectionPromptHandler.NotifyConnected();
        }
    }

}
