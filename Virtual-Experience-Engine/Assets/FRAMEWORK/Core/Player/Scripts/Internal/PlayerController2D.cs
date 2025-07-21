using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VE2.Common.API;
using VE2.Common.Shared;
using VE2.Core.Player.API;
using VE2.Core.UI.API;
using VE2.Core.VComponents.API;
using static VE2.Core.Player.API.PlayerSerializables;

namespace VE2.Core.Player.Internal
{
    [Serializable]
    internal class Interactor2DReferences : InteractorReferences
    {
        public Image ReticuleImage => _reticuleImage;
        [SerializeField, IgnoreParent] private Image _reticuleImage;

        public PlayerConnectionPromptHandler ConnectionPromptHandler => _connectionPromptHandler;
        [SerializeField, IgnoreParent] PlayerConnectionPromptHandler _connectionPromptHandler;

        public Collider FeetCollider => _feetCollider;
        [SerializeField, IgnoreParent] private Collider _feetCollider;

        public Transform GrabberInspectTransform => _grabberInspectTransform;
        [SerializeField, IgnoreParent] private Transform _grabberInspectTransform;
    }

    internal class PlayerController2D : BasePlayerController
    {
        public PlayerTransformData PlayerTransformData
        {
            get
            {
                return new PlayerTransformData(
                    IsVRMode: false,
                    rootPosition: _playerLocomotor2D.RootPosition,
                    rootRotation: _playerLocomotor2D.RootRotation,
                    verticalOffset: _playerLocomotor2D.VerticalOffset,
                    headPosition: _playerLocomotor2D.HeadLocalPosition,
                    headRotation: _playerLocomotor2D.HeadLocalRotation,
                    hand2DPosition: _interactor2D.GrabberTransformWrapper.localPosition,
                    hand2DRotation: _interactor2D.GrabberTransformWrapper.localRotation,
                    activatableIDs2D: (List<string>)_interactor2D.HeldNetworkedActivatableIDs,
                    activatableIDsFeet: (List<string>)_feetInteractor2D.HeldNetworkedActivatableIDs
                );
            }
        }

        public readonly Collider CharacterCollider;

        private readonly GameObject _playerGO;
        private readonly Player2DControlConfig _controlConfig;
        private readonly Player2DInputContainer _player2DInputContainer;
        private readonly Player2DLocomotor _playerLocomotor2D;
        private readonly Interactor2D _interactor2D;
        private readonly FeetInteractor _feetInteractor2D;

        private readonly IPrimaryUIServiceInternal _primaryUIService;
        private readonly RectTransform _primaryUIHolderRect;

        private readonly ISecondaryUIServiceInternal _secondaryUIService;
        private readonly RectTransform _secondaryUIHolder;
        private readonly RectTransform _overlayUIRect;
        private readonly InspectModeIndicator _inspectModeIndicator;

        internal PlayerController2D(HandInteractorContainer interactorContainer, IGrabInteractablesContainer grabInteractablesContainer, Player2DInputContainer player2DInputContainer,
            IPlayerPersistentDataHandler playerPersistentDataHandler, AvatarHandlerBuilderContext avatarHandlerBuilderContext,
            Player2DControlConfig controlConfig, PlayerInteractionConfig interactionConfig, MovementModeConfig movementModeConfig,
            CameraConfig cameraConfig, IRaycastProvider raycastProvider, ICollisionDetectorFactory collisionDetectorFactory, ILocalClientIDWrapper localClientIDWrapper, ILocalAdminIndicator localAdminIndicator,
            IPrimaryUIServiceInternal primaryUIService, ISecondaryUIServiceInternal secondaryUIService, IPlayerServiceInternal playerService)
        {
            GameObject player2DPrefab = Resources.Load("2dPlayer") as GameObject;
            _playerGO = GameObject.Instantiate(player2DPrefab, null, false);
            _playerGO.SetActive(false);

            _controlConfig = controlConfig;
            _player2DInputContainer = player2DInputContainer;

            _primaryUIService = primaryUIService;
            _secondaryUIService = secondaryUIService;

            Player2DReferences player2DReferences = _playerGO.GetComponent<Player2DReferences>();
            Camera = player2DReferences.Camera;

            _primaryUIHolderRect = player2DReferences.PrimaryUIHolderRect;
            _secondaryUIHolder = player2DReferences.SecondaryUIHolderRect;
            _overlayUIRect = player2DReferences.OverlayUIRect;
            _inspectModeIndicator = new InspectModeIndicator();
            CharacterCollider = player2DReferences.CharacterCollider;

            FreeGrabbingIndicator grabbingIndicator = new();

            _interactor2D = new(
                interactorContainer, grabInteractablesContainer, player2DInputContainer.InteractorInputContainer2D, interactionConfig,
                player2DReferences.Interactor2DReferences, InteractorType.Mouse2D, raycastProvider, localClientIDWrapper, localAdminIndicator, _inspectModeIndicator, grabbingIndicator);

            _feetInteractor2D = new(collisionDetectorFactory, ColliderType.Feet2D, player2DReferences.Interactor2DReferences.FeetCollider, InteractorType.Feet, localClientIDWrapper, localAdminIndicator, interactionConfig);
            _playerLocomotor2D = new(player2DReferences.Locomotor2DReferences, movementModeConfig, _inspectModeIndicator, player2DInputContainer.PlayerLocomotor2DInputContainer, Resources.Load<Player2DMovementConfig>("Player2DMovementConfig"), grabbingIndicator);

            _rootTransform = player2DReferences.Locomotor2DReferences.Controller.transform;

            base._PlayerHeadTransform = _playerLocomotor2D.HeadTransform;
            base._FeetCollisionDetector = _feetInteractor2D._collisionDetector as CollisionDetector;

            ConfigureCamera(cameraConfig);

            AvatarHandler = new(
                avatarHandlerBuilderContext.PlayerBuiltInGameObjectPrefabs,
                avatarHandlerBuilderContext.PlayerCustomGameObjectPrefabs,
                avatarHandlerBuilderContext.CurrentInstancedAvatarAppearance,
                true,
                player2DReferences.HeadTransform,
                player2DReferences.TorsoTransform);

            if (localClientIDWrapper.IsClientIDReady)
                OnClientIDReady(localClientIDWrapper.Value);
            else
                localClientIDWrapper.OnClientIDReady += OnClientIDReady;

            //TODO: think about inspect mode, does that live in the interactor, or the player controller?
            //If interactor, will need to make the interactor2d constructor take a this as a param, and forward the other params to the base constructor
        }

        internal void ActivatePlayer(PlayerTransformData initTransformData)
        {
            _playerGO.gameObject.SetActive(true);

            _playerLocomotor2D.RootPosition = initTransformData.RootPosition;
            _playerLocomotor2D.RootRotation = initTransformData.RootRotation;
            _playerLocomotor2D.VerticalOffset = initTransformData.VerticalOffset;
            _playerLocomotor2D.HeadLocalPosition = initTransformData.HeadLocalPosition;
            _playerLocomotor2D.HeadLocalRotation = initTransformData.HeadLocalRotation;
            _playerLocomotor2D.HandleOnEnable();

            _interactor2D.GrabberTransformWrapper.SetLocalPositionAndRotation(initTransformData.Hand2DLocalPosition, initTransformData.Hand2DLocalRotation);
            _interactor2D.HandleOnEnable();

            _feetInteractor2D.HandleOnEnable();

            _primaryUIService?.MovePrimaryUIToHolderRect(_primaryUIHolderRect);
            _secondaryUIService?.MoveSecondaryUIToHolderRect(_secondaryUIHolder);
            _secondaryUIService?.EnableShowHideKeyboardControl();

            if (_primaryUIService != null)
            {
                _primaryUIService.OnUIShowInternal += HandlePrimaryUIActivated;
                _primaryUIService.OnUIHideInternal += HandlePrimaryUIDeactivated;
            }
        }

        internal void DeactivatePlayer()
        {
            if (_playerGO != null)
                _playerGO.gameObject.SetActive(false);

            _playerLocomotor2D.HandleOnDisable();
            _interactor2D.HandleOnDisable();
            _feetInteractor2D.HandleOnDisable();

            if (_primaryUIService != null)
            {
                _primaryUIService.OnUIShowInternal -= HandlePrimaryUIActivated;
                _primaryUIService.OnUIHideInternal -= HandlePrimaryUIDeactivated;
            }
        }

        internal override void HandleUpdate()
        {
            base.HandleUpdate();

            if (_primaryUIService == null || !_primaryUIService.IsShowing)
            {
                _playerLocomotor2D.HandleUpdate();
                _interactor2D.HandleUpdate();
            }

            _feetInteractor2D.HandleUpdate();
        }

        internal void HandlePrimaryUIActivated()
        {
            _overlayUIRect.gameObject.SetActive(false);
            _playerLocomotor2D.HandleOnDisable();
            _interactor2D.HandleOnDisable(); //TODO - we don't want to drop grabbables 
        }

        internal void HandlePrimaryUIDeactivated()
        {
            _overlayUIRect.gameObject.SetActive(true);
            _playerLocomotor2D.HandleOnEnable();
            _interactor2D.HandleOnEnable();
        }

        // internal void HandleReceiveAvatarAppearance(InstancedAvatarAppearance newAvatarAppearance) 
        // {
        //     _localAvatarHandler.HandleReceiveAvatarAppearance(newAvatarAppearance);
        // }

        internal void MoveRectToOverlayUI(RectTransform newRect)
        {
            CommonUtils.MovePanelToFillRect(newRect, _overlayUIRect);
        }

        public override void SetPlayerPosition(Vector3 position)
        {
            _playerLocomotor2D.RootPosition = position;
        }

        internal void TearDown()
        {
            _playerLocomotor2D?.HandleOnDisable();
            _interactor2D?.HandleOnDisable();
            _feetInteractor2D?.HandleOnDisable();

            if (_primaryUIService != null)
            {
                _primaryUIService.OnUIShow.RemoveListener(HandlePrimaryUIActivated);
                _primaryUIService.OnUIHide.RemoveListener(HandlePrimaryUIDeactivated);
            }

            if (_playerGO != null)
                GameObject.Destroy(_playerGO);
        }
    }

    internal class InspectModeIndicator
    {
        public bool IsInspectModeActive = false;
    }

    internal class FreeGrabbingIndicator
    {
        public event Action<IRangedFreeGrabInteractionModule> OnGrabStarted;
        public event Action<IRangedFreeGrabInteractionModule> OnGrabEnded;

        public bool IsGrabbing = false;
        public void SetIsGrabbing(bool toggle, IRangedFreeGrabInteractionModule freeGrabbable)
        {
            if (IsGrabbing == toggle)
                return;

            IsGrabbing = toggle;

            if (IsGrabbing)
                OnGrabStarted?.Invoke(freeGrabbable);
            else
                OnGrabEnded?.Invoke(freeGrabbable);
        }
    }
}
