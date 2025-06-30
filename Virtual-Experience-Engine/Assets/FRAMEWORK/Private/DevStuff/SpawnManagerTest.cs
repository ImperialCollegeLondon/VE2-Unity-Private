using UnityEngine;
using UnityEngine.InputSystem;
using VE2.Common.API;
using VE2.Common.Shared;
using VE2.Core.VComponents.API;
using VE2.NonCore.Instancing.API;

public class SpawnManagerTest : MonoBehaviour
{   
    public InterfaceReference<IV_GameObjectSpawnManager> spawnManagerReference;
    public GameObject objectToSpawn;
    public Transform spawnPosition;

    private GameObject lastGameObjectSpawnedToReturn;
    private int counter = 0;
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
            InterfaceReference<IV_FreeGrabbable> freeGrabbable = spawnManagerReference.Interface.SpawnAndReturnGameObject().GetComponent<InterfaceReference<IV_FreeGrabbable>>();
            freeGrabbable.MonoBehaviour.enabled = false;
            freeGrabbable.GameObject.name = "FreeGrabbableObject_" + counter;
            freeGrabbable.MonoBehaviour.enabled = true;
            counter++;
        }
        if (Keyboard.current.lKey.wasPressedThisFrame)
        {
            spawnManagerReference.Interface.DespawnGameObject(lastGameObjectSpawnedToReturn);
        }
    }
}
