using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ViRSERuntime : MonoBehaviour
{
    [SerializeField] private GameObject primaryServerServicePrefab;
    private GameObject primaryServerService;

    [SerializeField] private GameObject localPlayerRigPrefab;
    private GameObject localPlayerRig;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        ViRSEManager bootConfig = ViRSEManager.instance;
        Debug.Log("Boot " + bootConfig.ServerType.ToString());

        if (bootConfig.ServerType != ServerType.Offline)
        {
            Debug.Log("Make server");
            PrimaryServerService primaryServerService = Instantiate(primaryServerServicePrefab).GetComponent<PrimaryServerService>();
            DontDestroyOnLoad(primaryServerService);
            //primaryServerService. //Needs to be fed the networking type
        }
        else
        {
            Debug.Log("Make player");
            SpawnPlayer();
        }
    }


    private void SpawnPlayer()
    {
        Transform spawnTransform = ViRSEManager.instance.transform;
        GameObject localPlayerRig = Instantiate(localPlayerRigPrefab, spawnTransform.position, spawnTransform.rotation, transform);
        DontDestroyOnLoad(localPlayerRig);
    }
}
