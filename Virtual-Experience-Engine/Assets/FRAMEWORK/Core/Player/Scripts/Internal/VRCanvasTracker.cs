using UnityEngine;

public class VRCanvasTracker : MonoBehaviour
{
    [SerializeField] private Transform _cameraTransform; // Assign the player's camera here.
    [SerializeField] private float _distanceFromCamera = 1.5f;
    [SerializeField] private float _angleTolerance = 30f; // Tolerance in degrees.
    [SerializeField] private float _distanceTolerance = 0.2f; // Tolerance in meters.
    [SerializeField] private float _moveSpeed = 5;

    private Vector3 targetLocalPosition;
    private Quaternion targetLocalRotation;

    void Update()
    {
        Transform parent = _cameraTransform.parent;
        Vector3 cameraForward = parent.InverseTransformDirection(new Vector3(_cameraTransform.forward.x, 0, _cameraTransform.forward.z).normalized);
        Vector3 canvasForward = transform.localRotation * Vector3.forward;
        canvasForward.y = 0; // Ensure it's XZ plane

        if (NeedsRepositioning(cameraForward, canvasForward))
            UpdateTargetTransform(cameraForward);

        SmoothMoveToTarget();
    }

    private bool NeedsRepositioning(Vector3 cameraForward, Vector3 canvasForward)
    {
        float angleDot = Vector3.Dot(cameraForward, canvasForward.normalized); // More efficient than Angle()
        bool angleExceeded = angleDot < Mathf.Cos(_angleTolerance * Mathf.Deg2Rad); // Compare against cosine of tolerance

        float localDistance = Vector3.Distance(transform.localPosition, _cameraTransform.localPosition);
        bool distanceExceeded = Mathf.Abs(localDistance - _distanceFromCamera) > _distanceTolerance;

        return angleExceeded || distanceExceeded;
    }

    private void UpdateTargetTransform(Vector3 cameraForward)
    {
        targetLocalPosition = cameraForward * _distanceFromCamera + _cameraTransform.localPosition;
        targetLocalRotation = Quaternion.LookRotation(cameraForward, Vector3.up);
    }

    private void SmoothMoveToTarget()
    {
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetLocalPosition, Time.deltaTime * _moveSpeed);
        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetLocalRotation, Time.deltaTime * _moveSpeed);
    }
}

