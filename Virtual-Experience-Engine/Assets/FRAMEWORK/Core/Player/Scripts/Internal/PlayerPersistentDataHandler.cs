using System;
using UnityEngine;
using static VE2.Core.Player.API.PlayerSerializables;

namespace VE2.Core.Player.Internal
{
    internal interface IPlayerPersistentDataHandler
    {
        public bool RememberPlayerSettings { get; set; }

        public PlayerPresentationConfig PlayerPresentationConfig { get; set; }

        public event Action<PlayerPresentationConfig> OnDebugSaveAppearance;

        /// <summary>
        /// Will save to playerprefs if RememberPlayerSettings is true
        /// </summary>
        public void MarkAppearanceChanged();

        public void SetDefaults(PlayerPresentationConfig defaultPlayerPresentationConfig);

        public AndroidJavaObject AddArgsToIntent(AndroidJavaObject intent);
    }

    /// <summary>
    /// Write to DefaultPlayerPresentationConfig after creating
    /// </summary>
    [ExecuteAlways]
    internal class PlayerPersistentDataHandler : MonoBehaviour, IPlayerPersistentDataHandler //TODO: Add control settings! 
    {
        private const string HasArgsArgName = "hasArgs";
        public static string RememberPlayerSettingsArgName => "rememberPlayerSettingsArg";
        public static string PlayerPresentationConfigArgName => "playerPresentationConfigArg";

        private bool _isPlaying => Application.isPlaying;

        [SpaceArea(10)]
        [SerializeField, IgnoreParent, DisableIf(nameof(_isPlaying), false), BeginGroup("Current Player Presentation")] private bool _rememberPlayerSettings = false;
        public bool RememberPlayerSettings
        {
            get => _rememberPlayerSettings;
            set
            {
                _rememberPlayerSettings = value;

                if (_rememberPlayerSettings == false)
                    PlayerPrefs.DeleteKey(PlayerPresentationConfigArgName);

                PlayerPrefs.SetInt(RememberPlayerSettingsArgName, value ? 1 : 0);
            }
        }

        [EditorButton("MarkAppearanceChanged", nameof(MarkAppearanceChanged), ApplyCondition = false)] //TODO - just for debug, remove once proper customisation UI is working
        [SerializeField, Disable] private bool _playerPresentationSetup = false;
        [SerializeField, Disable] private PlayerPresentationConfig _defaultPlayerPresentationConfig;
        [SerializeField, DisableIf(nameof(_isPlaying), false), EndGroup] private PlayerPresentationConfig _playerPresentationConfig = new();

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

                            Debug.Log("PlayerPersistentDataHandler getting presentation config - hasArgs: " + hasArgs);
                            if (hasArgs)
                            {
                                string playerPresentationConfigBytesAsString = intent.Call<string>("getStringExtra", PlayerPresentationConfigArgName);
                                byte[] playerPresentationConfigBytes = System.Convert.FromBase64String(playerPresentationConfigBytesAsString);
                                _playerPresentationConfig = new(playerPresentationConfigBytes);
                                Debug.Log("set new color to " + _playerPresentationConfig.AvatarRed + "-" + _playerPresentationConfig.AvatarGreen + "-" + _playerPresentationConfig.AvatarBlue);
                            }
                            else if (_rememberPlayerSettings)
                                _playerPresentationConfig = GetPlayerPresentationFromPlayerPrefs();
                            else
                                _playerPresentationConfig = _defaultPlayerPresentationConfig; 
                        }
                    }
                    else
                    {
                        if (_rememberPlayerSettings)
                            _playerPresentationConfig = GetPlayerPresentationFromPlayerPrefs();
                        else
                            _playerPresentationConfig = _defaultPlayerPresentationConfig; 
                    }

                    _playerPresentationSetup = true;
                }

                return _playerPresentationConfig;
            }
            set
            {
                _playerPresentationSetup = true;
                _playerPresentationConfig = value;
                MarkAppearanceChanged();
            }
        }

        private PlayerPresentationConfig GetPlayerPresentationFromPlayerPrefs()
        {
            string playerPresentationConfigBytesAsString = PlayerPrefs.GetString(PlayerPresentationConfigArgName);
            byte[] playerPresentationConfigBytes = System.Convert.FromBase64String(playerPresentationConfigBytesAsString);
            return new(playerPresentationConfigBytes);
        }

        public event Action<PlayerPresentationConfig> OnDebugSaveAppearance;

        public void MarkAppearanceChanged()
        {
            OnDebugSaveAppearance?.Invoke(_playerPresentationConfig); //TODO remove

            PlayerPrefs.SetInt(RememberPlayerSettingsArgName, _rememberPlayerSettings ? 1 : 0);
            
            if (_rememberPlayerSettings) //TODO: On android, this will save in plugins playerprefs, not in the ve2.apk playerprefs.. don't do it if we're android, and not the hub? But how best to tell if in hub?
            {
                PlayerPrefs.SetString(PlayerPresentationConfigArgName, Convert.ToBase64String(_playerPresentationConfig.Bytes));
            }
        }

        public void SetDefaults(PlayerPresentationConfig defaultPlayerPresentationConfig)
        {
            _defaultPlayerPresentationConfig = defaultPlayerPresentationConfig;
        }

        public AndroidJavaObject AddArgsToIntent(AndroidJavaObject intent)
        {
            intent.Call<AndroidJavaObject>("putExtra", HasArgsArgName, true);
            intent.Call<AndroidJavaObject>("putExtra", PlayerPresentationConfigArgName, Convert.ToBase64String(_playerPresentationConfig.Bytes));
            return intent;
        }

        private void Awake()
        {
            if (FindObjectsByType<PlayerPersistentDataHandler>(FindObjectsSortMode.None).Length > 1)
            {
                Debug.LogError("There should only be one PlayerSettingsHandler in the scene, but a new one was created. Deleting the new one.");
                Destroy(gameObject);
                return;
            }

            ResetData();
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
            DontDestroyOnLoad(gameObject);
        }

        private void OnDisable()
        {
            ResetData();
        }

        private void ResetData() 
        {
            _playerPresentationSetup = false;
        }
    }
}
