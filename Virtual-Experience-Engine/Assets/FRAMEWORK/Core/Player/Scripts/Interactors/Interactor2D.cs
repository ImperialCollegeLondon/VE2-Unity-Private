using UnityEngine;
using UnityEngine.UI;

namespace VE2.Core.Player
{

    public class Interactor2D : PointerInteractor
    {
        [SerializeField] private Image reticuleImage;

        protected override void SetInteractorState(InteractorState newState)
        {
            reticuleImage.enabled = newState != InteractorState.Grabbing;

            switch (newState)
            {
                case InteractorState.Idle:
                    reticuleImage.color = StaticColors.Instance.lightBlue;
                    break;
                case InteractorState.InteractionAvailable:
                    reticuleImage.color = StaticColors.Instance.tangerine;
                    break;
                case InteractorState.InteractionLocked:
                    reticuleImage.color = Color.red;
                    break;
                case InteractorState.Grabbing:
                    //No colour 
                    break;
            }
        }
    }

}
