using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using ViRSE.Core.Shared;

[ExecuteInEditMode]
public class ViRSECoreServiceLocator : MonoBehaviour
{
    private static ViRSECoreServiceLocator _instance;
    public static ViRSECoreServiceLocator Instance { //Reload-proof singleton
        get {
            //Debug.Log("GeT CORE LOCATOR");
            if (_instance == null)
                _instance = FindFirstObjectByType<ViRSECoreServiceLocator>();

            if (_instance == null)
            {
                    _instance = new GameObject($"ViRSECoreServiceLocator{SceneManager.GetActiveScene().name}").AddComponent<ViRSECoreServiceLocator>();
                //if (!Application.isPlaying)
                //{
                //    Debug.Log("<color=yellow>MADE NEW CORE LOCATOR</color> " + SceneManager.GetActiveScene().name);
                //}
                //else
                //    Debug.Log("<color=red>TRIED TO CREATE A NEW CORE LOCATOR AT RUNTIME</color>");
            }

            return _instance;
        }
    }

    //Record the gameobject name so we can re-locate multiplayer support after a domain reload
    [SerializeField, HideInInspector] private string _multiplayerSupportGOName;
    private IMultiplayerSupport _multiPlayerSupport;
    public IMultiplayerSupport MultiplayerSupport {
        get {
            if (_multiPlayerSupport == null && !string.IsNullOrEmpty(_multiplayerSupportGOName))
                _multiPlayerSupport = GameObject.Find(_multiplayerSupportGOName)?.GetComponent<IMultiplayerSupport>();

            if (_multiPlayerSupport != null && !_multiPlayerSupport.IsEnabled)
                return null;
            else
                return _multiPlayerSupport;
        }
        set //Will need to be called externally
        {
            _multiPlayerSupport = value;

            if (value != null)
                _multiplayerSupportGOName = value.MultiplayerSupportGameObjectName;
        }
    }

    //Record the gameobject name so we can re-locate multiplayer support after a domain reload
    [SerializeField, HideInInspector] public string PlayerSettingsproviderGOName { get; private set; }
    private IPlayerSettingsProvider _playerSettingsProvider;
    public IPlayerSettingsProvider PlayerSettingsProvider {
        get {
            if (_playerSettingsProvider == null && !string.IsNullOrEmpty(PlayerSettingsproviderGOName))
                _playerSettingsProvider = GameObject.Find(PlayerSettingsproviderGOName)?.GetComponent<IPlayerSettingsProvider>();

            if (_playerSettingsProvider != null && !_playerSettingsProvider.IsEnabled)
                return null;
            else 
                return _playerSettingsProvider;
        }
        set //Will need to be called externally
        {
            _playerSettingsProvider = value;

            if (value != null)
                PlayerSettingsproviderGOName = value.GameObjectName;
        }
    }

    private void Awake()
    {
        //Debug.Log("awake core");
        _instance = this;
        //gameObject.hideFlags = HideFlags.HideInHierarchy; //To hide
        gameObject.hideFlags &= ~HideFlags.HideInHierarchy; //To show
    }

    private void OnDisable()
    {
        //Debug.Log("SCENE CHANGE core");
        _instance = null;
    }

}
