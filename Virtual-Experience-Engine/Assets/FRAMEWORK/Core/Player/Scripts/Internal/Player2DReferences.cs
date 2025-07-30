using System;
using UnityEngine;

namespace VE2.Core.Player.Internal
{
    [AddComponentMenu("")] // Prevents this MonoBehaviour from showing in the Add Component menu
    internal class Player2DReferences : BasePlayerReferences
    {
        public Interactor2DReferences Interactor2DReferences => _interactor2DReferences;
        [SerializeField, IgnoreParent] private Interactor2DReferences _interactor2DReferences;

        public Locomotor2DReferences Locomotor2DReferences => _locomotor2DReferences;
        [SerializeField, IgnoreParent] private Locomotor2DReferences _locomotor2DReferences;

        public RectTransform SecondaryUIHolderRect => _secondaryUIHolderRect;
        [SerializeField, IgnoreParent] private RectTransform _secondaryUIHolderRect;

        public RectTransform OverlayUIRect => _overlayUIRect;
        [SerializeField, IgnoreParent] private RectTransform _overlayUIRect;

        public Collider CharacterCollider => _characterCollider;
        [SerializeField, IgnoreParent] private Collider _characterCollider;
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
    }
}
