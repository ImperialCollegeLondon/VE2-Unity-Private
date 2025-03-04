using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VE2.Core.Player.API;
using VE2.Core.VComponents.API;
using static VE2.Core.Player.API.PlayerSerializables;

namespace VE2.Core.Player.Internal
{
    [Serializable]
    internal class Interactor2DReferences : InteractorReferences
    {
        public Image ReticuleImage => _reticuleImage;
        [SerializeField, IgnoreParent] private Image _reticuleImage;
    }

    internal class PlayerController2D 
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
                    hand2DRotation: _interactor2D.GrabberTransform.localRotation
                );
            }
        }

        private readonly GameObject _playerGO;
        private readonly Player2DControlConfig _controlConfig;
        private readonly Player2DInputContainer _player2DInputContainer;
        private readonly Player2DLocomotor _playerLocomotor2D;
        private readonly Interactor2D _interactor2D;

        internal PlayerController2D(InteractorContainer interactorContainer, Player2DInputContainer player2DInputContainer, IPlayerPersistentDataHandler playerPersistentDataHandler,
            Player2DControlConfig controlConfig, IRaycastProvider raycastProvider, ILocalClientIDProvider multiplayerSupport) 
        {
            GameObject player2DPrefab = Resources.Load("2dPlayer") as GameObject;
            _playerGO = GameObject.Instantiate(player2DPrefab, null, false);
            _playerGO.SetActive(false);

            _controlConfig = controlConfig;
            _player2DInputContainer = player2DInputContainer;

            Player2DReferences player2DReferences = _playerGO.GetComponent<Player2DReferences>();

            _interactor2D = new(
                interactorContainer, player2DInputContainer.InteractorInputContainer2D,
                player2DReferences.Interactor2DReferences, InteractorType.Mouse2D, raycastProvider, multiplayerSupport);

            _playerLocomotor2D = new(player2DReferences.Locomotor2DReferences);
            
            //TODO: think about inspect mode, does that live in the interactor, or the player controller?
            //If interactor, will need to make the interactor2d constructor take a this as a param, and forward the other params to the base constructor
        }

        public void ActivatePlayer(PlayerTransformData initTransformData)
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
        }

        public void DeactivatePlayer() 
        {
            if (_playerGO != null)
                _playerGO.gameObject.SetActive(false);

            _playerLocomotor2D.HandleOnDisable();
            _interactor2D.HandleOnDisable();
        }

        public void HandleUpdate() 
        {
            _playerLocomotor2D.HandleUpdate();
            _interactor2D.HandleUpdate();
        }
    }
}
