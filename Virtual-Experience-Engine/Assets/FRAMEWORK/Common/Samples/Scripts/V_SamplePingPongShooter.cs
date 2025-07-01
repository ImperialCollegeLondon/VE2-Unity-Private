using UnityEngine;

public class V_SamplePingPongShooter : MonoBehaviour
{
    [SerializeField] private float _forceMagnitude = 10f;
    [SerializeField] private Transform _spawnPoint;
    [SerializeField] private GameObject _projectilePrefab;

    public void Shoot()
    {
        if (_projectilePrefab == null || _spawnPoint == null)
        {
            Debug.LogWarning("Projectile prefab or spawn point is not set.");
            return;
        }

        GameObject projectile = Instantiate(_projectilePrefab, _spawnPoint.position, _spawnPoint.rotation);
        Rigidbody rb = projectile.GetComponent<Rigidbody>();

        if (rb == null)
        {
            Debug.LogWarning("Projectile does not have a Rigidbody component.");
            Destroy(projectile);
            return;
        }

        rb.AddForce(_spawnPoint.forward * _forceMagnitude, ForceMode.Impulse);
    }
}
