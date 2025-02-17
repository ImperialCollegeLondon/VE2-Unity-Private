using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using VE2.Common;
using static VE2.Common.CommonSerializables;

//TODO: Think about how these args will work for args passed in from the very start 
//E.G the launcher passing customerID and customerKey... 
//If android, look using the SettingsHandler, if desktop, read cmd args explicitly

/*
    This should be internal to Player - if a different service needs access to these settings, it should go via the player API
    The reason we have this whole "Settingshandler" thing is to try and share inspector things between different services 
    Hold on, what about settings that don't really fit to a service?
    e.g - the starting instance code
    That could be in some kind "ServerRegistrationSettingsHandler"? is that not then inconsistent?
    it _should_ come from the platform, but instancing doesn't might still need this data even if PlatformService isn't preseant (i.e, load just a plugin)
    Maybe platform and instancing should just give come in the same package?
    If you want instancing off-platform, then just make sure arguments come into the system... wont platform still try to connect to platform server 
*/

//TODO: NEW - do we even want to expose this in the API assembly at all? PluginLoader needs it I suppose...
//Basically, the API needs to expose read access for these settings 
//We want a IPlayerSettingsReadable, and a IPlayerSettingsReadWritable? Pass ReadWritable to the UI, Readable to platform?


//If we create this from PlayerController, where do we create the CustomerLoginSettingsHandler?
//In the classes that need them, just don't make a second one
[ExecuteAlways]
internal class PlayerSettingsHandler : MonoBehaviour, IPlayerSettingsHandler //TODO: Add control settings! Unless those should live somewhere else?
{
    private const string HasArgsArgName = "hasArgs";
    public static string RememberPlayerSettingsArgName => "rememberPlayerSettings";
    public static string PlayerNameArgName => "playerName";
    public static string PlayerHeadTypeArgName => "playerHeadType";
    public static string PlayerTorsoTypeArgName => "playerTorsoType";
    public static string PlayerRedArgName => "playerRed";
    public static string PlayerGreenArgName => "playerGreen";
    public static string PlayerBlueArgName => "playerBlue";

    public string GameObjectName => gameObject.name;

    //TODO: Also need RememberMeDefault and RememberMeCurrent

    public PlayerPresentationConfig DefaultPlayerPresentationConfig;

    private bool _isPlaying => Application.isPlaying;
    private bool _playerPresentationSetup = false;
    [SpaceArea(10)]
    [SerializeField, IgnoreParent, DisableIf(nameof(_isPlaying), false), BeginGroup("Current Player Presentation")] private bool _rememberPlayerSettings = false;
    public bool RememberPlayerSettings
    {
        get => _rememberPlayerSettings;
        set
        {
            _rememberPlayerSettings = value;

            if (_rememberPlayerSettings == false)
            {
                PlayerPrefs.DeleteKey(PlayerNameArgName);
                PlayerPrefs.DeleteKey(PlayerHeadTypeArgName);
                PlayerPrefs.DeleteKey(PlayerTorsoTypeArgName);
                PlayerPrefs.DeleteKey(PlayerRedArgName);
                PlayerPrefs.DeleteKey(PlayerGreenArgName);
                PlayerPrefs.DeleteKey(PlayerBlueArgName);
            }

            PlayerPrefs.SetInt(RememberPlayerSettingsArgName, value ? 1 : 0);
        }
    }

    [EditorButton("MarkPlayerSettingsUpdated", nameof(SavePlayerAppearance), ApplyCondition = true)]
    [SerializeField, IgnoreParent, DisableIf(nameof(_isPlaying), false), EndGroup] private PlayerPresentationConfig _playerPresentationConfig = new();

    /// <summary>
    /// call MarkPlayerSettingsUpdated after modifying this property
    /// </summary>
    public PlayerPresentationConfig PlayerPresentationConfig 
    {
        get
        {
            if (!_playerPresentationSetup)
            {
                if (Application.platform == RuntimePlatform.Android)
                {
                    using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                    using (AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                    using (AndroidJavaObject intent = currentActivity.Call<AndroidJavaObject>("getIntent"))
                    {
                        bool hasArgs = intent == null ? false : intent.Call<bool>("getBooleanExtra", HasArgsArgName, false);

                        if (hasArgs)
                            _playerPresentationConfig = new PlayerPresentationConfig(
                                playerName: intent.Call<string>("getStringExtra", PlayerNameArgName),
                                avatarHeadType: (ViRSEAvatarHeadAppearanceType)intent.Call<int>("getIntExtra", PlayerHeadTypeArgName),
                                avatarBodyType: (ViRSEAvatarTorsoAppearanceType)intent.Call<int>("getIntExtra", PlayerTorsoTypeArgName),
                                avatarRed: (ushort)intent.Call<int>("getIntExtra", PlayerRedArgName, 0),
                                avatarGreen: (ushort)intent.Call<int>("getIntExtra", PlayerGreenArgName, 0),
                                avatarBlue: (ushort)intent.Call<int>("getIntExtra", PlayerBlueArgName, 0));
                        else if (_rememberPlayerSettings)
                            _playerPresentationConfig = GetPlayerPresentationFromPlayerPrefs();
                        else
                            _playerPresentationConfig = DefaultPlayerPresentationConfig; 
                    }

                    _playerPresentationSetup = true;
                }
                else
                {
                    if (PlayerArgsDesktopBus.Instance.HasArgs)
                        _playerPresentationConfig = PlayerArgsDesktopBus.Instance.PlayerPresentationConfig;
                    else if (_rememberPlayerSettings)
                        _playerPresentationConfig = GetPlayerPresentationFromPlayerPrefs();
                    else
                        _playerPresentationConfig = new PlayerPresentationConfig(DefaultPlayerPresentationConfig); //Don't want to copy the reference, just the values
                }
            }
            else
                Debug.Log("Player settings setup!");

            return _playerPresentationConfig;
        }
        set
        {
            _playerPresentationConfig = value;
            //MarkPlayerSettingsUpdated();
        }
    }

    private PlayerPresentationConfig GetPlayerPresentationFromPlayerPrefs()
    {
        return
            _playerPresentationConfig = new PlayerPresentationConfig(
                playerName: PlayerPrefs.GetString(PlayerNameArgName, DefaultPlayerPresentationConfig.PlayerName),
                avatarHeadType: (ViRSEAvatarHeadAppearanceType)PlayerPrefs.GetInt(PlayerHeadTypeArgName, (int)DefaultPlayerPresentationConfig.AvatarHeadType),
                avatarBodyType: (ViRSEAvatarTorsoAppearanceType)PlayerPrefs.GetInt(PlayerTorsoTypeArgName, (int)DefaultPlayerPresentationConfig.AvatarTorsoType),
                avatarRed: (ushort)PlayerPrefs.GetInt(PlayerRedArgName, DefaultPlayerPresentationConfig.AvatarRed),
                avatarGreen: (ushort)PlayerPrefs.GetInt(PlayerGreenArgName, DefaultPlayerPresentationConfig.AvatarGreen),
                avatarBlue: (ushort)PlayerPrefs.GetInt(PlayerBlueArgName, DefaultPlayerPresentationConfig.AvatarBlue));
    }

    //public event Action<PlayerPresentationConfig> OnPlayerPresentationConfigChanged;

    public void SavePlayerAppearance()
    {
        //OnPlayerPresentationConfigChanged?.Invoke(_playerPresentationConfig);

        PlayerPrefs.SetInt(RememberPlayerSettingsArgName, _rememberPlayerSettings ? 1 : 0);
        
        if (_rememberPlayerSettings) //TODO: On android, this will save in plugins playerprefs, not in the ve2.apk playerprefs.. don't do it if we're android, and not the hub? But how best to tell if in hub?
        {
            PlayerPrefs.SetString(PlayerNameArgName, _playerPresentationConfig.PlayerName);
            PlayerPrefs.SetInt(PlayerHeadTypeArgName, (int)_playerPresentationConfig.AvatarHeadType);
            PlayerPrefs.SetInt(PlayerTorsoTypeArgName, (int)_playerPresentationConfig.AvatarTorsoType);
            PlayerPrefs.SetInt(PlayerRedArgName, _playerPresentationConfig.AvatarRed);
            PlayerPrefs.SetInt(PlayerGreenArgName, _playerPresentationConfig.AvatarGreen);
            PlayerPrefs.SetInt(PlayerBlueArgName, _playerPresentationConfig.AvatarBlue);
        }
    }

    private void Awake()
    {
        _playerPresentationSetup = false;
        _rememberPlayerSettings = PlayerPrefs.GetInt(RememberPlayerSettingsArgName) == 1 ? true: false;

        if (Application.isPlaying)
        {
            _rememberPlayerSettings = PlayerPrefs.GetInt(RememberPlayerSettingsArgName, 0) == 1;
        }
        else
        {
            _playerPresentationConfig = new PlayerPresentationConfig();
        }

        //gameObject.hideFlags = HideFlags.HideInHierarchy; //To hide
        gameObject.hideFlags &= ~HideFlags.HideInHierarchy; //To show
    }

    private void OnDisable()
    {
        _playerPresentationSetup = false;
    }
}


public class ServerRegArgDefaults : MonoBehaviour
{
    private static ServerRegArgDefaults _instance;
    public static ServerRegArgDefaults Instance
    { //Reload-proof singleton
        get
        {
            if (_instance == null)
                _instance = FindFirstObjectByType<ServerRegArgDefaults>();

            if (_instance == null && !Application.isPlaying)
                _instance = new GameObject($"CustomerLoginArgDefaults{SceneManager.GetActiveScene().name}").AddComponent<ServerRegArgDefaults>();

            return _instance;
        }
    }

    [SerializeField, IgnoreParent] private string _customerID;
    public string CustomerID => _customerID;

    [SerializeField, IgnoreParent] private string _customerKey;
    public string CustomerKey => _customerKey;

    [SerializeField, IgnoreParent] private string _instanceCode;
    public string InstanceCode => _instanceCode;
}

public class PlatformNetworkArgDefaults : MonoBehaviour
{
    private static PlatformNetworkArgDefaults _instance;
    public static PlatformNetworkArgDefaults Instance
    { //Reload-proof singleton
        get
        {
            if (_instance == null)
                _instance = FindFirstObjectByType<PlatformNetworkArgDefaults>();

            if (_instance == null && !Application.isPlaying)
                _instance = new GameObject($"PlatformNetworkArgDefaults{SceneManager.GetActiveScene().name}").AddComponent<PlatformNetworkArgDefaults>();

            return _instance;
        }
    }

    [SerializeField, IgnoreParent] private string ipAddress;
    public string IPAddress => ipAddress;

    [SerializeField, IgnoreParent] private string _portNumber;
    public string PortNumber => _portNumber;
}

public class InstanceNetworkArgDefaults : MonoBehaviour
{
    private static InstanceNetworkArgDefaults _instance;
    public static InstanceNetworkArgDefaults Instance
    { //Reload-proof singleton
        get
        {
            if (_instance == null)
                _instance = FindFirstObjectByType<InstanceNetworkArgDefaults>();

            if (_instance == null && !Application.isPlaying)
                _instance = new GameObject($"InstanceNetworkArgDefaults{SceneManager.GetActiveScene().name}").AddComponent<InstanceNetworkArgDefaults>();

            return _instance;
        }
    }

    [SerializeField, IgnoreParent] private string ipAddress;
    public string IPAddress => ipAddress;

    [SerializeField, IgnoreParent] private string _portNumber;
    public string PortNumber => _portNumber;
}

public class FTPNetworkArgDefaults : MonoBehaviour
{
    private static InstanceNetworkArgDefaults _instance;
    public static InstanceNetworkArgDefaults Instance
    { //Reload-proof singleton
        get
        {
            if (_instance == null)
                _instance = FindFirstObjectByType<InstanceNetworkArgDefaults>();

            if (_instance == null && !Application.isPlaying)
                _instance = new GameObject($"InstanceNetworkArgDefaults{SceneManager.GetActiveScene().name}").AddComponent<InstanceNetworkArgDefaults>();

            return _instance;
        }
    }

    [SerializeField, IgnoreParent] private string ipAddress;
    public string IPAddress => ipAddress;

    [SerializeField, IgnoreParent] private string _portNumber;
    public string PortNumber => _portNumber;

    [SerializeField, IgnoreParent] private string _username;
    public string Username => _username;

    [SerializeField, IgnoreParent] private string _password;
    public string Password => _password;
}