using UnityEngine;
using UnityEngine.UI;
using VE2.Core.Common;
using VE2.Core.Player.API;
using VE2.Core.VComponents.API;

namespace VE2.Core.Player.Internal
{
    internal class Interactor2D : PointerInteractor
    {
        private readonly V_CollisionDetector _collisionDetector;
        private readonly Image _reticuleImage;
        private readonly ColorConfiguration _colorConfig;

        internal Interactor2D(InteractorContainer interactorContainer, InteractorInputContainer interactorInputContainer,
            InteractorReferences interactorReferences, InteractorType interactorType, IRaycastProvider raycastProvider, ILocalClientIDProvider playerSyncer) : 
            base(interactorContainer, interactorInputContainer,
                interactorReferences, interactorType, raycastProvider, playerSyncer)   
        {
            Interactor2DReferences interactor2DReferences = interactorReferences as Interactor2DReferences;
            _reticuleImage = interactor2DReferences.ReticuleImage;
            _collisionDetector = interactor2DReferences.CollisionDetector;

            _colorConfig = Resources.Load<ColorConfiguration>("ColorConfiguration"); //TODO: Inject, can probably actually go into the base class
        }

        public override void HandleOnEnable()
        {
            base.HandleOnEnable();
            _collisionDetector.OnCollideStart += HandleCollideStart;
            _collisionDetector.OnCollideEnd += HandleCollideEnd;
        }

        public override void HandleOnDisable()
        {
            base.HandleOnEnable();
            _collisionDetector.OnCollideStart -= HandleCollideStart;
            _collisionDetector.OnCollideEnd -= HandleCollideEnd;
        }

        private void HandleCollideStart(ICollideInteractionModule collideInteractionModule)
        {
            if (!_WaitingForLocalClientID && !collideInteractionModule.AdminOnly && collideInteractionModule.CollideInteractionType == CollideInteractionType.Feet)
            {
                collideInteractionModule.InvokeOnCollideEnter(_InteractorID);
                HeldActivatableIDs.Add(collideInteractionModule.ID);
            }

        }

        private void HandleCollideEnd(ICollideInteractionModule collideInteractionModule)
        {
            if (!_WaitingForLocalClientID && !collideInteractionModule.AdminOnly && collideInteractionModule.CollideInteractionType == CollideInteractionType.Feet)
            {
                collideInteractionModule.InvokeOnCollideExit(_InteractorID);
                HeldActivatableIDs.Remove(collideInteractionModule.ID);
            }
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
    }

}
