using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.PlayerLoop;
using ViRSE.Core.Shared;
using static ViRSE.Core.Shared.CoreCommonSerializables;

namespace ViRSE.Core.Player
{
    [Serializable]
    public class PlayerStateConfig : BaseStateConfig
    {
        //events for state change (2d/vr), teleport?
    }

    /*
    We need to move some of this config stuff to the actual mono 
    ah, idk, we want to be able to HAVE buttons in the nested inspectors, surely? 
    E.G a button to "invoke" an invokable
    That wouldn't need to go into the state module though...
    */

    // [Serializable]
    // public class PlayerPresentationConfigWrapper
    // {
    //     [EditorButton(nameof(UpdateAvatar), "Update avatar", activityType: ButtonActivityType.OnPlayMode)]
    //     [SerializeField, IgnoreParent] public PlayerPresentationConfig PresentationConfig = new(); //TODO - Hide when a provider is present 
    //     [SerializeField, IgnoreParent] public PlayerPresentationOverrides PresentationOverrides = new();

    //     [HideInInspector] public event Action OnLocalChangeToPlayerPresentation;

    //     public void UpdateAvatar() => OnLocalChangeToPlayerPresentation?.Invoke();
    // }

    [Serializable]
    public class PlayerAvatarConfig
    {
        [SerializeField] public bool LoadAvatarConfigFromPlayerPrefs = true;

        [SerializeField] public List<GameObject> avatarHeads;
        [SerializeField] public List<GameObject> avatarBodies;
    }

    //TODO, consolidate all this into one config class?

    [ExecuteInEditMode]
    public class V_PlayerSpawner : MonoBehaviour, IPlayerAppearanceOverridesProvider //Should this be called "PlayerIntegration"?
    {
        [SerializeField] public bool enableVR;
        [SerializeField] public bool enable2D;

        public event Action OnLocalChangeToUserSettings;

        [Space(5)]
        [Title("Avatar Presentation Overrides")]
        [EditorButton(nameof(NotifyProviderOfChangeAppearanceOverrides), "Update overrides", activityType: ButtonActivityType.OnPlayMode)]
        [SerializeField, IgnoreParent] public PlayerPresentationOverrides PresentationOverrides = new();

        [SerializeField, IgnoreParent] public PlayerStateConfig playerStateConfig = new();

        private const string LOCAL_PLAYER_RIG_PREFAB_PATH = "LocalPlayerRig";
        private GameObject _localPlayerRig;
        private Player _player;
        [SerializeField, HideInInspector] private bool startingPositionSet = false;
        [SerializeField, HideInInspector] private Vector3 playerStartPosition;
        [SerializeField] private Quaternion playerStartRotation;

        #region Appearance Overrides Interfaces 
        public PlayerPresentationOverrides PlayerPresentationOverrides { get => PresentationOverrides; }
        public void NotifyProviderOfChangeAppearanceOverrides() => OnAppearanceOverridesChanged?.Invoke();
        public event Action OnAppearanceOverridesChanged;

        public bool IsEnabled => enabled;
        public string GameObjectName => gameObject.name;
        #endregion

        //TODO - need an API for changing overrrides 

        void OnEnable() 
        {
            if (!Application.isPlaying)
            {
                ViRSECoreServiceLocator.Instance.PlayerAppearanceOverridesProvider = this;
                return;
            }

            if (!startingPositionSet)
            {
                playerStartPosition = transform.position;
                playerStartRotation = transform.rotation;
                startingPositionSet = true;
            }

            if (ViRSECoreServiceLocator.Instance.PlayerSettingsProvider == null) 
            {
                Debug.LogError("Error, V_PlayerSpawner cannot spawn player, no player settings provider found");
                return;
            }

            if (ViRSECoreServiceLocator.Instance.PlayerSettingsProvider.ArePlayerSettingsReady)
                HandlePlayerSettingsReady();
            else
                ViRSECoreServiceLocator.Instance.PlayerSettingsProvider.OnPlayerSettingsReady += HandlePlayerSettingsReady;
            
        }

        private void HandlePlayerSettingsReady()
        {
            ViRSECoreServiceLocator.Instance.PlayerSettingsProvider.OnPlayerSettingsReady -= HandlePlayerSettingsReady;

            GameObject localPlayerRigGO = GameObject.Find(LOCAL_PLAYER_RIG_PREFAB_PATH);
            GameObject localPlayerRigPrefab = Resources.Load("LocalPlayerRig") as GameObject;
            _localPlayerRig = Instantiate(localPlayerRigPrefab, transform.position, transform.rotation);

            _player = _localPlayerRig.GetComponent<Player>();
            _player.Initialize(playerStateConfig, ViRSECoreServiceLocator.Instance.PlayerSettingsProvider, this);

            _player.RootPosition = playerStartPosition;
            _player.RootRotation = playerStartRotation;
            //TODO, also need to wire in some VR dependency, so that the sync module can track the VR position, head, hands, etc
        }

        private void OnDisable() 
        {
            if (Application.isPlaying && _localPlayerRig != null) 
            {
                Debug.Log("Destyoing player");

                playerStartPosition = _player.RootPosition;
                playerStartRotation = _player.RootRotation;
                
                DestroyImmediate(_localPlayerRig);
            }
        }
    }
}

// public class UserSettingsPersistableWrapper
// {
//     public UserSettingsPersistable UserSettings { get; private set; }
//     public event Action OnUserSettingsChanged;

//     public void UpdateUserSettings(UserSettingsPersistable userSettings)
//     {
//         UserSettings = userSettings;
//     }
// }
