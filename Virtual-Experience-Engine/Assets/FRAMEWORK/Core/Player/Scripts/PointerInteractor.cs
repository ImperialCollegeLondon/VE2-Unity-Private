using System.Collections.Generic;
using UnityEngine;
using VE2.Common;
using VE2.Core.Common;
using VE2.Core.VComponents.InteractableInterfaces;

namespace VE2.Core.Player
{
    //TODO: DOesn't need to be a MB?
    //If not an MB, we need to figure out how the state module will find the interactor!!
    public abstract class PointerInteractor : MonoBehaviour, IInteractor
    {
        [SerializeField] public Transform GrabberTransform;
        [SerializeField] protected LayerMask _LayerMask; // Add a layer mask field
        [SerializeField] protected string _RaycastHitDebug;

        protected const float MAX_RAYCAST_DISTANCE = 10;
        protected IRangedGrabInteractionModule _CurrentGrabbingGrabbable;
        protected bool IsCurrentlyGrabbing => _CurrentGrabbingGrabbable != null;
        protected InteractorID _InteractorID => new(_multiplayerSupport == null ? (ushort)0 : _multiplayerSupport.LocalClientID, _InteractorType);
        protected bool _WaitingForMultiplayerSupport => _multiplayerSupport != null && !_multiplayerSupport.IsConnectedToServer;

        //TODO: maybe these can all be private?
        protected Transform _RayOrigin;
        protected InteractorType _InteractorType;
        protected IMultiplayerSupport _multiplayerSupport;
        private InteractorInputContainer _interactorInputContainer; 
        protected IRaycastProvider _RaycastProvider;

        // Setup method to initialize the ray origin and max raycast distance
        //TODO: Ideally, this would be a constructor
        public void Initialize(Transform rayOrigin, InteractorType interactorType, IMultiplayerSupport multiplayerSupport, InteractorInputContainer interactorInputContainer, IRaycastProvider raycastProvider)
        {
            _RayOrigin = rayOrigin;
            _InteractorType = interactorType;
            _multiplayerSupport = multiplayerSupport;
            _interactorInputContainer = interactorInputContainer;
            _RaycastProvider = raycastProvider;

            if (_WaitingForMultiplayerSupport)
                _multiplayerSupport.OnConnectedToServer += RenameInteractorToLocalID;
            else
                RenameInteractorToLocalID();
        }

        public virtual void HandleOnEnable()
        {
            Debug.Log(gameObject.name + " Listening to inputs");
            _interactorInputContainer.RangedClick.OnPressed += HandleRangedClickPressed;
            _interactorInputContainer.HandheldClick.OnPressed += HandleHandheldClickPressed;
            _interactorInputContainer.Grab.OnPressed += HandleGrabPressed;
            _interactorInputContainer.ScrollTickUp.OnTickOver += HandleScrollUp;
            _interactorInputContainer.ScrollTickDown.OnTickOver += HandleScrollDown;
        }

        public virtual void HandleOnDisable()
        {
            Debug.Log(gameObject.name + " Stopped listening to inputs");
            _interactorInputContainer.RangedClick.OnPressed -= HandleRangedClickPressed;
            _interactorInputContainer.HandheldClick.OnPressed -= HandleHandheldClickPressed;
            _interactorInputContainer.Grab.OnPressed -= HandleGrabPressed;
            _interactorInputContainer.ScrollTickUp.OnTickOver -= HandleScrollUp;
            _interactorInputContainer.ScrollTickDown.OnTickOver -= HandleScrollDown;
        }

        private void RenameInteractorToLocalID() 
        {
            ushort localID = 0;

            if (_multiplayerSupport != null)
            {
                _multiplayerSupport.OnConnectedToServer -= RenameInteractorToLocalID;
                localID = _multiplayerSupport.LocalClientID;
            }

            gameObject.name = $"Interactor{localID}-{_InteractorType}";
        }

        void Update()
        {
            if (IsCurrentlyGrabbing)
                return;

            if (!_WaitingForMultiplayerSupport && TryGetHoveringRangedInteractable(out IRangedInteractionModule hoveringInteractable))
            {
                bool isAllowedToInteract = !hoveringInteractable.AdminOnly;
                SetInteractorState(isAllowedToInteract ? InteractorState.InteractionAvailable : InteractorState.InteractionLocked);
                _RaycastHitDebug = hoveringInteractable.ToString();
            }
            else
            {
                SetInteractorState(InteractorState.Idle);
                _RaycastHitDebug = "none";
            }
        }

        private void HandleRangedClickPressed()
        {
            if (!_WaitingForMultiplayerSupport && !IsCurrentlyGrabbing && TryGetHoveringRangedInteractable(out IRangedInteractionModule hoveringInteractable))
            {
                if (hoveringInteractable is IRangedClickInteractionModule rangedClickInteractable)
                    rangedClickInteractable.Click(_InteractorID.ClientID);
            }
        }

        private void HandleGrabPressed()
        {
            if (IsCurrentlyGrabbing)
            {
                IRangedGrabInteractionModule rangedGrabInteractableToDrop = _CurrentGrabbingGrabbable;
                _CurrentGrabbingGrabbable = null;
                rangedGrabInteractableToDrop.RequestLocalDrop(_InteractorID);
            }
            else if (!_WaitingForMultiplayerSupport && TryGetHoveringRangedInteractable(out IRangedInteractionModule hoveringInteractable))
            {
                if (!hoveringInteractable.AdminOnly)
                {
                    if (hoveringInteractable is IRangedGrabInteractionModule rangedGrabInteractable)
                    {
                        _CurrentGrabbingGrabbable = rangedGrabInteractable;
                        rangedGrabInteractable.RequestLocalGrab(_InteractorID);
                    }
                }
                else
                {
                    //TODO, maybe play an error sound or something
                }
            }
        }

        public Transform ConfirmGrab(IRangedGrabInteractionModule rangedGrabInteractable)
        {
            _CurrentGrabbingGrabbable = rangedGrabInteractable;
            SetInteractorState(InteractorState.Grabbing);
            return GrabberTransform;
        }

        public void ConfirmDrop()
        {
            SetInteractorState(InteractorState.Idle);
            _CurrentGrabbingGrabbable = null;
        }

        private void HandleHandheldClickPressed()
        {
            if (!_WaitingForMultiplayerSupport && IsCurrentlyGrabbing)
            {
                foreach (IHandheldInteractionModule handheldInteraction in _CurrentGrabbingGrabbable.HandheldInteractions)
                {
                    if (handheldInteraction is IHandheldClickInteractionModule handheldClickInteraction)
                    {
                        handheldClickInteraction.Click(_InteractorID.ClientID);
                    }
                }
            }
        }

        private void HandleScrollUp()
        {
            foreach (IHandheldInteractionModule handheldInteraction in _CurrentGrabbingGrabbable.HandheldInteractions)
            {
                if (handheldInteraction is IHandheldScrollInteractionModule handheldScrollInteraction)
                {
                    handheldScrollInteraction.ScrollUp(_InteractorID.ClientID);
                }
            }
        }
        private void HandleScrollDown()
        {
            foreach (IHandheldInteractionModule handheldInteraction in _CurrentGrabbingGrabbable.HandheldInteractions)
            {
                if (handheldInteraction is IHandheldScrollInteractionModule handheldScrollInteraction)
                {
                    handheldScrollInteraction.ScrollDown(_InteractorID.ClientID);
                }
            }
        }

        protected bool TryGetHoveringRangedInteractable(out IRangedInteractionModule hoveringInteractable)
        {
            // Perform the raycast using the layer mask
            if (!_WaitingForMultiplayerSupport && _RaycastProvider.TryGetRangedInteractionModule(_RayOrigin.position, _RayOrigin.transform.forward, out RaycastResultWrapper rangedInteractableHitResult, MAX_RAYCAST_DISTANCE, _LayerMask))
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

        protected abstract void SetInteractorState(InteractorState newState);
        protected enum InteractorState
        {
            Idle,
            InteractionAvailable,
            InteractionLocked,
            Grabbing
        }
    }
}

