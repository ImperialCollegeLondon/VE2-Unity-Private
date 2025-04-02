using UnityEngine;
using UnityEngine.Audio;

public class Bouncer : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] private Vector3 _force;

    void OnCollisionEnter(Collision collision)
    {
        float magnitude = _force.magnitude;
        collision.rigidbody.AddForce(transform.TransformVector(_force.normalized)*magnitude, ForceMode.Impulse);
    }
}
