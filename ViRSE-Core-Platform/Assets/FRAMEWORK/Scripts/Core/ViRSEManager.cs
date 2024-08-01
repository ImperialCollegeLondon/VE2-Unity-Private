using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ViRSEManager : MonoBehaviour
{
    public static ViRSEManager instance;

    //public bool test; If I uncomment this, it works. If I comment it back out again, the script component gets greyed out in the inspector

    [SerializeField] private ServerType serverType;
    public ServerType ServerType => serverType;

    private void Awake()
    {
        instance = this;
    }

    void Start()
    {
        GameObject runTimePrefab = Resources.Load<GameObject>("ViRSE_Runtime");
        GameObject runTimeInstance = Instantiate(runTimePrefab);
        DontDestroyOnLoad(runTimeInstance);
    }
}

public enum ServerType
{
    Offline, 
    Local, 
    Test, 
    Prod
}
