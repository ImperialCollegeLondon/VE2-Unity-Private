using UnityEngine;
using VE2.Core.Player.InteractionFinders;

public class V_HandVRReferences : MonoBehaviour
{
    public LineRenderer LineRenderer => _lineRenderer;
    [SerializeField] private LineRenderer _lineRenderer;

    public Transform RayOrigin => _rayOrigin;
    [SerializeField] private Transform _rayOrigin; 

    public GameObject HandVisualGO => _handVisualGO;
    [SerializeField] private GameObject _handVisualGO;

    public V_CollisionDetector CollisionDetector => _collisionDetector;
    [SerializeField] private V_CollisionDetector _collisionDetector;

    public GameObject DragIconHolder => _dragIconHolder;
    [SerializeField] public GameObject _dragIconHolder; //Entire icon

    public GameObject HorizontalDragIndicator => _horizontalDragIndicator;
    [SerializeField] public GameObject _horizontalDragIndicator;

    public GameObject VerticalDragIndicator => _verticalDragIndicator;
    [SerializeField] public GameObject _verticalDragIndicator;

    public GameObject SphereDragIcon => _sphereDragIcon;
    [SerializeField] public GameObject _sphereDragIcon;

    //TODO: AnimationController?
    //TODO: Raycast start pos
    //TODO: Tooltips? 
}
