using UnityEngine;
using UnityEngine.UI;
using VE2.Common;
using VE2.Core.Common;

namespace VE2.Core.Player
{
    public class Interactor2D : PointerInteractor
    {
        private Image _reticuleImage;

        public Interactor2D(InteractorContainer interactorContainer, InteractorInputContainer interactorInputContainer,
            InteractorReferences interactorReferences, InteractorType interactorType, IRaycastProvider raycastProvider, IMultiplayerSupport multiplayerSupport) : 
            base(interactorContainer, interactorInputContainer,
                interactorReferences, interactorType, raycastProvider, multiplayerSupport)   
        {
            Interactor2DReferences interactor2DReferences = interactorReferences as Interactor2DReferences;
            _reticuleImage = interactor2DReferences.ReticuleImage;
        }

        protected override void SetInteractorState(InteractorState newState)
        {
            _reticuleImage.enabled = newState != InteractorState.Grabbing;

            switch (newState)
            {
                case InteractorState.Idle:
                    _reticuleImage.color = StaticColors.Instance.lightBlue;
                    break;
                case InteractorState.InteractionAvailable:
                    _reticuleImage.color = StaticColors.Instance.tangerine;
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
