using System;
using System.Collections.Generic;
using UnityEngine;
using VE2.Common.Shared;
using VE2.Core.Player.API;

namespace VE2.Core.Player.Internal
{
    internal class HandController 
    {
        internal Transform Transform => _nonGrabbingHandGO.transform.parent;
        internal Transform HandVisualHolderTransform => _nonGrabbingHandGO.transform;
        internal IReadOnlyList<string> HeldActivatableIDs => _interactor.HeldNetworkedActivatableIDs;

        private readonly GameObject _nonGrabbingHandGO;
        private readonly IValueInput<Vector3> _positionInput;
        private readonly IValueInput<Quaternion> _rotationInput;
        private readonly InteractorVR _interactor;
        private readonly DragLocomotorController _dragLocomotor;
        private readonly SnapTurnController _snapTurn;
        private readonly TeleportController _teleport;
        private readonly WristUIHandler _wristUIHandler;

        private List<Material> _colorMaterials = new();

        public HandController(GameObject nonGrabbingHandTransform, HandVRInputContainer handVRInputContainer, InteractorVR interactor, 
            DragLocomotorController dragLocomotor, SnapTurnController snapTurn, TeleportController teleport, WristUIHandler wristUIHandler)
        {
            _nonGrabbingHandGO = nonGrabbingHandTransform;

            _colorMaterials = CommonUtils.GetAvatarColorMaterialsForGameObject(nonGrabbingHandTransform);

            _positionInput = handVRInputContainer.HandPosition;
            _rotationInput = handVRInputContainer.HandRotation;

            _interactor = interactor;
            _dragLocomotor = dragLocomotor;
            _snapTurn = snapTurn;
            _teleport = teleport;
            _wristUIHandler = wristUIHandler;
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
            Transform.localPosition = _positionInput.Value;
            Transform.localRotation = _rotationInput.Value;

            //Only show the hand if its actually tracking
            
            _nonGrabbingHandGO.SetActive(Transform.localPosition != Vector3.zero);

            //Rotate the hand 90 degrees along its local x axis to match the controller 
            Transform.Rotate(Vector3.right, 90, Space.Self);

            _interactor.HandleUpdate();
            _dragLocomotor.HandleUpdate();
            _snapTurn.HandleUpdate();
            _teleport.HandleUpdate();
            _wristUIHandler.HandleUpdate();
        }

        //TODO:
        /*
            When interactor tells us it's grabbed, we need to hide the hand model 
            Add some HandModel handler, wire in input to the hand model
            Figure out hand poses, should probably be orchestrated through here? 
        */
    }
}
