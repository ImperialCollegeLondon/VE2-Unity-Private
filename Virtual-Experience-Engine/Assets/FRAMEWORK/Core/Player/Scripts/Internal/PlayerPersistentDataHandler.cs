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
    }

    /// <summary>
    /// Write to DefaultPlayerPresentationConfig after creating
    /// </summary>
    [ExecuteAlways]
    internal class PlayerPersistentDataHandler : MonoBehaviour, IPlayerPersistentDataHandler //TODO: Add control settings! 
    {
        private const string HasArgsArgName = "hasArgs";
        public static string RememberPlayerSettingsArgName => "rememberPlayerSettings";
        public static string PlayerNameArgName => "playerName";
        public static string PlayerHeadTypeArgName => "playerHeadType";
        public static string PlayerTorsoTypeArgName => "playerTorsoType";
        public static string PlayerRedArgName => "playerRed";
        public static string PlayerGreenArgName => "playerGreen";
        public static string PlayerBlueArgName => "playerBlue";

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

                            if (hasArgs)
                                _playerPresentationConfig = new PlayerPresentationConfig(
                                    playerName: intent.Call<string>("getStringExtra", PlayerNameArgName),
                                    avatarHeadType: (VE2AvatarHeadAppearanceType)intent.Call<int>("getIntExtra", PlayerHeadTypeArgName),
                                    avatarBodyType: (VE2AvatarTorsoAppearanceType)intent.Call<int>("getIntExtra", PlayerTorsoTypeArgName),
                                    avatarRed: (ushort)intent.Call<int>("getIntExtra", PlayerRedArgName, 0),
                                    avatarGreen: (ushort)intent.Call<int>("getIntExtra", PlayerGreenArgName, 0),
                                    avatarBlue: (ushort)intent.Call<int>("getIntExtra", PlayerBlueArgName, 0));
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
            return
                _playerPresentationConfig = new PlayerPresentationConfig(
                    playerName: PlayerPrefs.GetString(PlayerNameArgName, _defaultPlayerPresentationConfig.PlayerName),
                    avatarHeadType: (VE2AvatarHeadAppearanceType)PlayerPrefs.GetInt(PlayerHeadTypeArgName, (int)_defaultPlayerPresentationConfig.AvatarHeadType),
                    avatarBodyType: (VE2AvatarTorsoAppearanceType)PlayerPrefs.GetInt(PlayerTorsoTypeArgName, (int)_defaultPlayerPresentationConfig.AvatarTorsoType),
                    avatarRed: (ushort)PlayerPrefs.GetInt(PlayerRedArgName, _defaultPlayerPresentationConfig.AvatarRed),
                    avatarGreen: (ushort)PlayerPrefs.GetInt(PlayerGreenArgName, _defaultPlayerPresentationConfig.AvatarGreen),
                    avatarBlue: (ushort)PlayerPrefs.GetInt(PlayerBlueArgName, _defaultPlayerPresentationConfig.AvatarBlue));
        }

        public event Action<PlayerPresentationConfig> OnDebugSaveAppearance;

        public void MarkAppearanceChanged()
        {
            OnDebugSaveAppearance?.Invoke(_playerPresentationConfig); //TODO remove

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

        public void SetDefaults(PlayerPresentationConfig defaultPlayerPresentationConfig)
        {
            _defaultPlayerPresentationConfig = defaultPlayerPresentationConfig;
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
