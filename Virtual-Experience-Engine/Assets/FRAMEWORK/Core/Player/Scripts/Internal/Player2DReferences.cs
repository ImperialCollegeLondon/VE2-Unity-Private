using System;
using UnityEngine;

namespace VE2.Core.Player.Internal
{
    internal class Player2DReferences : MonoBehaviour
    {
        public Interactor2DReferences Interactor2DReferences => _interactor2DReferences;
        [SerializeField, IgnoreParent] private Interactor2DReferences _interactor2DReferences;

        public Locomotor2DReferences Locomotor2DReferences => _locomotor2DReferences;
        [SerializeField, IgnoreParent] private Locomotor2DReferences _locomotor2DReferences;
    }

    [Serializable]
    internal class Locomotor2DReferences 
    {
        public CharacterController Controller => _controller;
        [SerializeField, IgnoreParent] private CharacterController _controller;

        public Transform CameraTransform => _cameraTransform;
        [SerializeField, IgnoreParent] private Transform _cameraTransform;

        public Transform VerticalOffsetTransform => _verticalOffsetTransform;
        [SerializeField, IgnoreParent] private Transform _verticalOffsetTransform;

        public LayerMask GroundLayer => _groundLayer;
        [SerializeField, IgnoreParent] private LayerMask _groundLayer;
    }
}
