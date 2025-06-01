using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using VE2.Common.API;
using VE2.Common.Shared;
using VE2.Core.Player.API;
using VE2.Core.UI.API;
using VE2.Core.VComponents.API;
using static VE2.Core.Player.API.PlayerSerializables;

namespace VE2.Core.Player.Internal
{
    internal class PlayerControllerVR : BasePlayerController
    {
        public PlayerTransformData PlayerTransformData
        {
            get
            {
                return new PlayerTransformData(
                    IsVRMode: true,
                    rootPosition: _rootTransform.position,
                    rootRotation: _rootTransform.rotation,
                    verticalOffset: _verticalOffsetTransform.localPosition.y,
                    headPosition: _headTransform.transform.localPosition,
                    headRotation: _headTransform.transform.localRotation,
                    handVRLeftPosition: _handControllerLeft.Transform.localPosition,
                    handVRLeftRotation: _handControllerLeft.Transform.localRotation,
                    handVRRightPosition: _handControllerRight.Transform.localPosition,
                    handVRRightRotation: _handControllerRight.Transform.localRotation,
                    activatableIDsVRLeft: (List<string>)_handControllerLeft.HeldActivatableIDs,
                    activatableIDsVRRight: (List<string>)_handControllerRight.HeldActivatableIDs,
                    activatableIDsFeet: (List<string>)_feetInteractorVR.HeldNetworkedActivatableIDs
                );
            }
        }

        private readonly GameObject _playerGO;
        private readonly PlayerVRInputContainer _playerVRInputContainer;
        private readonly PlayerVRControlConfig _controlConfig;
        private readonly IXRManagerWrapper _xrManagerSettingsWrapper;
        private readonly Transform _verticalOffsetTransform;
        private readonly Transform _headTransform;
        private readonly FeetInteractor _feetInteractorVR;
        private readonly ResetViewUIHandler _resetViewUIHandler;
        private readonly Transform _neutralPositionOffsetTransform;
        private readonly MovementModeConfig _movementModeConfig;
        private readonly CameraConfig _cameraConfig;

        private readonly HandController _handControllerLeft;
        private readonly HandController _handControllerRight;

        //TODO - it's both players that need these. 
        //That suggests we probably want this in a base class? Think about refactoring
        private readonly IPrimaryUIServiceInternal _primaryUIService; //Secondary lives on the hands
        private readonly RectTransform _primaryUIHolderRect;
        private readonly ISecondaryUIServiceInternal _secondaryUIService;

        internal PlayerControllerVR(HandInteractorContainer interactorContainer, IGrabInteractablesContainer grabInteractablesContainer, PlayerVRInputContainer playerVRInputContainer, IPlayerPersistentDataHandler playerSettingsHandler, 
            PlayerVRControlConfig controlConfig, PlayerInteractionConfig interactionConfig, MovementModeConfig movementModeConfig, CameraConfig cameraConfig, IRaycastProvider raycastProvider, 
            ICollisionDetectorFactory collisionDetectorFactory, IXRManagerWrapper xrManagerSettingsWrapper, ILocalClientIDWrapper localClientIDWrapper,
            IPrimaryUIServiceInternal primaryUIService, ISecondaryUIServiceInternal secondaryUIService)
        {
            GameObject playerVRPrefab = Resources.Load("vrPlayer") as GameObject;
            _playerGO = GameObject.Instantiate(playerVRPrefab, null, false);
            _playerGO.SetActive(false);

            _playerVRInputContainer = playerVRInputContainer;
            _controlConfig = controlConfig;
            _xrManagerSettingsWrapper = xrManagerSettingsWrapper;

            _primaryUIService = primaryUIService;
            _secondaryUIService = secondaryUIService;

            PlayerVRReferences playerVRReferences = _playerGO.GetComponent<PlayerVRReferences>();
            Camera = playerVRReferences.Camera;
            _rootTransform = playerVRReferences.RootTransform;
            _verticalOffsetTransform = playerVRReferences.VerticalOffsetTransform;
            _headTransform = playerVRReferences.HeadTransform;
            _primaryUIHolderRect = playerVRReferences.PrimaryUIHolderRect;
            _feetInteractorVR = new FeetInteractor(collisionDetectorFactory, ColliderType.FeetVR, playerVRReferences.FeetCollider, InteractorType.Feet, localClientIDWrapper, interactionConfig);
            _resetViewUIHandler = playerVRReferences.ResetViewUIHandler;
            _neutralPositionOffsetTransform = playerVRReferences.NeutralPositionOffsetTransform;
            _movementModeConfig = movementModeConfig;
            _cameraConfig = cameraConfig;

            base._PlayerHeadTransform = _headTransform;
            base._FeetCollisionDetector = _feetInteractorVR._collisionDetector as CollisionDetector;

            GameObject handVRLeftPrefab = Resources.Load<GameObject>("HandVRLeft");
            GameObject handVRLeftGO = GameObject.Instantiate(handVRLeftPrefab, _verticalOffsetTransform, false);
            GameObject handVRRightGO = GameObject.Instantiate(handVRLeftPrefab, _verticalOffsetTransform, false);
            handVRRightGO.transform.localScale = new Vector3(-1, 1, 1);
            handVRRightGO.name = "HandVRRight";

            FreeGrabbableWrapper leftHandGrabbableWrapper = new FreeGrabbableWrapper();
            FreeGrabbableWrapper rightHandGrabbableWrapper = new FreeGrabbableWrapper();

            _handControllerLeft = CreateHandController(handVRLeftGO, handVRRightGO, interactorContainer, grabInteractablesContainer, 
                playerVRInputContainer.HandVRLeftInputContainer, playerVRInputContainer.HandVRRightInputContainer.DragLocomotorInputContainer,
                interactionConfig, InteractorType.LeftHandVR, raycastProvider, collisionDetectorFactory, ColliderType.HandVRLeft, localClientIDWrapper, 
                leftHandGrabbableWrapper, rightHandGrabbableWrapper, secondaryUIService, movementModeConfig, false);

            _handControllerRight = CreateHandController(handVRRightGO, handVRLeftGO, interactorContainer, grabInteractablesContainer, 
                playerVRInputContainer.HandVRRightInputContainer, playerVRInputContainer.HandVRLeftInputContainer.DragLocomotorInputContainer,
                interactionConfig, InteractorType.RightHandVR, raycastProvider, collisionDetectorFactory, ColliderType.HandVRRight, localClientIDWrapper, 
                rightHandGrabbableWrapper, leftHandGrabbableWrapper, secondaryUIService, movementModeConfig, true);
        
            ConfigureCamera(cameraConfig);
        }


        private HandController CreateHandController(GameObject handGO, GameObject otherHandGO, HandInteractorContainer interactorContainer, IGrabInteractablesContainer grabInteractablesContainer,
            HandVRInputContainer handVRInputContainer, DragLocomotorInputContainer otherHandDragInputContainer, PlayerInteractionConfig playerInteractionConfig, InteractorType interactorType,
            IRaycastProvider raycastProvider, ICollisionDetectorFactory collisionDetectorFactory, ColliderType colliderType, ILocalClientIDWrapper localClientID, FreeGrabbableWrapper thisHandGrabbableWrapper,
            FreeGrabbableWrapper otherHandGrabbableWrapper, ISecondaryUIServiceInternal secondaryUIService, MovementModeConfig movementModeConfig, bool needsToFlip)
        {
            HandVRReferences thisHandVRReferences = handGO.GetComponent<HandVRReferences>();
            HandVRReferences otherHandVRReferences = otherHandGO.GetComponent<HandVRReferences>();

            HoveringOverScrollableIndicator hoveringOverScrollableIndicator = new();

            InteractorVR interactor = new(
                interactorContainer, grabInteractablesContainer, handVRInputContainer.InteractorVRInputContainer,
                playerInteractionConfig, thisHandVRReferences.InteractorVRReferences, 
                interactorType, raycastProvider, collisionDetectorFactory, colliderType, localClientID, thisHandGrabbableWrapper, hoveringOverScrollableIndicator);

            DragLocomotorController dragLocomotor = new(
                thisHandVRReferences.LocomotorVRReferences,
                handVRInputContainer.DragLocomotorInputContainer, otherHandDragInputContainer,
                _rootTransform, _verticalOffsetTransform, _headTransform, handGO.transform,
                movementModeConfig);

            SnapTurnController snapTurn = new(
                handVRInputContainer.SnapTurnInputContainer,
                _rootTransform,
                handVRInputContainer.TeleportInputContainer, thisHandGrabbableWrapper, otherHandGrabbableWrapper, thisHandVRReferences.InteractorVRReferences.RayOrigin, otherHandVRReferences.InteractorVRReferences.RayOrigin, movementModeConfig);

            TeleportController teleport = new(
                handVRInputContainer.TeleportInputContainer,
                thisHandVRReferences.TeleporterReferences.TeleportLineRenderer, thisHandVRReferences.InteractorVRReferences.LineRenderer, thisHandVRReferences.TeleporterReferences.TeleportCursorPrefab,
                _rootTransform, _headTransform, otherHandVRReferences.InteractorVRReferences.RayOrigin,
                thisHandGrabbableWrapper, otherHandGrabbableWrapper, hoveringOverScrollableIndicator, movementModeConfig);

            WristUIHandler wristUIHandler = new(
                secondaryUIService, thisHandVRReferences.WristUIReferences.WristUIHolder, _headTransform, thisHandVRReferences.WristUIReferences.Indicator, needsToFlip);

            return new HandController(handGO, handVRInputContainer, interactor, dragLocomotor, snapTurn, teleport, wristUIHandler);
        }

        public void ActivatePlayer(PlayerTransformData initTransformData)
        {
            _playerGO.SetActive(true);

            _rootTransform.SetPositionAndRotation(initTransformData.RootPosition, initTransformData.RootRotation);
            _verticalOffsetTransform.localPosition = new Vector3(0, initTransformData.VerticalOffset, 0);
            //We don't set head transform here, tracking will override it anyway

            if (_xrManagerSettingsWrapper.IsInitializationComplete)
                HandleXRInitComplete();
            else
                _xrManagerSettingsWrapper.OnLoaderInitialized += HandleXRInitComplete;

            _playerVRInputContainer.ResetView.OnStartCharging += HandleResetViewChargeStarted;
            _playerVRInputContainer.ResetView.OnChargeComplete += HandleResetViewCharged;
            _playerVRInputContainer.ResetView.OnCancelCharging += HandleResetViewCancelled;

            _handControllerLeft.HandleOnEnable();
            _handControllerRight.HandleOnEnable();
            _feetInteractorVR.HandleOnEnable();

            _primaryUIService?.MovePrimaryUIToHolderRect(_primaryUIHolderRect);
            _secondaryUIService?.DisableShowHideKeyboardControl();
        }

        public void DeactivatePlayer()
        {
            if (_playerGO != null)
                _playerGO?.SetActive(false);

            _xrManagerSettingsWrapper.StopSubsystems();

            _playerVRInputContainer.ResetView.OnStartCharging -= HandleResetViewChargeStarted;
            _playerVRInputContainer.ResetView.OnChargeComplete -= HandleResetViewCharged;
            _playerVRInputContainer.ResetView.OnCancelCharging -= HandleResetViewCancelled;

            _handControllerLeft.HandleOnDisable();
            _handControllerRight.HandleOnDisable();
            _feetInteractorVR.HandleOnDisable();
        }

        private void HandleXRInitComplete()
        {
            _xrManagerSettingsWrapper.OnLoaderInitialized -= HandleXRInitComplete;
            _xrManagerSettingsWrapper.StartSubsystems();
        }

        internal void HandleLocalAvatarColorChanged(Color newColor)
        {
            _handControllerLeft.HandleLocalAvatarColorChanged(newColor);
            _handControllerRight.HandleLocalAvatarColorChanged(newColor);
        }

        internal override void HandleUpdate()
        {
            base.HandleUpdate();
            _handControllerLeft.HandleUpdate();
            _handControllerRight.HandleUpdate();

            if (_playerVRInputContainer.ResetView.IsCharging)
                _resetViewUIHandler.SetProgress(_playerVRInputContainer.ResetView.ChargeProgress);
        }

        private void HandleResetViewChargeStarted()
        {
            _resetViewUIHandler.StartShowing();
        }

        private void HandleResetViewCharged()
        {
            _resetViewUIHandler.SetResetViewPrimed();
            DOVirtual.DelayedCall(0.2f, ExecuteResetView, false);
        }

        private void ExecuteResetView()
        {
            _resetViewUIHandler.StopShowing();

            Vector3 verticalOffsetLocalPosition = _verticalOffsetTransform.localPosition;

            Vector3 positionOffsetToCorrect = _rootTransform.position - Camera.transform.position + verticalOffsetLocalPosition;
            _neutralPositionOffsetTransform.position += positionOffsetToCorrect;

            // Project root and camera forward vectors onto the horizontal (XZ) plane
            Vector3 cameraForward = Camera.transform.forward;
            cameraForward.y = 0f;
            cameraForward.Normalize();

            Vector3 rootForward = _rootTransform.forward;
            rootForward.y = 0f;
            rootForward.Normalize();

            // Compute the signed angle between them around the Y axis
            float signedYawDelta = Vector3.SignedAngle(cameraForward, rootForward, Vector3.up);

            // Apply the inverse rotation (i.e., rotate the offset transform by the angle needed to cancel the camera's yaw offset)
            Quaternion yRotation = Quaternion.AngleAxis(signedYawDelta, Vector3.up);
            _neutralPositionOffsetTransform.rotation = yRotation * _neutralPositionOffsetTransform.rotation;

            _cameraConfig.OnResetViewVR?.Invoke();
        }

        private void HandleResetViewCancelled()
        {
            _resetViewUIHandler.StopShowing();
        }

        public void TearDown()
        {
            if (_xrManagerSettingsWrapper.IsInitializationComplete)
            {
                _xrManagerSettingsWrapper.StopSubsystems();
                _xrManagerSettingsWrapper.DeinitializeLoader();
            }
        }
    }
}
