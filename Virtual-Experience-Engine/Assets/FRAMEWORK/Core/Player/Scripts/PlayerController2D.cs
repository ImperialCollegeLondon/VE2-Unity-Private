using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VE2.Common;
using VE2.Core.Common;
using static VE2.Common.CommonSerializables;

namespace VE2.Core.Player
{
    public class PlayerController2D : PlayerController
    {
        //TODO: Maybe this can be encompassed in a Player2DReferences object?
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
        private Player2DInputContainer _player2DInputContainer;

        public void Initialize(Player2DControlConfig controlConfig, IMultiplayerSupport multiplayerSupport, Player2DInputContainer player2DInputContainer, IRaycastProvider raycastProvider) 
        {
            _controlConfig = controlConfig;
            _player2DInputContainer = player2DInputContainer;
            _interactor2D.Initialize(_camera2D.transform, InteractorType.Mouse2D, multiplayerSupport, player2DInputContainer.InteractorInputContainer2D, raycastProvider);

            //TODO: think about inspect mode, does that live in the interactor, or the player controller?
            //If interactor, will need to make the interactor2d constructor take a this as a param, and forward the other params to the base constructor
        }

        public override void ActivatePlayer(PlayerTransformData initTransformData)
        {
            base.ActivatePlayer(initTransformData);
            _rootPosition = initTransformData.RootPosition;
            transform.rotation = initTransformData.RootRotation;
            _camera2D.transform.rotation = initTransformData.HeadLocalRotation;
            _interactor2D.GrabberTransform.SetLocalPositionAndRotation(initTransformData.Hand2DLocalPosition, initTransformData.Hand2DLocalRotation);
            _interactor2D.HandleOnEnable();
        }

        public override void DeactivatePlayer() 
        {
            base.DeactivatePlayer();
            _interactor2D.HandleOnDisable();
        }
    }
}
