using System;
using System.Collections.Generic;
using UnityEngine;
using VE2.Common;
using VE2.Core.Common;

namespace VE2.Core.Player
{
    public class V_HandController //TODO: Where does snap turn go? Maybe just in here?
    {
        public Transform GrabberTransform => _interactor.GrabberTransform;

        private readonly GameObject _handGO;
        private readonly IValueInput<Vector3> _positionInput;
        private readonly IValueInput<Quaternion> _rotationInput;
        private readonly InteractorVR _interactor;
        private readonly DragLocomotor _dragLocomotor;
        private List<Material> _colorMaterials = new();

        public V_HandController(GameObject handGO, HandVRInputContainer handVRInputContainer, InteractorVR interactor, DragLocomotor dragLocomotor)
        {
            _handGO = handGO;
            _colorMaterials = CommonUtils.GetAvatarColorMaterialsForGameObject(handGO);

            _positionInput = handVRInputContainer.HandPosition;
            _rotationInput = handVRInputContainer.HandRotation;

            _interactor = interactor;
            _dragLocomotor = dragLocomotor;
        }

        public void HandleOnEnable()
        {
            _interactor.HandleOnEnable();
            _dragLocomotor.HandleOEnable();
        }

        public void HandleOnDisable()
        {
            _interactor.HandleOnDisable();
            _dragLocomotor.HandleOnDisable();
        }

        public void HandleUpdate()
        {
            _handGO.transform.localPosition = _positionInput.Value;
            _handGO.transform.localRotation = _rotationInput.Value;

            _interactor.HandleUpdate();
            _dragLocomotor.HandleUpdate();
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