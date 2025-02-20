using System;
using UnityEngine;

namespace VE2.Core.Player.Internal
{
    internal class V_HandVRReferences : MonoBehaviour
    {
        public InteractorVRReferences InteractorVRReferences => _interactorVRReferences;
        [SerializeField] private InteractorVRReferences _interactorVRReferences;

        public DragLocomotorReferences LocomotorVRReferences => _locomotorVRReferences;
        [SerializeField] private DragLocomotorReferences _locomotorVRReferences;

        //TODO: AnimationController?
        //TODO: Tooltips? 
    }

[Serializable]
    internal class InteractorVRReferences : InteractorReferences
    {
        public LineRenderer LineRenderer => _lineRenderer;
        [SerializeField, IgnoreParent] private LineRenderer _lineRenderer;

        public V_CollisionDetector CollisionDetector => _collisionDetector;
        [SerializeField, IgnoreParent] private V_CollisionDetector _collisionDetector;

        public GameObject HandVisualGO => _handVisualGO;
        [SerializeField, IgnoreParent] private GameObject _handVisualGO;
    }

    [Serializable]
    public class DragLocomotorReferences
    {
        public GameObject DragIconHolder => _dragIconHolder;
        [SerializeField, IgnoreParent] public GameObject _dragIconHolder; //Entire icon

        public GameObject HorizontalDragIndicator => _horizontalDragIndicator;
        [SerializeField, IgnoreParent] public GameObject _horizontalDragIndicator;

        public GameObject VerticalDragIndicator => _verticalDragIndicator;
        [SerializeField, IgnoreParent] public GameObject _verticalDragIndicator;

        public GameObject SphereDragIcon => _sphereDragIcon;
        [SerializeField, IgnoreParent] public GameObject _sphereDragIcon;
    }
}
