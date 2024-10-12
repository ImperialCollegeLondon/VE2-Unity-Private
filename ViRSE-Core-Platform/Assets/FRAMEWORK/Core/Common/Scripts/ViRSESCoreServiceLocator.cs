using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using ViRSE.Core.Shared;
using static ViRSE.Core.Shared.CoreCommonSerializables;

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
                Debug.Log("MADE NEW CORE LOCATOR");
                _instance = new GameObject($"ViRSECoreServiceLocator{SceneManager.GetActiveScene().name}").AddComponent<ViRSECoreServiceLocator>();
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

            if (_multiPlayerSupport == null || !_multiPlayerSupport.IsEnabled)
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
    //[SerializeField] public string testString;
    [SerializeField] public string PlayerSettingsProviderGOName; //{ get; private set; }
    private IPlayerSettingsProvider _playerSettingsProvider;
    public IPlayerSettingsProvider PlayerSettingsProvider {
        get {

            if (_playerSettingsProvider == null && !string.IsNullOrEmpty(PlayerSettingsProviderGOName))
                _playerSettingsProvider = GameObject.Find(PlayerSettingsProviderGOName)?.GetComponent<IPlayerSettingsProvider>();

            if (_playerSettingsProvider == null || !_playerSettingsProvider.IsEnabled)
                return null;
            else 
                return _playerSettingsProvider;
        }
        set //Will need to be called externally
        {
            _playerSettingsProvider = value;

            if (value != null)
            {
                PlayerSettingsProviderGOName = value.GameObjectName;
            }
        }
    }

    //[SerializeField] private string testString;
    [SerializeField] public string PlayerOverridesProviderGOName; // { get; private set; }
    private IPlayerAppearanceOverridesProvider _playerOverridesProvider;
    public IPlayerAppearanceOverridesProvider PlayerAppearanceOverridesProvider
    {
        get
        {
            if (_playerOverridesProvider == null && !string.IsNullOrEmpty(PlayerOverridesProviderGOName))
                _playerOverridesProvider = GameObject.Find(PlayerOverridesProviderGOName)?.GetComponent<IPlayerAppearanceOverridesProvider>();

            if (_playerOverridesProvider == null || !_playerOverridesProvider.IsEnabled)
                return null;
            else
                return _playerOverridesProvider;
        }
        set //Will need to be called externally
        {
            _playerOverridesProvider = value;

            if (value != null)
                PlayerOverridesProviderGOName = value.GameObjectName;
        }
    }

    [SerializeField] public string PlayerSpawnerGOName; // { get; private set; }
    private IPlayerSpawner _playerSpawner;
    public IPlayerSpawner PlayerSpawner
    {
        get
        {
            if (_playerSpawner == null && !string.IsNullOrEmpty(PlayerSpawnerGOName))
                _playerSpawner = GameObject.Find(PlayerSpawnerGOName)?.GetComponent<IPlayerSpawner>();

            if (_playerSpawner == null || !_playerSpawner.IsEnabled)
                return null;
            else
                return _playerSpawner;
        }
        set //Will need to be called externally
        {
            _playerSpawner = value;

            if (value != null)
                PlayerSpawnerGOName = value.GameObjectName;
        }
    }


    //Record the gameobject name so we can re-locate multiplayer support after a domain reload
    // [SerializeField, HideInInspector] public string PlayerSpawnerGOName { get; private set; }
    // private V_PlayerSpawner _playerSettingsProvider;
    // public IPlayerSettingsProvider PlayerSettingsProvider
    // {
    //     get
    //     {
    //         if (_playerSettingsProvider == null && !string.IsNullOrEmpty(PlayerSettingsproviderGOName))
    //             _playerSettingsProvider = GameObject.Find(PlayerSettingsproviderGOName)?.GetComponent<IPlayerSettingsProvider>();

    //         if (_playerSettingsProvider != null && !_playerSettingsProvider.IsEnabled)
    //             return null;
    //         else
    //             return _playerSettingsProvider;
    //     }
    //     set //Will need to be called externally
    //     {
    //         _playerSettingsProvider = value;

    //         if (value != null)
    //             PlayerSettingsproviderGOName = value.GameObjectName;
    //     }
    // }

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

public interface IPlayerAppearanceOverridesProvider
{
    public PlayerPresentationOverrides PlayerPresentationOverrides { get; }
    public bool IsEnabled { get; }
    public string GameObjectName { get; }

    public void NotifyProviderOfChangeAppearanceOverrides();
    public event Action OnAppearanceOverridesChanged;

    public GameObject GetHeadOverrideGO(AvatarAppearanceOverrideType overrideType);
    public GameObject GetTorsoOverrideGO(AvatarAppearanceOverrideType overrideType);
}

public interface IPlayerSpawner //TODO, maybe this is where the instance service gets the player data from?
{
    public bool IsEnabled { get; }
    public string GameObjectName { get; }

    //Unlike the other components, the player spawner should be able to 
    //Activate and deactivate at runtime
    public event Action OnEnabledStateChanged;
}

/*
    As the player turns on and off, we need to notify the InstanceService so we change the InstancedAppearance 


    Someone might use the playersettingsprovider outside of the player rig 
    Anyway, we need to talk to the player spawner because the player spawner should turn on and off 

    The spawner being enabled tells us whether UsingViRSEAvatar is true 
    Then we pull from player settings provider, 
    if there's an override thing present, that tells us to use the override settings
*/
