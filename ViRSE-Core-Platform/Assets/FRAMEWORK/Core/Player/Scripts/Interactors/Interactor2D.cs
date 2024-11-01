using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using ViRSE.Common;
using ViRSE.Core.Common;
using ViRSE.Core.VComponents.PlayerInterfaces;

namespace ViRSE.Core.Player
{
    public abstract class BaseInteractor : MonoBehaviour
    {
        [SerializeField] public Transform GrabberTransform;
        [SerializeField] protected LayerMask _LayerMask; // Add a layer mask field
        [SerializeField] protected string _RaycastHitDebug;

        protected Transform _RayOrigin;
        protected const float MAX_RAYCAST_DISTANCE = 10;
        protected InteractorID _InteractorID => new(_multiplayerSupport == null ? ushort.MaxValue : _multiplayerSupport.LocalClientID, InteractorType.Mouse2D);
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

            SubscribeToInputHandler(_InputHandler);
        }

        protected abstract void SubscribeToInputHandler(IInputHandler inputHandler);

        protected bool TryGetHoveringRangedInteractable(out IRangedPlayerInteractable hoveringInteractable)
        {
            // Perform the raycast using the layer mask
            if (!_waitingForMultiplayerSupport && _RaycastProvider.TryGetGameObject(_RayOrigin.position, _RayOrigin.transform.forward, out RaycastResultWrapper rangedInteractableHitResult, MAX_RAYCAST_DISTANCE, _LayerMask))
            {
                if (rangedInteractableHitResult.GameObject.TryGetComponent(out IRangedPlayerInteractable rangedInteractable) && rangedInteractableHitResult.Distance <= rangedInteractable.InteractRange)
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
            if (TryGetHoveringRangedInteractable(out IRangedPlayerInteractable hoveringInteractable))
            {
                if (!hoveringInteractable.AdminOnly)
                {
                    if (hoveringInteractable is IRangedClickPlayerInteractable rangedClickInteractable)
                        rangedClickInteractable.Click(_InteractorID.ClientID);
                }
                else 
                {
                    //TODO, maybe play an error sound or something
                }
            }
        }

        void Update()
        {
            if (TryGetHoveringRangedInteractable(out IRangedPlayerInteractable hoveringInteractable))
            {
                bool isAllowedToInteract = !hoveringInteractable.AdminOnly; //TODO: Add admin check
                reticuleImage.color = isAllowedToInteract ? StaticColors.Instance.tangerine : Color.red;
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
