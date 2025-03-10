using System;
using UnityEngine;
using UnityEngine.EventSystems;
using VE2.Core.Player.API;
using VE2.Core.VComponents.API;

namespace VE2.Core.Player.Internal
{
    internal class InteractorReferences 
    {
        public Transform GrabberTransform => _grabberTransform;
        [SerializeField, IgnoreParent] private Transform _grabberTransform;

        public Transform RayOrigin => _rayOrigin;
        [SerializeField, IgnoreParent] private Transform _rayOrigin;

        public LayerMask LayerMask => _layerMask;
        [SerializeField, IgnoreParent] private LayerMask _layerMask;

        [SerializeField] private StringWrapper _raycastHitDebug;
        public StringWrapper RaycastHitDebug => _raycastHitDebug;
    }

    [Serializable]
    internal class StringWrapper
    {
        [SerializeField, IgnoreParent] public string Value;
    }

    [Serializable]
    internal class FreeGrabbableWrapper
    {
        public IRangedFreeGrabInteractionModule RangedFreeGrabInteraction { get; internal set; }
    }

    internal abstract class PointerInteractor : IInteractor
    {
        public Transform GrabberTransform => _GrabberTransform;

        protected bool IsCurrentlyGrabbing => _CurrentGrabbingGrabbable != null;
        private ushort _localClientID => _localClientIDProvider == null ? (ushort)0 : _localClientIDProvider.LocalClientID;
        protected InteractorID _InteractorID => new(_localClientID, _InteractorType);
        protected bool _WaitingForLocalClientID => _localClientIDProvider != null && !_localClientIDProvider.IsClientIDReady;

        protected const float MAX_RAYCAST_DISTANCE = 10;
        protected IRangedGrabInteractionModule _CurrentGrabbingGrabbable;

        private GameObject lastHoveredUIObject = null; // Keep track of the last hovered UI object

        private readonly InteractorContainer _interactorContainer;
        private readonly InteractorInputContainer _interactorInputContainer;

        protected readonly Transform _GrabberTransform;
        protected readonly Transform _RayOrigin;
        private readonly LayerMask _layerMask;
        private readonly StringWrapper _raycastHitDebug;


        private readonly InteractorType _InteractorType;
        private readonly IRaycastProvider _RaycastProvider;
        private readonly ILocalClientIDProvider _localClientIDProvider;

        internal readonly FreeGrabbableWrapper GrabbableWrapper;

        internal PointerInteractor(InteractorContainer interactorContainer, InteractorInputContainer interactorInputContainer,
            InteractorReferences interactorReferences, InteractorType interactorType, IRaycastProvider raycastProvider, ILocalClientIDProvider localClientIDProvider, FreeGrabbableWrapper grabbableWrapper = null)
        {
            _interactorContainer = interactorContainer;
            _interactorInputContainer = interactorInputContainer;

            _GrabberTransform = interactorReferences.GrabberTransform;
            _RayOrigin = interactorReferences.RayOrigin;
            _layerMask = interactorReferences.LayerMask;
            _raycastHitDebug = interactorReferences.RaycastHitDebug;

            _InteractorType = interactorType;
            _RaycastProvider = raycastProvider;

            GrabbableWrapper = grabbableWrapper;
            _localClientIDProvider = localClientIDProvider;
        }

        public virtual void HandleOnEnable()
        {
            _interactorInputContainer.RangedClick.OnPressed += HandleRangedClickPressed;
            _interactorInputContainer.HandheldClick.OnPressed += HandleHandheldClickPressed;
            _interactorInputContainer.Grab.OnPressed += HandleGrabPressed;
            _interactorInputContainer.ScrollTickUp.OnTickOver += HandleScrollUp;
            _interactorInputContainer.ScrollTickDown.OnTickOver += HandleScrollDown;

            if (_WaitingForLocalClientID)
                _localClientIDProvider.OnClientIDReady += HandleLocalClientIDReady;
            else
                HandleLocalClientIDReady(_localClientID);
        }

        public virtual void HandleOnDisable()
        {
            _interactorInputContainer.RangedClick.OnPressed -= HandleRangedClickPressed;
            _interactorInputContainer.HandheldClick.OnPressed -= HandleHandheldClickPressed;
            _interactorInputContainer.Grab.OnPressed -= HandleGrabPressed;
            _interactorInputContainer.ScrollTickUp.OnTickOver -= HandleScrollUp;
            _interactorInputContainer.ScrollTickDown.OnTickOver -= HandleScrollDown;

            if (_localClientIDProvider != null)
                _localClientIDProvider.OnClientIDReady -= HandleLocalClientIDReady;

            _interactorContainer.DeregisterInteractor(_InteractorID.ToString());
        }

        private void HandleLocalClientIDReady(ushort clientID) 
        {
            if (_localClientIDProvider != null)
                _localClientIDProvider.OnClientIDReady -= HandleLocalClientIDReady;

            _interactorContainer.RegisterInteractor(_InteractorID.ToString(), this);
        }

        public void HandleUpdate()
        {
            if (IsCurrentlyGrabbing)
                return;

            RaycastResultWrapper raycastResultWrapper = GetRayCastResult();

            if (!_WaitingForLocalClientID && raycastResultWrapper.HitInteractableOrUI && !(raycastResultWrapper.HitInteractable && !raycastResultWrapper.RangedInteractableIsInRange))
            {
                bool isAllowedToInteract = (raycastResultWrapper.HitInteractable && !raycastResultWrapper.RangedInteractable.AdminOnly) || 
                    (raycastResultWrapper.HitUI && raycastResultWrapper.UIButton.interactable);

                SetInteractorState(isAllowedToInteract ? InteractorState.InteractionAvailable : InteractorState.InteractionLocked);

                if (raycastResultWrapper.HitUI)
                {
                    if (isAllowedToInteract)
                        HandleHoverOverUIGameObject(raycastResultWrapper.UIButton.gameObject);
                    else
                        HandleNoHoverOverUIGameObject();
                }

                _raycastHitDebug.Value = raycastResultWrapper.HitInteractable ? raycastResultWrapper.RangedInteractable.ToString() : raycastResultWrapper.UIButton.name;
            }
            else
            {
                SetInteractorState(InteractorState.Idle);
                _raycastHitDebug.Value = "none";
                HandleNoHoverOverUIGameObject();
            }

            HandleRaycastDistance(raycastResultWrapper.HitDistance);
        }

        private void HandleHoverOverUIGameObject(GameObject go)
        {
            if (go == lastHoveredUIObject)
                return;

            lastHoveredUIObject = go;

            ExecuteEvents.Execute<IPointerEnterHandler>(
                go,
                new PointerEventData(EventSystem.current),
                (handler, eventData) => handler.OnPointerEnter((PointerEventData)eventData)
            );
        }

        private void HandleNoHoverOverUIGameObject()
        {
            if (lastHoveredUIObject == null)
                return;

            ExecuteEvents.Execute<IPointerExitHandler>(
                lastHoveredUIObject,
                new PointerEventData(EventSystem.current),
                (handler, eventData) => handler.OnPointerExit((PointerEventData)eventData)
            );

            lastHoveredUIObject = null;
        }

        protected virtual void HandleRaycastDistance(float distance) { } //TODO: Code smell? InteractorVR needs this to set the LineRenderer length

        private RaycastResultWrapper GetRayCastResult()
        {
            if (_RayOrigin == null) 
                return null;

            return _RaycastProvider.Raycast(_RayOrigin.position, _RayOrigin.forward, MAX_RAYCAST_DISTANCE, _layerMask);
        }
        
        private void HandleRangedClickPressed()
        {
            if (_WaitingForLocalClientID || IsCurrentlyGrabbing)
                return;

            RaycastResultWrapper raycastResultWrapper = GetRayCastResult();

            if (raycastResultWrapper.HitInteractable && raycastResultWrapper.RangedInteractableIsInRange &&
                raycastResultWrapper.RangedInteractable is IRangedClickInteractionModule rangedClickInteractable)
            {
                rangedClickInteractable.Click(_InteractorID.ClientID);
            }
            else if (raycastResultWrapper.HitUI && raycastResultWrapper.UIButton.IsInteractable())
            {
                raycastResultWrapper.UIButton.onClick.Invoke();
            }
        }

        private void HandleGrabPressed()
        {
            if (IsCurrentlyGrabbing)
            {
                IRangedGrabInteractionModule rangedGrabInteractableToDrop = _CurrentGrabbingGrabbable;
                rangedGrabInteractableToDrop.RequestLocalDrop(_InteractorID);
            }
            else 
            {
                RaycastResultWrapper raycastResultWrapper = GetRayCastResult();

                if (!_WaitingForLocalClientID && raycastResultWrapper != null && raycastResultWrapper.HitInteractable && raycastResultWrapper.RangedInteractableIsInRange )
                {   
                    if (!raycastResultWrapper.RangedInteractable.AdminOnly)
                    {
                        if (raycastResultWrapper.RangedInteractable is IRangedGrabInteractionModule rangedGrabInteractable)
                        {
                            rangedGrabInteractable.RequestLocalGrab(_InteractorID);
                        }
                    }
                    else
                    {
                        //TODO, maybe play an error sound or something
                    }
                }
            }
        }

        public void ConfirmGrab(IRangedGrabInteractionModule rangedGrabInteractable)
        {
            Debug.Log("ConfirmGrab - null? " + (rangedGrabInteractable == null));
            _CurrentGrabbingGrabbable = rangedGrabInteractable;

            if (rangedGrabInteractable is IRangedFreeGrabInteractionModule rangedFreeGrabInteractable && GrabbableWrapper != null)
                GrabbableWrapper.RangedFreeGrabInteraction = rangedFreeGrabInteractable;
                
            SetInteractorState(InteractorState.Grabbing);
        }

        public void ConfirmDrop()
        {
            SetInteractorState(InteractorState.Idle);
            _CurrentGrabbingGrabbable = null;

            if (GrabbableWrapper != null)
                GrabbableWrapper.RangedFreeGrabInteraction = null;
        }

        private void HandleHandheldClickPressed()
        {
            if (!_WaitingForLocalClientID && IsCurrentlyGrabbing)
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
            if (!IsCurrentlyGrabbing)
                return;

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
            if (!IsCurrentlyGrabbing)
                return;

            foreach (IHandheldInteractionModule handheldInteraction in _CurrentGrabbingGrabbable.HandheldInteractions)
            {
                if (handheldInteraction is IHandheldScrollInteractionModule handheldScrollInteraction)
                {
                    handheldScrollInteraction.ScrollDown(_InteractorID.ClientID);
                }
            }
        }

        // protected bool TryGetHoveringRangedInteractable(out IRangedInteractionModule hoveringInteractable)
        // {
        //     // Perform the raycast using the layer mask
        //     if (!_WaitingForMultiplayerSupport && _RaycastProvider.Raycast(_RayOrigin.position, _RayOrigin.transform.forward, MAX_RAYCAST_DISTANCE, _LayerMask))
        //     {
        //         if (rangedInteractableHitResult.HitDistance <= rangedInteractableHitResult.RangedInteractable.InteractRange)
        //         {
        //             hoveringInteractable = rangedInteractableHitResult.RangedInteractable;
        //             return true;
        //         }
        //     }

        //     hoveringInteractable = null;
        //     return false;
        // }


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

