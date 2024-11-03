using UnityEngine;
using ViRSE.Common;
using ViRSE.Core.Common;
using VIRSE.Core.VComponents.InteractableInterfaces;

namespace ViRSE.Core.Player
{
    public abstract class PointerInteractor : MonoBehaviour 
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

        protected bool TryGetHoveringRangedInteractable(out IRangedInteractionModule hoveringInteractable)
        {
            // Perform the raycast using the layer mask
            if (!_waitingForMultiplayerSupport && _RaycastProvider.TryGetRangedInteractionModule(_RayOrigin.position, _RayOrigin.transform.forward, out RaycastResultWrapper rangedInteractableHitResult, MAX_RAYCAST_DISTANCE, _LayerMask))
            {
                if (rangedInteractableHitResult.Distance <= rangedInteractableHitResult.RangedInteractable.InteractRange)
                {
                    hoveringInteractable = rangedInteractableHitResult.RangedInteractable;
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
}

