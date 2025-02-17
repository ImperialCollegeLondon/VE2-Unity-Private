using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using VE2.Common;

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
                _instance = new GameObject($"InstanceLocator{SceneManager.GetActiveScene().name}").AddComponent<InstancingAPI>();

            return _instance;
        }
    }

    internal static IInstanceService InstanceService => InstanceProvider.InstanceService;

    [SerializeField, HideInInspector] private string _instanceProviderGOName;
    private IInstanceProvider _instanceProvider;
    internal static IInstanceProvider InstanceProvider
    {
        get
        {
            if (Instance._instanceProvider == null && !string.IsNullOrEmpty(Instance._instanceProviderGOName))
                Instance._instanceProvider = GameObject.Find(Instance._instanceProviderGOName)?.GetComponent<IInstanceProvider>();

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
