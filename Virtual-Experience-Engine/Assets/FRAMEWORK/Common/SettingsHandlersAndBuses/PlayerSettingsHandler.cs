using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using static VE2.Common.CommonSerializables;

//TODO: Think about how these args will work for args passed in from the very start 
//E.G the launcher passing customerID and customerKey... 
//If android, look using the SettingsHandler, if desktop, read cmd args explicitly

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

    [SerializeField, IgnoreParent, DisableInPlayMode, BeginGroup("Default Player Presentation"), EndGroup, InLineEditor] private PlayerPresentationConfig _defaultPlayerPresentationConfig = new();
    public PlayerPresentationConfig DefaultPlayerPresentationConfig {
        get {
            return _defaultPlayerPresentationConfig;
        }
    }

    private bool _isPlaying => Application.isPlaying;
    private bool _playerPresentationSetup = false;
    [EditorButton("MarkPlayerSettingsUpdated", nameof(MarkPlayerSettingsUpdated), ApplyCondition = true)]
    [SpaceArea(10)]
    [SerializeField, IgnoreParent, DisableIf(nameof(_isPlaying), false), BeginGroup("Current Player Presentation"), EndGroup] private PlayerPresentationConfig _playerPresentationConfig = new();

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
                        else
                            _playerPresentationConfig = _defaultPlayerPresentationConfig;
                    }

                    _playerPresentationSetup = true;
                }
                else
                {
                    if (PlayerArgsDesktopBus.Instance.HasArgs)
                        _playerPresentationConfig = PlayerArgsDesktopBus.Instance.PlayerPresentationConfig;
                    else
                        _playerPresentationConfig = new PlayerPresentationConfig(_defaultPlayerPresentationConfig); //Don't want to copy the reference, just the values
                }
            }
            else
                Debug.Log("Player settings setup!");

            return _playerPresentationConfig;
        }
        set
        {
            _playerPresentationConfig = value;
            MarkPlayerSettingsUpdated();
        }
    }

    public event Action<PlayerPresentationConfig> OnPlayerPresentationConfigChanged;

    public void MarkPlayerSettingsUpdated()
    {
        Debug.Log("NMark player settings updated");
        OnPlayerPresentationConfigChanged?.Invoke(_playerPresentationConfig);
        
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

    private static bool _rememberPlayerSettings = false; //Assigned on instance creation
    public static bool RememberPlayerSettings
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

    private void Awake()
    {
        _playerPresentationSetup = false;

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