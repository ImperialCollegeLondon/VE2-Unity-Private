using UnityEngine;

internal class V_ImpulseImparter : MonoBehaviour
{
    [SerializeField] private float _forceMagnitude = 10f;
    [SerializeField] private Vector3 _forceLocalDirection = Vector3.up;

    public void ImpartForce()
    {
        Rigidbody targetRigidbody = GetComponent<Rigidbody>();

        if (targetRigidbody == null)
        {
            Debug.LogWarning("Target Rigidbody is null. Cannot impart force.");
            return;
        }

        Vector3 forceDirection = transform.TransformDirection(_forceLocalDirection);
        targetRigidbody.AddForce(forceDirection * _forceMagnitude, ForceMode.Impulse);
    }
}
