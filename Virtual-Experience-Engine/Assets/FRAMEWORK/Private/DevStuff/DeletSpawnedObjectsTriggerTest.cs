using UnityEngine;
using VE2.Core.VComponents.API;

public class DeletSpawnedObjectsTrigger : MonoBehaviour
{
    SpawnManagerTest spawnManagerTest;

    private void Start()
    {
        spawnManagerTest = FindFirstObjectByType<SpawnManagerTest>();

        if (spawnManagerTest == null)
            Debug.LogError("SpawnManagerTest not found in the scene.");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<IV_FreeGrabbable>() != null)
        {
            spawnManagerTest.spawnManagerReference.Interface.DespawnGameObject(other.gameObject);
        }
    }
}
