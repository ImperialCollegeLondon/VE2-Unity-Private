using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VE2.NonCore.Instancing.API
{
    [ExecuteAlways]
    public class InstancingAPI : MonoBehaviour
    {
        private static InstancingAPI _instance;
        private static InstancingAPI Instance
        { //Reload-proof InstancingAPI
            get
            {
                if (_instance == null)
                    _instance = FindFirstObjectByType<InstancingAPI>();

                if (_instance == null && !Application.isPlaying)
                    _instance = new GameObject($"InstancingAPI{SceneManager.GetActiveScene().name}").AddComponent<InstancingAPI>();

                return _instance;
            }
        }

        public static IInstanceService InstanceService => InstanceProvider?.InstanceService;

        [SerializeField, HideInInspector] private string _instanceProviderGOName;
        private IInstanceProvider _instanceProvider;
        internal static IInstanceProvider InstanceProvider
        {
            get
            {
                if (Instance == null)
                    return null;

                if (Instance._instanceProvider == null && !string.IsNullOrEmpty(Instance._instanceProviderGOName))
                {
                    GameObject instanceProviderGO = GameObject.Find(Instance._instanceProviderGOName);

                    if (instanceProviderGO != null)
                        Instance._instanceProvider = instanceProviderGO.GetComponent<IInstanceProvider>();
                }

                if (Instance._instanceProvider == null)
                {
                    Debug.LogError("InstanceService is not available");
                    return null;
                }  

                return Instance._instanceProvider;

            }
            set //Will need to be called externally
            {
                Instance._instanceProvider = value;

                if (value != null)
                    Instance._instanceProviderGOName = value.GameObjectName;
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
