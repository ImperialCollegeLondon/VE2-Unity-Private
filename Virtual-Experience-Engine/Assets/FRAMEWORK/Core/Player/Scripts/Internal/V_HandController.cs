using System;
using System.Collections.Generic;
using UnityEngine;
using VE2.Core.Common;
using VE2.Core.Player.API;

namespace VE2.Core.Player.Internal
{
    internal class V_HandController 
    {
        public Transform GrabberTransform => _interactor.GrabberTransform;

        internal Transform Transform => _handGO.transform;

        private readonly GameObject _handGO;
        private readonly IValueInput<Vector3> _positionInput;
        private readonly IValueInput<Quaternion> _rotationInput;
        private readonly InteractorVR _interactor;
        private readonly DragLocomotor _dragLocomotor;
        private readonly SnapTurn _snapTurn;
        private readonly Teleport _teleport;
        private List<Material> _colorMaterials = new();

        public V_HandController(GameObject handGO, HandVRInputContainer handVRInputContainer, InteractorVR interactor, DragLocomotor dragLocomotor, SnapTurn snapTurn, Teleport teleport)
        {
            _handGO = handGO;

            _colorMaterials = CommonUtils.GetAvatarColorMaterialsForGameObject(handGO);

            _positionInput = handVRInputContainer.HandPosition;
            _rotationInput = handVRInputContainer.HandRotation;

            _interactor = interactor;
            _dragLocomotor = dragLocomotor;
            _snapTurn = snapTurn;
            _teleport = teleport;
        }

        public void HandleOnEnable()
        {
            _interactor.HandleOnEnable();
            _dragLocomotor.HandleOEnable();
            _snapTurn.HandleOEnable();
            _teleport.HandleOEnable();
        }

        public void HandleOnDisable()
        {
            _interactor.HandleOnDisable();
            _dragLocomotor.HandleOnDisable();
            _snapTurn.HandleOnDisable();
            _teleport.HandleOnDisable();
        }

        public void HandleUpdate()
        {
            _handGO.transform.localPosition = _positionInput.Value;
            _handGO.transform.localRotation = _rotationInput.Value;

            //Only show the hand if its actually tracking
            _handGO.SetActive(_handGO.transform.localPosition != Vector3.zero);

            //Rotate the hand 90 degrees along its local x axis to match the controller 
            _handGO.transform.Rotate(Vector3.right, 90, Space.Self);

            _interactor.HandleUpdate();
            _dragLocomotor.HandleUpdate();
            _snapTurn.HandleUpdate();
            _teleport.HandleUpdate();
        }

        public void HandleLocalAvatarColorChanged(Color newColor)
        {
            foreach (Material material in _colorMaterials)
                material.color = newColor;
        }

        //TODO:
        /*
            When interactor tells us it's grabbed, we need to hide the hand model 
            Add some HandModel handler, wire in input to the hand model
            Figure out hand poses, should probably be orchestrated through here? 
        */
    }
}
