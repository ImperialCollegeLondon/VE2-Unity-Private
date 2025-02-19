using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using VE2.Common;

public class VComponentsAPI : MonoBehaviour
{
    private static VComponentsAPI _instance;
    private static VComponentsAPI Instance
    { //Reload-proof singleton
        get
        {
            if (_instance == null)
                _instance = FindFirstObjectByType<VComponentsAPI>();

            if (_instance == null)
                _instance = new GameObject($"VComponentsAPI-{SceneManager.GetActiveScene().name}").AddComponent<VComponentsAPI>();

            return _instance;
        }
    }

    public static bool HasMultiPlayerSupport => WorldStateSyncProvider != null && WorldStateSyncProvider.IsEnabled;
    public static IWorldStateSyncService WorldStateSyncService => WorldStateSyncProvider?.WorldStateSyncService;

    [SerializeField, HideInInspector] private string _worldStateSyncProviderGOName;
    private IWorldStateSyncProvider _worldStateSyncProvider;
    internal static IWorldStateSyncProvider WorldStateSyncProvider {
        get 
        {
            if (Instance._worldStateSyncProvider == null && !string.IsNullOrEmpty(Instance._worldStateSyncProviderGOName))
                Instance._worldStateSyncProvider = GameObject.Find(Instance._worldStateSyncProviderGOName)?.GetComponent<IWorldStateSyncProvider>();

            return Instance._worldStateSyncProvider;
        }
        set
        {
            Instance._worldStateSyncProvider = value;

            if (value != null)
                Instance._worldStateSyncProviderGOName = value.GameObjectName;
        }
    }

    private void Awake()
    {
        _instance = this;
        //gameObject.hideFlags = HideFlags.HideInHierarchy; //To hide
        gameObject.hideFlags &= ~HideFlags.HideInHierarchy; //To show
    }
}
