using UnityEngine;
using VE2.Core.Player.InteractionFinders;

public class V_HandVRReferences : MonoBehaviour
{
    public LineRenderer LineRenderer => _lineRenderer;
    [SerializeField] private LineRenderer _lineRenderer;

    public Transform RayOrigin => _rayOrigin;
    [SerializeField] private Transform _rayOrigin; 

    public V_CollisionDetector CollisionDetector => _collisionDetector;
    [SerializeField] private V_CollisionDetector _collisionDetector;

    //TODO: Collision detector 
    //TODO: AnimationController?
    //TODO: Raycast start pos
    //TODO: Tooltips? 
}
