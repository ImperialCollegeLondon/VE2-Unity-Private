using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using ViRSE.Core.Shared;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class ViRSENonCoreServiceLocator : MonoBehaviour
{
    private static ViRSENonCoreServiceLocator _instance;
    public static ViRSENonCoreServiceLocator Instance {
        get {
            if (_instance == null)
                _instance = FindFirstObjectByType<ViRSENonCoreServiceLocator>();

            if (_instance == null && !Application.isPlaying)
                    _instance = new GameObject($"ViRSENonCoreServiceLocator{SceneManager.GetActiveScene().name}").AddComponent<ViRSENonCoreServiceLocator>();
            
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
        gameObject.hideFlags = HideFlags.HideInHierarchy; //To hide
        //gameObject.hideFlags &= ~HideFlags.HideInHierarchy; //To show
    }
}
