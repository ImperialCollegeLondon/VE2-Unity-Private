using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VE2.Common;
using VE2.Core.Common;
using static VE2.Common.CommonSerializables;

namespace VE2.Core.Player
{
    [Serializable]
    public class Interactor2DReferences : InteractorReferences
    {
        public Image ReticuleImage => _reticuleImage;
        [SerializeField, IgnoreParent] private Image _reticuleImage;
    }

    public class PlayerController2D 
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

        public PlayerController2D(InteractorContainer interactorContainer, Player2DInputContainer player2DInputContainer,
            Player2DControlConfig controlConfig, IRaycastProvider raycastProvider, IMultiplayerSupport multiplayerSupport) 
        {
            GameObject player2DPrefab = Resources.Load("2dPlayer") as GameObject;
            _playerGO = GameObject.Instantiate(player2DPrefab, null, false);
            _playerGO.SetActive(false);

            _controlConfig = controlConfig;
            _player2DInputContainer = player2DInputContainer;

            Player2DReferences player2DReferences = _playerGO.GetComponent<Player2DReferences>();

            Interactor2DReferences _interactor2DReferences = player2DReferences.Interactor2DReferences;
            _interactor2D = new(
                interactorContainer, player2DInputContainer.InteractorInputContainer2D, 
                _interactor2DReferences.GrabberTransform, _interactor2DReferences.RayOrigin, _interactor2DReferences.LayerMask, _interactor2DReferences.RaycastHitDebug, 
                InteractorType.Mouse2D, raycastProvider, multiplayerSupport,
                _interactor2DReferences.ReticuleImage);

            Locomotor2DReferences _locomotor2DReferences = player2DReferences.Locomotor2DReferences;
            _playerLocomotor2D = new(
                _locomotor2DReferences.Controller, _locomotor2DReferences.VerticalOffsetTransform, _locomotor2DReferences.CameraTransform, _locomotor2DReferences.GroundLayer);
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
