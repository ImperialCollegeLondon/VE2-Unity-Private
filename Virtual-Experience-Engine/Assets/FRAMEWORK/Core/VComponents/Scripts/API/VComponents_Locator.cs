using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using VE2.Common;

public class VComponents_Locator : MonoBehaviour
{
    private static VComponents_Locator _instance;
    private static VComponents_Locator Instance
    { //Reload-proof singleton
        get
        {
            if (_instance == null)
                _instance = FindFirstObjectByType<VComponents_Locator>();

            if (_instance == null)
                _instance = new GameObject($"VComponents_Locator{SceneManager.GetActiveScene().name}").AddComponent<VComponents_Locator>();

            return _instance;
        }
    }

    public static bool HasMultiPlayerSupport => WorldStateSyncProvider != null;
    public static IWorldStateSyncService WorldStateSyncService => WorldStateSyncProvider?.WorldStateSyncService;

    [SerializeField, HideInInspector] public string _worldStateSyncProviderGOName;
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

    //Should store a provider 
    //Expose the IPlayerSyncService through that the provider 
    //That has the IPlayerSyncService interface (pipes through the InstanceService)
    //IPlayerSyncService has the IsConnected and OnConnected, LocalClientID, and (De/)RegisterLocalPlayer 
    //IWorldStateSyncService has just (De/)RegisterWorldStateModule
    //Theres also IInstanceService, which has IsHost, OnBecomeHost, LocalClientID
    //There's also also InstanceServiceInternal could just live internally
    //ALL THE THINGS ARE SYNCRONOUS!! Order of execution shouldn't matter

    private void Awake()
    {
        _instance = this;
        //gameObject.hideFlags = HideFlags.HideInHierarchy; //To hide
        gameObject.hideFlags &= ~HideFlags.HideInHierarchy; //To show
    }

    // private void OnDestroy()
    // {
    //     WorldStateModulesContainer.Reset();
    // }

    // private WorldStateModulesContainer _worldStateModulesContainer = new();
    // public static WorldStateModulesContainer WorldStateModulesContainer { get => Instance._worldStateModulesContainer; private set => Instance._worldStateModulesContainer = value; } //TODO: Internal
}
