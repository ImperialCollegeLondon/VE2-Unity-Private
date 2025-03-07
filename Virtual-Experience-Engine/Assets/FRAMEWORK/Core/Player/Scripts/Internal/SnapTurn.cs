using System;
using UnityEngine;
using VE2.Core.Player.API;

namespace VE2.Core.Player.Internal
{
    internal class SnapTurn
    {   
        private float _snapTurnAmount = 45.0f;

        private readonly SnapTurnInputContainer _inputContainer;
        private readonly Transform _rootTransform; //For rotating the player
        private readonly TeleportInputContainer _teleportInputContainer;
        private readonly FreeGrabbableWrapper _thisHandGrabbableWrapper;
        private readonly FreeGrabbableWrapper _otherHandGrabbableWrapper;
        private readonly Transform _handTransform;
        private readonly Transform _otherHandTransform;
        
        public SnapTurn(SnapTurnInputContainer inputContainer, Transform rootTransform, TeleportInputContainer teleportInputContainer, FreeGrabbableWrapper thisHandGrabbableWrapper, FreeGrabbableWrapper otherHandGrabbaleWrapper, Transform handTransform, Transform otherHandTransform)
        {
            _inputContainer = inputContainer;
            _rootTransform = rootTransform;
            _teleportInputContainer = teleportInputContainer;
            _thisHandGrabbableWrapper = thisHandGrabbableWrapper;
            _otherHandGrabbableWrapper = otherHandGrabbaleWrapper;
            _handTransform = handTransform;
            _otherHandTransform = otherHandTransform;

        }

        public void HandleUpdate() { }

        public void HandleOEnable()
        {
            _inputContainer.SnapTurnLeft.OnStickPressed += HandleSnapTurnLeft;
            _inputContainer.SnapTurnRight.OnStickPressed += HandleSnapTurnRight;
        }

        public void HandleOnDisable()
        {
            _inputContainer.SnapTurnLeft.OnStickPressed -= HandleSnapTurnLeft;
            _inputContainer.SnapTurnRight.OnStickPressed -= HandleSnapTurnRight;
        }

        private void HandleSnapTurnLeft()
        {   
            if(_teleportInputContainer.Teleport.IsPressed)
                return;

            if (_thisHandGrabbableWrapper.RangedFreeGrabInteraction != null)
                return;

            Vector3 initialHandPosition = _otherHandTransform.position;
            Quaternion initialHandRotation = _otherHandTransform.rotation;

            _rootTransform.rotation *= Quaternion.Euler(0, -_snapTurnAmount, 0);

            Vector3 finalHandPosition = _otherHandTransform.position;
            Quaternion finalHandRotation = _otherHandTransform.rotation;

            Vector3 deltaPosition = finalHandPosition - initialHandPosition;
            Quaternion deltaRotation = finalHandRotation * Quaternion.Inverse(initialHandRotation);

            if (_otherHandGrabbableWrapper.RangedFreeGrabInteraction != null)
                _otherHandGrabbableWrapper.RangedFreeGrabInteraction.ApplyDeltaWhenGrabbed(deltaPosition, deltaRotation);
        }

        private void HandleSnapTurnRight()
        {
            if (_teleportInputContainer.Teleport.IsPressed)
                return;

            if (_thisHandGrabbableWrapper.RangedFreeGrabInteraction != null)
                return;

            Vector3 initialHandPosition = _otherHandTransform.position;
            Quaternion initialHandRotation = _otherHandTransform.rotation;

            _rootTransform.rotation *= Quaternion.Euler(0, _snapTurnAmount, 0);

            Vector3 finalHandPosition = _otherHandTransform.position;
            Quaternion finalHandRotation = _otherHandTransform.rotation;

            Vector3 deltaPosition = finalHandPosition - initialHandPosition;
            Quaternion deltaRotation = finalHandRotation * Quaternion.Inverse(initialHandRotation);

            if (_otherHandGrabbableWrapper.RangedFreeGrabInteraction != null)
                _otherHandGrabbableWrapper.RangedFreeGrabInteraction.ApplyDeltaWhenGrabbed(deltaPosition, deltaRotation);
        }
    }
}
