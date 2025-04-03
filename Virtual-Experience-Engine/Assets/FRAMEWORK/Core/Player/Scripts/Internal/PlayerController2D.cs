using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VE2.Core.Common;
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

        public V_CollisionDetector CollisionDetector => _collisionDetector;
        [SerializeField, IgnoreParent] private V_CollisionDetector _collisionDetector;
    }

    internal class PlayerController2D : BasePlayerController
    {
        public PlayerTransformData PlayerTransformData {
            get {
                return new PlayerTransformData (
                    IsVRMode: false,
                    rootPosition: _playerLocomotor2D.RootPosition, 
                    rootRotation: _playerLocomotor2D.RootRotation,
                    verticalOffset: _playerLocomotor2D.VerticalOffset,
                    headPosition: _playerLocomotor2D.HeadLocalPosition,
                    headRotation: _playerLocomotor2D.HeadLocalRotation,
                    hand2DPosition: _interactor2D.GrabberTransform.localPosition, 
                    hand2DRotation: _interactor2D.GrabberTransform.localRotation,
                    activatableIDs2D: _interactor2D.HeldActivatableIDs,
                    activatableIDsFeet: _feetInteractor2D.HeldActivatableIDs
                );
            }
        }

        private readonly GameObject _playerGO;
        private readonly Player2DControlConfig _controlConfig;
        private readonly Player2DInputContainer _player2DInputContainer;
        private readonly Player2DLocomotor _playerLocomotor2D;
        private readonly Interactor2D _interactor2D;
        private readonly FeetInteractor _feetInteractor2D;
        private readonly AvatarVisHandler _localAvatarHandler;

        private readonly IPrimaryUIServiceInternal _primaryUIService;
        private readonly RectTransform _primaryUIHolderRect;

        private readonly ISecondaryUIServiceInternal _secondaryUIService;
        private readonly RectTransform _secondaryUIHolder;
        private readonly RectTransform _overlayUIRect;

        internal PlayerController2D(HandInteractorContainer interactorContainer, Player2DInputContainer player2DInputContainer, IPlayerPersistentDataHandler playerPersistentDataHandler,
            Player2DControlConfig controlConfig, IRaycastProvider raycastProvider, ILocalClientIDProvider multiplayerSupport, 
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
            _localAvatarHandler = player2DReferences.LocalAvatarHandler;
            _localAvatarHandler.Initialize(playerService);

            _primaryUIHolderRect = player2DReferences.PrimaryUIHolderRect;
            _secondaryUIHolder = player2DReferences.SecondaryUIHolderRect;
            _overlayUIRect = player2DReferences.OverlayUIRect;

            _interactor2D = new(
                interactorContainer, player2DInputContainer.InteractorInputContainer2D,
                player2DReferences.Interactor2DReferences, InteractorType.Mouse2D, raycastProvider, multiplayerSupport);

            _feetInteractor2D = new(player2DReferences.Interactor2DReferences.CollisionDetector, InteractorType.Feet, multiplayerSupport);

            _playerLocomotor2D = new(player2DReferences.Locomotor2DReferences);

            base._PlayerHeadTransform = _playerLocomotor2D.HeadTransform;
            base._FeetCollisionDetector = player2DReferences.Interactor2DReferences.CollisionDetector;

            if (_primaryUIService != null)
            {
                _primaryUIService.OnUIShow += HandlePrimaryUIActivated;
                _primaryUIService.OnUIHide += HandlePrimaryUIDeactivated;
            }
            
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

            _interactor2D.GrabberTransform.SetLocalPositionAndRotation(initTransformData.Hand2DLocalPosition, initTransformData.Hand2DLocalRotation);
            _interactor2D.HandleOnEnable();

            _feetInteractor2D.HandleOnEnable();

            _primaryUIService?.MovePrimaryUIToHolderRect(_primaryUIHolderRect);
            _secondaryUIService?.MoveSecondaryUIToHolderRect(_secondaryUIHolder);
            _secondaryUIService?.EnableShowHideKeyboardControl();
        }

        internal void DeactivatePlayer() 
        {
            if (_playerGO != null)
                _playerGO.gameObject.SetActive(false);

            _playerLocomotor2D.HandleOnDisable();
            _interactor2D.HandleOnDisable();
            _feetInteractor2D.HandleOnDisable();
        }

        internal override void HandleUpdate() 
        {
            base.HandleUpdate();

            if (_primaryUIService == null || !_primaryUIService.IsShowing)
            {
                _playerLocomotor2D.HandleUpdate();
                _interactor2D.HandleUpdate(); 
            }
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

        internal void HandleReceiveAvatarAppearance(OverridableAvatarAppearance newAvatarAppearance) 
        {
            _localAvatarHandler.HandleReceiveAvatarAppearance(newAvatarAppearance);
        }

        internal void MoveRectToOverlayUI(RectTransform newRect)
        {
            CommonUtils.MovePanelToFillRect(newRect, _overlayUIRect);
        }

        internal void TearDown() 
        {
            _playerLocomotor2D?.HandleOnDisable();
            _interactor2D?.HandleOnDisable();
            _feetInteractor2D?.HandleOnDisable();

            if (_primaryUIService != null)
            {
                _primaryUIService.OnUIShow -= HandlePrimaryUIActivated;
                _primaryUIService.OnUIHide -= HandlePrimaryUIDeactivated;
            }

            if (_playerGO != null)
                GameObject.Destroy(_playerGO);
        }
    }
}
