using UnityEngine;
using UnityEngine.SceneManagement;

public class PlatformServiceLocator : MonoBehaviour
{
    private static PlatformServiceLocator _instance;
    private static PlatformServiceLocator Instance
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

    internal static IPlatformService PlatformService => PlatformProvider.PlatformService;

    [SerializeField, HideInInspector] public string platformProviderGOName;
    private IPlatformProvider _platformProvider;
    internal static IPlatformProvider PlatformProvider
    {
        get
        {
            if (Instance._platformProvider == null && !string.IsNullOrEmpty(Instance.platformProviderGOName))
                Instance._platformProvider = GameObject.Find(Instance.platformProviderGOName)?.GetComponent<IPlatformProvider>();

                if (Instance._platformProvider == null)
                {
                    Debug.LogError("PlatformService is not available");
                    return null;
                }  

                return Instance._platformProvider;

        }
        set //Will need to be called externally
        {
            Instance._platformProvider = value;

            if (value != null)
                Instance.platformProviderGOName = value.GameObjectName;
        }
    }

    private void Awake()
    {
        _instance = this;
        gameObject.hideFlags = HideFlags.HideInHierarchy; //To hide
        //gameObject.hideFlags &= ~HideFlags.HideInHierarchy; //To show
    }
}
