using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using VE2.Common.API;
using VE2.Common.Shared;
using VE2.Core.Player.API;
using VE2.Core.UI.API;
using VE2.Core.VComponents.API;

namespace VE2.Core.Player.Internal
{
    internal class InteractorReferences
    {
        public Transform InteractorParentTransform => _interactorParentTransform;
        [SerializeField, IgnoreParent] private Transform _interactorParentTransform;

        public Transform GrabberVisualisationRayOrigin => _grabberVisualisationRayOrigin;
        [SerializeField, IgnoreParent] private Transform _grabberVisualisationRayOrigin; 

        public Transform GrabberTransform => _grabberTransform;
        [SerializeField, IgnoreParent] private Transform _grabberTransform;

        public GameObject GrabberVisualisation => _grabberVisualisation;
        [SerializeField, IgnoreParent] private GameObject _grabberVisualisation;

        public Transform RayOrigin => _rayOrigin;
        [SerializeField, IgnoreParent] private Transform _rayOrigin;

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
        public List<string> HeldActivatableIDs { get => _heldActivatableIDs; set => _heldActivatableIDs = value; }

        protected bool IsCurrentlyGrabbing => _CurrentGrabbingGrabbable != null;
        protected InteractorID _InteractorID => _LocalClientIDWrapper.IsClientIDReady ? new InteractorID(_LocalClientIDWrapper.Value, _InteractorType) : null;
        protected List<string> _heldActivatableIDs = new();

        protected const float MAX_RAYCAST_DISTANCE = 10;
        protected const float MAX_SPHERECAST_RADIUS = 10;
        protected IRangedInteractionModule _CurrentHoveringInteractable;
        protected IRangedClickInteractionModule _CurrentHoveringClickInteractable => _CurrentHoveringInteractable as IRangedClickInteractionModule;
        protected IRangedGrabInteractionModule _CurrentGrabbingGrabbable;


        private GameObject lastHoveredUIObject = null; // Keep track of the last hovered UI object

        private readonly HandInteractorContainer _interactorContainer;
        private readonly IGrabInteractablesContainer _grabInteractablesContainer;
        private readonly InteractorInputContainer _interactorInputContainer;
        private readonly LayerMask _raycastLayerMask;

        protected readonly Transform _interactorParentTransform;
        protected readonly Transform _GrabberTransform;
        private readonly LineRenderer _grabbableLineVisLineRenderer;
        protected readonly GameObject _GrabberVisualisation;
        protected readonly Transform _GrabberVisualisationRayOrigin;

        protected readonly Transform _RayOrigin;
        private readonly StringWrapper _raycastHitDebug;

        private readonly InteractorType _InteractorType;
        private readonly IRaycastProvider _RaycastProvider;
        protected readonly ILocalClientIDWrapper _LocalClientIDWrapper;

        //TODO - this can probably live just in InteractorVR... is there any reason the 2d interactor needs this? Think its just for teleporting?
        internal readonly FreeGrabbableWrapper GrabbableWrapper;

        private readonly HoveringOverScrollableIndicator _hoveringOverScrollableIndicator;

        internal PointerInteractor(HandInteractorContainer interactorContainer, IGrabInteractablesContainer grabInteractablesContainer, InteractorInputContainer interactorInputContainer, PlayerInteractionConfig interactionConfig,
            InteractorReferences interactorReferences, InteractorType interactorType, IRaycastProvider raycastProvider, 
            ILocalClientIDWrapper localClientIDWrapper, FreeGrabbableWrapper grabbableWrapper, HoveringOverScrollableIndicator hoveringOverScrollableIndicator)
        {
            _interactorContainer = interactorContainer;
            _grabInteractablesContainer = grabInteractablesContainer;
            _interactorInputContainer = interactorInputContainer;
            _raycastLayerMask = interactionConfig.InteractableLayers;

            _interactorParentTransform = interactorReferences.InteractorParentTransform;
            _GrabberTransform = interactorReferences.GrabberTransform;
            _GrabberVisualisation = interactorReferences.GrabberVisualisation;
            _GrabberVisualisationRayOrigin = interactorReferences.GrabberVisualisationRayOrigin;
            _grabbableLineVisLineRenderer = _GrabberVisualisation.GetComponent<LineRenderer>();
            _RayOrigin = interactorReferences.RayOrigin;
            _raycastHitDebug = interactorReferences.RaycastHitDebug;

            _InteractorType = interactorType;
            _RaycastProvider = raycastProvider;

            _LocalClientIDWrapper = localClientIDWrapper;

            GrabbableWrapper = grabbableWrapper;

            _hoveringOverScrollableIndicator = hoveringOverScrollableIndicator;
        }

        public virtual void HandleOnEnable()
        {
            _interactorInputContainer.RangedClick.OnPressed += HandleRangedClickPressed;
            _interactorInputContainer.RangedClick.OnReleased += HandleRangedClickReleased;
            _interactorInputContainer.HandheldClick.OnPressed += HandleHandheldClickPressed;
            _interactorInputContainer.Grab.OnPressed += HandleGrabPressed;
            _interactorInputContainer.ScrollTickUp.OnTickOver += HandleScrollUp;
            _interactorInputContainer.ScrollTickDown.OnTickOver += HandleScrollDown;

            _heldActivatableIDs = new();

            if (!_LocalClientIDWrapper.IsClientIDReady)
                _LocalClientIDWrapper.OnClientIDReady += HandleLocalClientIDReady;
            else
                HandleLocalClientIDReady(_LocalClientIDWrapper.Value);
        }

        public virtual void HandleOnDisable()
        {
            _interactorInputContainer.RangedClick.OnPressed -= HandleRangedClickPressed;
            _interactorInputContainer.RangedClick.OnReleased -= HandleRangedClickReleased;
            _interactorInputContainer.HandheldClick.OnPressed -= HandleHandheldClickPressed;
            _interactorInputContainer.Grab.OnPressed -= HandleGrabPressed;
            _interactorInputContainer.ScrollTickUp.OnTickOver -= HandleScrollUp;
            _interactorInputContainer.ScrollTickDown.OnTickOver -= HandleScrollDown;

            _heldActivatableIDs = new();

            _LocalClientIDWrapper.OnClientIDReady -= HandleLocalClientIDReady;
            _interactorContainer?.DeregisterInteractor(_InteractorID.ToString());
        }

        protected virtual void HandleLocalClientIDReady(ushort clientID)
        {
            _LocalClientIDWrapper.OnClientIDReady -= HandleLocalClientIDReady;
            _interactorContainer.RegisterInteractor(_InteractorID.ToString(), this);
        }

        public void HandleUpdate()
        {
            RaycastResultWrapper raycastResultWrapper = GetRayCastResult();
            RaycastResultWrapper sphereCastResultWrapper = GetSphereCastResult(); // for 2D interactor, this will be null

            IRangedInteractionModule previousHoveringInteractable = _CurrentHoveringInteractable;

            //Update the current hovering interactable, as long as we're not waiting for id, and it's not a grabbable that we were previously hovering over
            if (_LocalClientIDWrapper.IsClientIDReady && !(previousHoveringInteractable is IRangedGrabInteractionModule previousRangedGrabInteractable && _CurrentGrabbingGrabbable == previousRangedGrabInteractable))
            {
                if(raycastResultWrapper.HitInteractable && raycastResultWrapper.RangedInteractableIsInRange)
                    _CurrentHoveringInteractable = raycastResultWrapper.RangedInteractableInRange;
                else if(this is InteractorVR && !raycastResultWrapper.HitInteractable && sphereCastResultWrapper != null && sphereCastResultWrapper.HitInteractable && sphereCastResultWrapper.RangedInteractableIsInRange)
                    _CurrentHoveringInteractable = sphereCastResultWrapper.RangedInteractableInRange;
                else
                    _CurrentHoveringInteractable = null;
            }

            //If we've stopped hovering over something, call exit hover. If we were holding its click down, release
            if (previousHoveringInteractable != null && previousHoveringInteractable != _CurrentHoveringInteractable)
            {
                previousHoveringInteractable.ExitHover(_InteractorID);

                if (previousHoveringInteractable is IRangedHoldClickInteractionModule previousRangedClickInteractable && _heldActivatableIDs.Contains(previousRangedClickInteractable.ID))
                {
                    previousRangedClickInteractable.ClickUp(_InteractorID);
                    _heldActivatableIDs.Remove(previousRangedClickInteractable.ID);
                }
            }

            //If we've started hovering over something, call enter hover
            if (_LocalClientIDWrapper.IsClientIDReady && _CurrentHoveringInteractable != null && _CurrentHoveringInteractable != previousHoveringInteractable)
            {
                if (_CurrentHoveringClickInteractable != null && this is InteractorVR && !_CurrentHoveringClickInteractable.ActivateAtRangeInVR)
                    return;

                _CurrentHoveringInteractable.EnterHover(_InteractorID);
            }

            //if grabbing an adjustable module, update the visualisation, and update input value
            if (IsCurrentlyGrabbing && _CurrentGrabbingGrabbable is IRangedAdjustableInteractionModule rangedAdjustableInteraction)
            {
                _hoveringOverScrollableIndicator.IsHoveringOverScrollableObject = false;

                _grabbableLineVisLineRenderer.startWidth = _grabbableLineVisLineRenderer.endWidth = 0.005f;
                _grabbableLineVisLineRenderer.SetPosition(0, _GrabberVisualisationRayOrigin.position);
                _grabbableLineVisLineRenderer.SetPosition(1, rangedAdjustableInteraction.Transform.position);

                HandleUpdateGrabbingAdjustable();
            }
            else if (!IsCurrentlyGrabbing)
            {
                bool isAllowedToInteract = false;

                //If hovering over an interactable, handle interactor and hover=========
                if (_LocalClientIDWrapper.IsClientIDReady && (raycastResultWrapper.HitUIButton || raycastResultWrapper.HitInteractableInRange))
                {
                    if (raycastResultWrapper.HitInteractable)
                    {
                        isAllowedToInteract = !raycastResultWrapper.RangedInteractable.AdminOnly;
                        if (raycastResultWrapper.RangedInteractable is IRangedClickInteractionModule rangedClickInteraction && this is InteractorVR)
                            isAllowedToInteract &= rangedClickInteraction.ActivateAtRangeInVR;

                        _hoveringOverScrollableIndicator.IsHoveringOverScrollableObject = raycastResultWrapper.HitScrollableInteractableInRange;
                        _raycastHitDebug.Value = raycastResultWrapper.RangedInteractable.ToString();
                    }
                    else if (raycastResultWrapper.HitUIButton)
                    {
                        isAllowedToInteract = raycastResultWrapper.UIButton.interactable;
                        _hoveringOverScrollableIndicator.IsHoveringOverScrollableObject = raycastResultWrapper.HitScrollableUI;
                        _raycastHitDebug.Value = raycastResultWrapper.HitInteractable ? raycastResultWrapper.RangedInteractable.ToString() : raycastResultWrapper.UIButton.name;

                        if (isAllowedToInteract)
                            HandleHoverOverUIGameObject(raycastResultWrapper.UIButton.gameObject);
                        else
                            HandleNoHoverOverUIGameObject();
                    }

                    SetInteractorState(isAllowedToInteract ? InteractorState.InteractionAvailable : InteractorState.InteractionLocked);
                }
                else
                {
                    //check if were in vr and spherecast actually hits something
                    if (this is InteractorVR && !raycastResultWrapper.HitInteractable)
                    {
                        if (sphereCastResultWrapper != null && sphereCastResultWrapper.HitInteractable && sphereCastResultWrapper.RangedInteractableIsInRange)
                        {
                            isAllowedToInteract = !sphereCastResultWrapper.RangedInteractable.AdminOnly;

                            _hoveringOverScrollableIndicator.IsHoveringOverScrollableObject = sphereCastResultWrapper.HitScrollableInteractableInRange;
                            _raycastHitDebug.Value = sphereCastResultWrapper.RangedInteractable.ToString();

                            SetInteractorState(isAllowedToInteract ? InteractorState.InteractionAvailable : InteractorState.InteractionLocked);

                        }
                        else
                        {
                            //set this here because otherwise it doesnt return back to idle state
                            _hoveringOverScrollableIndicator.IsHoveringOverScrollableObject = false;
                            HandleNoHoverOverUIGameObject();
                            SetInteractorState(InteractorState.Idle);
                            _raycastHitDebug.Value = "none";
                        }

                    }
                    else
                    {
                        _hoveringOverScrollableIndicator.IsHoveringOverScrollableObject = false;
                        HandleNoHoverOverUIGameObject();
                        SetInteractorState(InteractorState.Idle);
                        _raycastHitDebug.Value = "none";
                    }
                }
                
                //break out of the whole thing if we're in 2D, this bit is just to manage the raycast distance for the VR interactor
                if(this is Interactor2D)
                    return;

                //if raycast is not null then set it to the distance of the raycast
                //else if the spherecast is not null then set it to the distance of the spherecast
                //else set it to the distance of the raycast (which is 10)
                float distance = raycastResultWrapper.HitInteractable ? raycastResultWrapper.HitDistance : sphereCastResultWrapper.HitInteractable ? sphereCastResultWrapper.HitDistance : MAX_RAYCAST_DISTANCE;
                bool isOnPalm = !raycastResultWrapper.HitInteractable && sphereCastResultWrapper.HitInteractable && sphereCastResultWrapper.RangedInteractableIsInRange;
                
                //jank way to set the parameters for the raycast distance
                HandleRaycastDistance(distance, isOnPalm, sphereCastResultWrapper.HitPosition);
            }
        }

        protected abstract void HandleUpdateGrabbingAdjustable();

        private void HandleHoverOverUIGameObject(GameObject go)
        {
            if (go == lastHoveredUIObject)
                return;

            if (lastHoveredUIObject != null)
                InformUIObjectEndHover(lastHoveredUIObject);

            lastHoveredUIObject = go;
            InformUIObjectStartHover(go);
        }

        private void HandleNoHoverOverUIGameObject()
        {
            if (lastHoveredUIObject == null)
                return;

            InformUIObjectEndHover(lastHoveredUIObject);
            lastHoveredUIObject = null;
        }

        private void InformUIObjectStartHover(GameObject go)
        {
            ExecuteEvents.Execute<IPointerEnterHandler>(
                go,
                new PointerEventData(EventSystem.current),
                (handler, eventData) => handler.OnPointerEnter((PointerEventData)eventData)
            );

            if (go.TryGetComponent(out IUIColorHandler colorHandler))
                colorHandler.OnPointerEnter();
        }

        private void InformUIObjectEndHover(GameObject go)
        {
            ExecuteEvents.Execute<IPointerExitHandler>(
                go,
                new PointerEventData(EventSystem.current),
                (handler, eventData) => handler.OnPointerExit((PointerEventData)eventData)
            );

            if (go.TryGetComponent(out IUIColorHandler colorHandler))
                colorHandler.OnPointerExit();
        }

        protected virtual void HandleRaycastDistance(float distance, bool isOnPalm = false, Vector3 point = default) { } //TODO: Code smell? InteractorVR needs this to set the LineRenderer length

        private RaycastResultWrapper GetRayCastResult()
        {
            if (_RayOrigin == null)
                return null;

            return _RaycastProvider.Raycast(_RayOrigin.position, _RayOrigin.forward, MAX_RAYCAST_DISTANCE, _raycastLayerMask);
        }

        private RaycastResultWrapper GetSphereCastResult(bool failsafeGrab = false)
        {
            if (_GrabberTransform == null || this is Interactor2D)
                return null;

            if (_InteractorType == InteractorType.LeftHandVR)
                return _RaycastProvider.SphereCastAll(_GrabberTransform.position, MAX_SPHERECAST_RADIUS, _GrabberTransform.up, 0f, _raycastLayerMask, failsafeGrab, _GrabberTransform.right);
            if (_InteractorType == InteractorType.RightHandVR)
                return _RaycastProvider.SphereCastAll(_GrabberTransform.position, MAX_SPHERECAST_RADIUS, _GrabberTransform.up, 0f, _raycastLayerMask, failsafeGrab, -_GrabberTransform.right);
            else
                return _RaycastProvider.SphereCastAll(_GrabberTransform.position, MAX_SPHERECAST_RADIUS, _GrabberTransform.up, 0f, _raycastLayerMask);
        }

        private void HandleRangedClickPressed()
        {
            if (!_LocalClientIDWrapper.IsClientIDReady || IsCurrentlyGrabbing)
                return;

            RaycastResultWrapper raycastResultWrapper = GetRayCastResult();

            if (raycastResultWrapper.HitInteractable && raycastResultWrapper.RangedInteractableIsInRange &&
                raycastResultWrapper.RangedInteractable is IRangedClickInteractionModule rangedClickInteractable)
            {
                //TODO - Code smell? This is a bit of a hack to get around the fact that we don't have a way to check if we're in VR or not
                if (this is InteractorVR && !rangedClickInteractable.ActivateAtRangeInVR)
                    return;

                rangedClickInteractable.ClickDown(_InteractorID);
                _CurrentHoveringInteractable = rangedClickInteractable;

                if (rangedClickInteractable is IRangedHoldClickInteractionModule)
                    _heldActivatableIDs.Add(rangedClickInteractable.ID);
            }
            else if (raycastResultWrapper.HitUIButton && raycastResultWrapper.UIButton.IsInteractable())
            {
                raycastResultWrapper.UIButton.onClick.Invoke();
            }
        }

        private void HandleRangedClickReleased()
        {
            if (!_LocalClientIDWrapper.IsClientIDReady || IsCurrentlyGrabbing)
                return;

            if (_CurrentHoveringClickInteractable != null && _CurrentHoveringClickInteractable is IRangedHoldClickInteractionModule _CurrentHoveringHoldClickInteractable)
            {
                _CurrentHoveringHoldClickInteractable.ClickUp(_InteractorID);
                _heldActivatableIDs.Remove(_CurrentHoveringHoldClickInteractable.ID);
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

                if (_LocalClientIDWrapper.IsClientIDReady)
                {
                    if (raycastResultWrapper != null && raycastResultWrapper.HitInteractable && raycastResultWrapper.RangedInteractableIsInRange)
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
                    else
                    {
                        if (this is Interactor2D)
                            return;

                        RaycastResultWrapper sphereCastResultWrapper = GetSphereCastResult(failsafeGrab: true);

                        if (sphereCastResultWrapper != null && sphereCastResultWrapper.HitInteractable && sphereCastResultWrapper.RangedInteractableIsInRange)
                        {
                            if (!sphereCastResultWrapper.RangedInteractable.AdminOnly)
                            {
                                if (sphereCastResultWrapper.RangedInteractable is IRangedGrabInteractionModule rangedGrabInteractable)
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
            }
        }

        public void ConfirmGrab(string id)
        {
            if (!_grabInteractablesContainer.GrabInteractables.TryGetValue(id, out IRangedGrabInteractionModule rangedGrabInteractable))
            {
                Debug.LogError($"Failed to confirm grab, could not find grabbable with id {id}");
                return;
            }

            _CurrentGrabbingGrabbable = rangedGrabInteractable;

            SetInteractorState(InteractorState.Grabbing);

            if (rangedGrabInteractable is IRangedFreeGrabInteractionModule rangedFreeGrabInteractable && GrabbableWrapper != null)
            {
                GrabbableWrapper.RangedFreeGrabInteraction = rangedFreeGrabInteractable;
            }
            else if (_CurrentGrabbingGrabbable is IRangedAdjustableInteractionModule rangedAdjustableInteraction)
            {
                HandleStartGrabbingAdjustable(rangedAdjustableInteraction);
                _GrabberVisualisation.SetActive(true);
            }
        }

        protected abstract void HandleStartGrabbingAdjustable(IRangedAdjustableInteractionModule rangedAdjustableInteraction);

        public void ConfirmDrop()
        {
            SetInteractorState(InteractorState.Idle);

            if (_CurrentGrabbingGrabbable is IRangedAdjustableInteractionModule)
                HandleStopGrabbingAdjustable();

            _CurrentGrabbingGrabbable = null;

            //reset the virtual grabber transform to the original position
            _GrabberTransform.localPosition = Vector3.zero;
            _GrabberVisualisation.SetActive(false);

            if (GrabbableWrapper != null)
                GrabbableWrapper.RangedFreeGrabInteraction = null;
        }

        protected abstract void HandleStopGrabbingAdjustable();

        private void HandleHandheldClickPressed()
        {
            if (_LocalClientIDWrapper.IsClientIDReady && IsCurrentlyGrabbing)
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
            if (IsCurrentlyGrabbing)
            {
                foreach (IHandheldInteractionModule handheldInteraction in _CurrentGrabbingGrabbable.HandheldInteractions)
                {
                    if (handheldInteraction is IHandheldScrollInteractionModule handheldScrollInteraction)
                    {
                        handheldScrollInteraction.ScrollUp(_InteractorID.ClientID);
                    }
                }
            }
            else
            {
                RaycastResultWrapper raycastResultWrapper = GetRayCastResult();

                if (_LocalClientIDWrapper.IsClientIDReady && raycastResultWrapper != null && raycastResultWrapper.HitInteractable && raycastResultWrapper.RangedInteractableIsInRange)
                {
                    if (!raycastResultWrapper.RangedInteractable.AdminOnly)
                    {
                        //if while scrolling up, raycast returns an adjustable module
                        if (raycastResultWrapper.RangedInteractable is IRangedAdjustableInteractionModule rangedAdjustableInteraction)
                        {
                            rangedAdjustableInteraction.ScrollUp();
                        }
                    }
                }
            }
        }

        private void HandleScrollDown()
        {
            if (IsCurrentlyGrabbing)
            {
                foreach (IHandheldInteractionModule handheldInteraction in _CurrentGrabbingGrabbable.HandheldInteractions)
                {
                    if (handheldInteraction is IHandheldScrollInteractionModule handheldScrollInteraction)
                    {
                        handheldScrollInteraction.ScrollDown(_InteractorID.ClientID);
                    }
                }
            }
            else
            {
                RaycastResultWrapper raycastResultWrapper = GetRayCastResult();

                if (_LocalClientIDWrapper.IsClientIDReady && raycastResultWrapper != null && raycastResultWrapper.HitInteractable && raycastResultWrapper.RangedInteractableIsInRange)
                {
                    if (!raycastResultWrapper.RangedInteractable.AdminOnly)
                    {
                        //if while scrolling up, raycast returns an adjustable module
                        if (raycastResultWrapper.RangedInteractable is IRangedAdjustableInteractionModule rangedAdjustableInteraction)
                        {
                            rangedAdjustableInteraction.ScrollDown();
                        }
                    }
                }
            }
        }


        protected abstract void SetInteractorState(InteractorState newState);

        internal enum InteractorState

        {
            Idle,
            InteractionAvailable,
            InteractionLocked,
            Grabbing
        }
    }
}

