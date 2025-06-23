using UnityEngine;
using UnityEngine.InputSystem;
using VE2.Common.API;
using VE2.Common.Shared;
using VE2.NonCore.Instancing.API;

public class SpawnManagerTest : MonoBehaviour
{   
    public InterfaceReference<IV_GameObjectSpawnManager> spawnManagerReference;
    public GameObject objectToSpawn;
    public Transform spawnPosition;

    private GameObject lastGameObjectSpawnedToReturn;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        spawnManagerReference.Interface.ObjectToSpawn = objectToSpawn;
        spawnManagerReference.Interface.SpawnPosition = spawnPosition;
    }

    // Update is called once per frame
    void Update()
    {
        if (Keyboard.current.kKey.wasPressedThisFrame)
        {
            spawnManagerReference.Interface.SpawnGameObject();
        }

        if (Keyboard.current.zKey.wasPressedThisFrame)
        {
            lastGameObjectSpawnedToReturn = spawnManagerReference.Interface.SpawnAndReturnGameObject();
        }
        if (Keyboard.current.lKey.wasPressedThisFrame)
        {
            spawnManagerReference.Interface.OnDespawnTriggered(lastGameObjectSpawnedToReturn);
        }
    }
}
