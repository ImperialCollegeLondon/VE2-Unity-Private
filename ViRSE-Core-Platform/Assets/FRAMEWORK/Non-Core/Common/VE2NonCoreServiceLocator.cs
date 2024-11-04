using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class VE2NonCoreServiceLocator : MonoBehaviour
{
    private static VE2NonCoreServiceLocator _instance;
    public static VE2NonCoreServiceLocator Instance {
        get {
            if (_instance == null)
                _instance = FindFirstObjectByType<VE2NonCoreServiceLocator>();

            if (_instance == null && !Application.isPlaying)
                    _instance = new GameObject($"VE2NonCoreServiceLocator{SceneManager.GetActiveScene().name}").AddComponent<VE2NonCoreServiceLocator>();
            
            return _instance;
        }
    }

    //Record the gameobject name so we can re-locate multiplayer support after a domain reload
    [SerializeField, HideInInspector] private string _instanceNetworkSettingsGOName;
    private IInstanceNetworkSettingsProvider _instanceNetworkSettingsProvider;
    public IInstanceNetworkSettingsProvider InstanceNetworkSettingsProvider {
        get {
            if (_instanceNetworkSettingsProvider == null && !string.IsNullOrEmpty(_instanceNetworkSettingsGOName))
                _instanceNetworkSettingsProvider = GameObject.Find(_instanceNetworkSettingsGOName)?.GetComponent<IInstanceNetworkSettingsProvider>();

            if (_instanceNetworkSettingsProvider == null || !_instanceNetworkSettingsProvider.IsEnabled)
                return null;
            else
                return _instanceNetworkSettingsProvider;

        }
        set {
            _instanceNetworkSettingsProvider = value;

            if (value != null)
                _instanceNetworkSettingsGOName = value.GameObjectName;
        }
    }


    private void Awake()
    {
        _instance = this;
        //gameObject.hideFlags = HideFlags.HideInHierarchy; //To hide
        gameObject.hideFlags &= ~HideFlags.HideInHierarchy; //To show
    }
}
