using System;
using UnityEngine;
using VE2.Core.Common;
using VE2.Core.Player;
public class Teleport
{
    private readonly TeleportInputContainer _inputContainer;
    private readonly Transform _rootTransform; // For rotating the player
    private readonly Transform _teleportRaycastOrigin; // Position of the teleport raycast origin

    private GameObject _reticle;
    private LineRenderer _lineRenderer;
    private GameObject _lineRendererObject;
    private GameObject _arrowObject;
    private Vector3 _hitPoint;
    private float _teleportRayDistance = 50f;
    private Quaternion _teleportTargetRotation;
    private LayerMask _groundLayerMask => LayerMask.GetMask("Traversible");
    private Vector2 _currentTeleportDirection;
    private int _lineSegmentCount = 20; // Number of segments in the Bezier curve
    private float _maxSlopeAngle = 45f; // Maximum slope angle in degrees

    public Teleport(TeleportInputContainer inputContainer, Transform rootTransform, Transform teleportRaycastOrigin)
    {
        _inputContainer = inputContainer;
        _rootTransform = rootTransform;
        _teleportRaycastOrigin = teleportRaycastOrigin;
    }

    public void HandleUpdate()
    {
        if (_inputContainer.Teleport.IsPressed)
        {
            CastTeleportRay();
        }
    }

    public void HandleOEnable()
    {
        CreateTeleportRayAndReticle();

        _inputContainer.Teleport.OnPressed += HandleTeleportActivated;
        _inputContainer.Teleport.OnReleased += HandleTeleportDeactivated;
    }

    public void HandleOnDisable()
    {
        _inputContainer.Teleport.OnPressed -= HandleTeleportActivated;
        _inputContainer.Teleport.OnReleased -= HandleTeleportDeactivated;

        //CancelTeleport();
    }

    private void HandleTeleportActivated()
    {
        // Enable User For Teleport
        Debug.Log("Teleport Activated");
    }

    private void HandleTeleportDeactivated()
    {
        // Teleport User
        Debug.Log("Teleport Deactivated");
        _rootTransform.position = _hitPoint;
        _rootTransform.rotation = _arrowObject.transform.rotation;
        Debug.Log($"Teleporting to: {_hitPoint} with Rotation of {_teleportTargetRotation.eulerAngles}");
        CancelTeleport();
    }

    private void CastTeleportRay()
    {
        Vector3 startPosition = _teleportRaycastOrigin.position;
        Vector3 direction = _teleportRaycastOrigin.forward;
        _teleportRaycastOrigin.gameObject.SetActive(false);

        if (Physics.Raycast(startPosition, direction, out RaycastHit hit, _teleportRayDistance, _groundLayerMask))
        {
            if (IsValidSurface(hit.normal))
            {
                _hitPoint = hit.point;
                _reticle.transform.position = _hitPoint;
                Vector3 arrowDirection = _teleportRaycastOrigin.forward;
                arrowDirection.y = 0;
                if (arrowDirection != Vector3.zero)
                {
                    arrowDirection.Normalize();
                }
                _arrowObject.transform.rotation = Quaternion.LookRotation(arrowDirection, Vector3.up);
                _arrowObject.transform.position = _hitPoint + direction * (_arrowObject.transform.localScale.z / 2) + Vector3.up * (_arrowObject.transform.localScale.y / 2);

                // Update the line renderer positions
                DrawBezierCurve(startPosition, _hitPoint);
                _lineRenderer.material.color = Color.green;

                _reticle.SetActive(true);
                _arrowObject.SetActive(true);
                UpdateTargetRotation();
            }
            else
            {
                DrawBezierCurve(startPosition, hit.point);
                // Surface is not valid for teleportation
                _reticle.SetActive(false);
                _arrowObject.SetActive(false);
                _lineRenderer.material.color = Color.red;

            }
        }
        else
        {
            // Update the line renderer positions
            if (Physics.Raycast(startPosition, direction, out RaycastHit otherhit, _teleportRayDistance))
            {
                DrawBezierCurve(startPosition, otherhit.point);
            }
            else
            {
                DrawBezierCurve(startPosition, direction * _teleportRayDistance);
            }

            _reticle.SetActive(false);
            _arrowObject.SetActive(false);
            _lineRenderer.material.color = Color.red;
        }
        _lineRendererObject.SetActive(true);
    }

    private bool IsValidSurface(Vector3 normal)
    {
        // Check if the surface normal is within the acceptable slope angle
        float angle = Vector3.Angle(Vector3.up, normal);
        return angle <= _maxSlopeAngle;
    }

    private void CancelTeleport()
    {
        _reticle.SetActive(false);
        _lineRendererObject.SetActive(false);
        _arrowObject.SetActive(false);
        _teleportRaycastOrigin.gameObject.SetActive(true);
    }

    private void CreateTeleportRayAndReticle()
    {
        if (_lineRendererObject == null)
        {
            _lineRendererObject = new GameObject("LineRendererObject");
            _lineRenderer = _lineRendererObject.AddComponent<LineRenderer>();
            _lineRenderer.positionCount = _lineSegmentCount + 1;
            _lineRenderer.startWidth = 0.01f;
            _lineRenderer.endWidth = 0.01f;
            _lineRenderer.material = new Material(Shader.Find("Unlit/Color"));
            _lineRenderer.material.color = Color.yellow;
            _lineRendererObject.SetActive(false);
            _lineRendererObject.transform.SetParent(_teleportRaycastOrigin.parent);
            _lineRendererObject.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        }

        if (_reticle == null)
        {
            _reticle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _reticle.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
            Material reticleMaterial = new Material(Shader.Find("Unlit/Color"));
            _reticle.GetComponent<Renderer>().material = new Material(Shader.Find("Unlit/Color"));
            _reticle.GetComponent<Renderer>().material.color = Color.green;
        }

        _reticle.SetActive(false);

        if (_arrowObject == null)
        {
            _arrowObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _arrowObject.transform.localScale = new Vector3(0.1f, 0.1f, 0.25f);
            _arrowObject.GetComponent<Renderer>().material = new Material(Shader.Find("Unlit/Color"));
            _arrowObject.GetComponent<Renderer>().material.color = Color.yellow;
        }
        _arrowObject.SetActive(false);
    }

    private void UpdateTargetRotation()
    {
        _currentTeleportDirection = _inputContainer.TeleportDirection.Value;
        float rotationAngle = Mathf.Atan2(_currentTeleportDirection.x, _currentTeleportDirection.y) * Mathf.Rad2Deg;
        _teleportTargetRotation = Quaternion.Euler(0, rotationAngle, 0);

        Debug.Log($"Current Teleport Direction: {_currentTeleportDirection}, Teleport Rotation: {rotationAngle}");

        _arrowObject.transform.rotation *= _teleportTargetRotation;
    }

    private void DrawBezierCurve(Vector3 startPosition, Vector3 endPosition)
    {
        Vector3 controlPoint = (startPosition + endPosition) / 2 + Vector3.up * 2; // Control point for the Bezier curve

        for (int i = 0; i <= _lineSegmentCount; i++)
        {
            float t = i / (float)_lineSegmentCount;
            Vector3 position = CalculateQuadraticBezierPoint(t, startPosition, controlPoint, endPosition);
            _lineRenderer.SetPosition(i, position);
        }
    }

    private Vector3 CalculateQuadraticBezierPoint(float t, Vector3 startPosition, Vector3 controlPoint, Vector3 endPosition)
    {
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;

        Vector3 p = uu * startPosition; // (1-t)^2 * startPosition
        p += 2 * u * t * controlPoint; // 2 * (1-t) * t * controlPoint
        p += tt * endPosition; // t^2 * endPosition

        return p;
    }
}
