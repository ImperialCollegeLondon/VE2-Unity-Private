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
    private LayerMask _groundLayerMask => LayerMask.GetMask("Ground");
    private bool _isTeleporting;
    private Vector2 _currentTeleportDirection;
    private Vector2 _previousTeleportDirection;
    public Teleport(TeleportInputContainer inputContainer, Transform rootTransform, Transform teleportRaycastOrigin)
    {
        _inputContainer = inputContainer;
        _rootTransform = rootTransform;
        _teleportRaycastOrigin = teleportRaycastOrigin;
    }

    public void HandleUpdate()
    {
        if (_isTeleporting)
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
        _isTeleporting = true;
    }

    private void HandleTeleportDeactivated()
    {
        // Teleport User
        if (_isTeleporting)
        {
            Debug.Log("Teleport Deactivated");
            _rootTransform.SetPositionAndRotation(_hitPoint, _teleportTargetRotation);
            Debug.Log($"Teleporting to: {_hitPoint} with Rotation of {_teleportTargetRotation.eulerAngles}");
        }
        CancelTeleport();
    }

    private void CastTeleportRay()
    {
        Vector3 startPosition = _teleportRaycastOrigin.position;
        Vector3 direction = _teleportRaycastOrigin.forward;
        _teleportRaycastOrigin.gameObject.SetActive(false);
        if (Physics.Raycast(startPosition, direction, out RaycastHit hit, _teleportRayDistance, _groundLayerMask))
        {
            _hitPoint = hit.point;
            _reticle.transform.position = _hitPoint;
            // Update the line renderer positions
            _lineRenderer.SetPosition(0, startPosition);
            _lineRenderer.SetPosition(1, _hitPoint);
            _lineRenderer.material.color = Color.green;
            _reticle.SetActive(true);
            UpdateTargetRotation();
            //Debug.Log("Teleport ray hit the ground at: " + _hitPoint);
        }
        else
        {
            // Update the line renderer positions
            _lineRenderer.SetPosition(0, startPosition);
            if (Physics.Raycast(startPosition, direction, out RaycastHit otherhit, _teleportRayDistance))
            {
                _lineRenderer.SetPosition(1, otherhit.point);
            }
            else
            {
                _lineRenderer.SetPosition(1, direction * _teleportRayDistance);
            }

            _reticle.SetActive(false);
            _arrowObject.SetActive(false);
            _lineRenderer.material.color = Color.red;
            //Debug.Log("Teleport ray hit the object at: " + hit.point);
        }
        _lineRendererObject.SetActive(true);
    }

    private void CancelTeleport()
    {
        _reticle.SetActive(false);
        _lineRendererObject.SetActive(false);
        _arrowObject.SetActive(false);
        _isTeleporting = false;
        _teleportRaycastOrigin.gameObject.SetActive(true);
    }

    private void CreateTeleportRayAndReticle()
    {
        if (_lineRendererObject == null)
        {
            _lineRendererObject = new GameObject("LineRendererObject");
            _lineRenderer = _lineRendererObject.AddComponent<LineRenderer>();
            _lineRenderer.positionCount = 2;
            _lineRenderer.startWidth = 0.01f;
            _lineRenderer.endWidth = 0.01f;
            _lineRenderer.material = new Material(Shader.Find("Unlit/Color"));
            _lineRenderer.material.color = Color.yellow;
            _lineRendererObject.SetActive(false);
            _lineRendererObject.transform.SetParent(_teleportRaycastOrigin.parent);
            _lineRendererObject.transform.localPosition = Vector3.zero;
            _lineRendererObject.transform.localRotation = Quaternion.identity;
        }

        if (_reticle == null)
        {
            _reticle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _reticle.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
            Material reticleMaterial = new Material(Shader.Find("Unlit/Color"));
            reticleMaterial.color = new Color(0f, 1f, 0f, 0.1f); 
            _reticle.GetComponent<Renderer>().material = reticleMaterial;
        }

        _reticle.SetActive(false);

        if (_arrowObject == null)
        {
            _arrowObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _arrowObject.transform.localScale = new Vector3(0.1f, 0.05f, 0.25f);
            _arrowObject.GetComponent<Renderer>().material = new Material(Shader.Find("Unlit/Color"));
            _arrowObject.GetComponent<Renderer>().material.color = Color.blue;
        }
        _arrowObject.SetActive(false);
    }

    private void UpdateTargetRotation()
    {
        _previousTeleportDirection = _currentTeleportDirection;
        _currentTeleportDirection = _inputContainer.TeleportDirection.Value;

        float rotationAngle = Mathf.Atan2(_previousTeleportDirection.x, _previousTeleportDirection.y) * Mathf.Rad2Deg;
        _teleportTargetRotation = Quaternion.Euler(0, rotationAngle, 0);

        Debug.Log($"Previous Teleport Direction: {_previousTeleportDirection}, Current Teleport Direction: {_currentTeleportDirection}, Teleport Rotation: {rotationAngle}");
        if (_arrowObject != null)
        {
            _arrowObject.SetActive(true);
            _arrowObject.transform.SetPositionAndRotation(_hitPoint + _arrowObject.transform.forward * (_arrowObject.transform.localScale.z / 2), _teleportTargetRotation);
        }
    }
}
