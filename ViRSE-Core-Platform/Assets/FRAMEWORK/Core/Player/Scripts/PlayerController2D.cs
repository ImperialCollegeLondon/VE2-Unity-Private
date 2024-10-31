using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ViRSE.Core.Shared;
using static ViRSE.Core.Shared.CoreCommonSerializables;

namespace ViRSE.Core.Player
{
    public class PlayerController2D : PlayerController
    {
        [SerializeField] private Camera _camera2D;
        [SerializeField] private Player2DLocomotor _playerLocomotor2D;
        [SerializeField] private Interactor2D _interactor2D;
        [SerializeField] private CharacterController _characterController;

        private Vector3 _rootPosition { 
            get => transform.position + (Vector3.down * _characterController.height / 2); 
            set {
                _characterController.enabled = false;
                transform.position = value + (Vector3.up * _characterController.height / 2);
                _characterController.enabled = true;
            }
        }    

        public override PlayerTransformData PlayerTransformData {
            get {
                return new PlayerTransformData (
                    false,
                    _rootPosition, 
                    transform.rotation,
                    _camera2D.transform.position - _rootPosition,
                    _camera2D.transform.localRotation,
                    _interactor2D.GrabberTransform.localPosition, 
                    _interactor2D.GrabberTransform.localRotation
                );
            }
        }

        private Player2DControlConfig _controlConfig;

        public void Initialize(Player2DControlConfig controlConfig, IMultiplayerSupport multiplayerSupport, IInputHandler inputHandler, IRaycastProvider raycastProvider) 
        {
            _controlConfig = controlConfig;
            _interactor2D.Initialize(_camera2D, multiplayerSupport, inputHandler, raycastProvider);
        }

        public override void ActivatePlayer(PlayerTransformData initTransformData)
        {
            _rootPosition = initTransformData.RootPosition;
            transform.rotation = initTransformData.RootRotation;
            _camera2D.transform.rotation = initTransformData.HeadLocalRotation;
            _interactor2D.GrabberTransform.SetLocalPositionAndRotation(initTransformData.Hand2DLocalPosition, initTransformData.Hand2DLocalRotation);
            gameObject.SetActive(true);
        }

        public override void DeactivatePlayer() 
        {
            gameObject.SetActive(false);
        }
    }
}
