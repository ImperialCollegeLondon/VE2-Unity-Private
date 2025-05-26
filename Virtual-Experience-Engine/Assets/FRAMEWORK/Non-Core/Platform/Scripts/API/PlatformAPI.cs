using UnityEngine;
using UnityEngine.SceneManagement;

namespace VE2.NonCore.Platform.API
{
    [ExecuteAlways]
    [AddComponentMenu("")] // Prevents this MonoBehaviour from showing in the Add Component menu
    public class PlatformAPI : MonoBehaviour
    {
        private static PlatformAPI _instance;
        private static PlatformAPI Instance
        { //Reload-proof singleton
            get
            {
                if (_instance == null)
                    _instance = FindFirstObjectByType<PlatformAPI>();

                if (_instance == null && !Application.isPlaying)
                    _instance = new GameObject($"PlatformAPI-{SceneManager.GetActiveScene().name}").AddComponent<PlatformAPI>();

                return _instance;
            }
        }

        //Internal for now, unless the pluguin needs to talk to this for some reason
        internal static IPlatformService PlatformService => PlatformProvider?.PlatformService;

        [SerializeField, HideInInspector] public string platformProviderGOName;
        private IPlatformProvider _platformProvider;
        internal static IPlatformProvider PlatformProvider
        {
            private get
            {
                if (Instance._platformProvider == null && !string.IsNullOrEmpty(Instance.platformProviderGOName))
                    Instance._platformProvider = GameObject.Find(Instance.platformProviderGOName)?.GetComponent<IPlatformProvider>();

                if (Instance._platformProvider == null)
                {
                    //Debug.LogError("PlatformProvider is not available");
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
}
