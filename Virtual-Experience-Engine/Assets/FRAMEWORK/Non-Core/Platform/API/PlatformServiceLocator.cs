using UnityEngine;
using UnityEngine.SceneManagement;

public class PlatformServiceLocator : MonoBehaviour
{
    private static PlatformServiceLocator _instance;
    public static PlatformServiceLocator Instance
    { //Reload-proof singleton
        get
        {
            if (_instance == null)
                _instance = FindFirstObjectByType<PlatformServiceLocator>();

            if (_instance == null && !Application.isPlaying)
                _instance = new GameObject($"PlatformLocator{SceneManager.GetActiveScene().name}").AddComponent<PlatformServiceLocator>();

            return _instance;
        }
    }

    [SerializeField, HideInInspector] public string platformServiceGOName;
    private IPlatformService _platformService;
    public IPlatformService PlatformService
    {
        get
        {
            if (_platformService == null && !string.IsNullOrEmpty(platformServiceGOName))
                _platformService = GameObject.Find(platformServiceGOName)?.GetComponent<IPlatformService>();

                if (_platformService == null)
                {
                    Debug.LogError("PlatformService is not available");
                    return null;
                }  

                return _platformService;

        }
        set //Will need to be called externally
        {
            _platformService = value;

            if (value != null)
                platformServiceGOName = value.GameObjectName;
        }
    }

    private void Awake()
    {
        _instance = this;
        gameObject.hideFlags = HideFlags.HideInHierarchy; //To hide
        //gameObject.hideFlags &= ~HideFlags.HideInHierarchy; //To show
    }
}
