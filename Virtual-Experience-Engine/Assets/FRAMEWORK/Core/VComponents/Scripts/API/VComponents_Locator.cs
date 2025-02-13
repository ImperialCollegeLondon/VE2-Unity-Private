using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using VE2.Common;

public class VComponents_Locator : MonoBehaviour
{
    private static VComponents_Locator _instance;
    public static VComponents_Locator Instance
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

    /*
        How do we actually create the WorldStateModulesContainer?
        We want to make sure we have a fresh one before the start of each scene 
        But we can't reset it in awake

        Shouldn't matter, its declared as = new();, so it should be fresh each time
    */

    private void Awake()
    {
        _instance = this;
        //gameObject.hideFlags = HideFlags.HideInHierarchy; //To hide
        gameObject.hideFlags &= ~HideFlags.HideInHierarchy; //To show
    }

    private void OnDestroy()
    {
        WorldStateModulesContainer.Reset();
    }

    public WorldStateModulesContainer WorldStateModulesContainer { get; private set; } = new();

    public void SetWorldStateModulesContainer(WorldStateModulesContainer worldStateModulesContainer) //TODO: what's this doing? Testing? If so, should be internal?
    {
        WorldStateModulesContainer = worldStateModulesContainer;
    }
}
