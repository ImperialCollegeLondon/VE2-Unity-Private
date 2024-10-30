using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using ViRSE.Core.Shared;
using ViRSE.Core.VComponents;

namespace ViRSE.Core.Player
{
    public abstract class BaseInteractor : MonoBehaviour
    {
        [SerializeField] public Transform GrabberTransform;
        [SerializeField] protected LayerMask _LayerMask; // Add a layer mask field
        [SerializeField] protected string _RaycastHitDebug;

        protected Transform _RayOrigin;
        protected const float MAX_RAYCAST_DISTANCE = 10;
        protected InteractorID _InteractorID => new(_multiplayerSupport == null ? 0 : _multiplayerSupport.LocalClientID, InteractorType.TwoD);
        private bool _waitingForMultiplayerSupport => _multiplayerSupport != null && !_multiplayerSupport.IsConnectedToServer;

        protected IMultiplayerSupport _multiplayerSupport;
        protected IInputHandler _InputHandler;
        protected IRaycastProvider _RaycastProvider;

        // Setup method to initialize the ray origin and max raycast distance
        public void Initialize(Camera camera2d, IMultiplayerSupport multiplayerSupport, IInputHandler inputHandler, IRaycastProvider raycastProvider)
        {
            _RayOrigin = camera2d.transform;

            _multiplayerSupport = multiplayerSupport;
            _InputHandler = inputHandler;
            _RaycastProvider = raycastProvider;
        }

        protected abstract void SubscribeToInputHandler(IInputHandler inputHandler);

        protected bool TryGetHoveringRangedInteractable(out IRangedPlayerInteractableImplementor hoveringInteractable)
        {
            // Perform the raycast using the layer mask
            if (!_waitingForMultiplayerSupport && _RaycastProvider.TryGetRangedPlayerInteractable(_RayOrigin.position, _RayOrigin.transform.forward, out IRangedPlayerInteractableImplementor rangedInteractable, MAX_RAYCAST_DISTANCE, _LayerMask))
            {
                if (!rangedInteractable.AdminOnly)
                {
                    hoveringInteractable = rangedInteractable;
                    return true;
                }
            }

            hoveringInteractable = null;
            return false;
        }

        private void OnDestroy()
        {
            UnsubscribeFromInputHandler(_InputHandler);
        }

        protected abstract void UnsubscribeFromInputHandler(IInputHandler inputHandler);
    }

    public class Interactor2D : BaseInteractor
    {
        [SerializeField] private Image reticuleImage;

        protected override void SubscribeToInputHandler(IInputHandler inputHandler) 
        {
            inputHandler.OnMouseLeftClick += HandleLeftClick;
        }

        private void HandleLeftClick()
        {
            if (TryGetHoveringRangedInteractable(out IRangedPlayerInteractableImplementor hoveringInteractable))
            {
                if (hoveringInteractable is IRangedClickPlayerInteractableImplementor rangedClickInteractable)
                    rangedClickInteractable.InvokeOnClickDown(_InteractorID);
            }
        }

        void Update()
        {
            if (TryGetHoveringRangedInteractable(out IRangedPlayerInteractableImplementor hoveringInteractable))
            {
                reticuleImage.color = StaticColors.Instance.tangerine;
                _RaycastHitDebug = hoveringInteractable.ToString();
            }
            else 
            {
                reticuleImage.color = StaticColors.Instance.lightBlue;
                _RaycastHitDebug = "none";
            }
        }

        protected override void UnsubscribeFromInputHandler(IInputHandler inputHandler)
        {
            inputHandler.OnMouseLeftClick -= HandleLeftClick;
        }
    }

}
