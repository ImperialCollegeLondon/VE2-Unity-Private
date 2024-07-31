using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ViRSERuntime : MonoBehaviour
{
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        ViRSEManager bootConfig = ViRSEManager.instance;

        if (bootConfig.ServerType != ServerType.Offline)
        {
            //Do server stuff
        }
        else
        {
            SpawnPlayer();
        }
    }

    private void SpawnPlayer()
    {
        Transform spawnTransform = ViRSEManager.instance.transform;
        GameObject playerRigPrefab = AssetLoader.Instance.LocalPlayerRigPrefab;
        Instantiate(playerRigPrefab, spawnTransform.position, spawnTransform.rotation, transform);
    }
}
