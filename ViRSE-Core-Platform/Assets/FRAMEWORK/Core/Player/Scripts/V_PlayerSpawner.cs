using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.PlayerLoop;
using 
ViRSE.Core.Shared;
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
        #region Utlity
        public bool PlayerSettingsProviderPresent => PlayerSettingsProvider != null;
        public IPlayerSettingsProvider PlayerSettingsProvider => ViRSECoreServiceLocator.Instance.PlayerSettingsProvider;
        [SerializeField, HideInInspector] public bool SettingsProviderPresent => PlayerSettingsProvider != null;
        #endregion

        [SerializeField] public bool enableVR;
        [SerializeField] public bool enable2D;

        [SerializeField, HideIf(nameof(PlayerSettingsProvider), true)] public bool exchangeSettingsWithPlayerPrefs = true;

        private UserSettingsPersistable _userSettings = null;
        public UserSettingsPersistable UserSettings
        {
            get
            {
                if (_userSettings == null)
                {
                    if (PlayerSettingsProviderPresent)
                        _userSettings = PlayerSettingsProvider.UserSettings;
                    else if (exchangeSettingsWithPlayerPrefs)
                        _userSettings = new(); //TODO - load from player prefs
                    else
                        _userSettings = new();
                }
                return _userSettings;
            }
            set
            {
                if (PlayerSettingsProviderPresent)
                    Debug.LogError($"Error, can't override user settings, user settings come from {PlayerSettingsProvider.GameObjectName}");
                else if (exchangeSettingsWithPlayerPrefs)
                    _userSettings = value; //TODO - save to player prefs
                else
                    _userSettings = value;
            }
        }

        public event Action OnLocalChangeToUserSettings;

        [Space(5)]
        [Title("Avatar Presentation Overrides" /*, ApplyCondition = true */)]
        [EditorButton(nameof(UpdateAvatarOverrides), "Update overrides", activityType: ButtonActivityType.OnPlayMode, ApplyCondition = true)]
        [SerializeField, IgnoreParent] public PlayerPresentationOverrides PresentationOverrides = new();
        private void UpdateAvatarOverrides() => OnLocalChangeToPresentationOverrides?.Invoke();
        public event Action OnLocalChangeToPresentationOverrides;

        [SerializeField, IgnoreParent] public PlayerStateConfig playerStateConfig;

        private const string LOCAL_PLAYER_RIG_PREFAB_PATH = "LocalPlayerRig";

        #region Interfaces 
        public PlayerPresentationOverrides PlayerPresentationOverrides { get => PresentationOverrides; }
        public bool IsEnabled => enabled;
        public string GameObjectName => gameObject.name;
        #endregion

        //TODO - probably need some api to set the user settings so the customer can override them when off-platform

        /*
            Instance sync needs the avatar appearance 
            If we're not on platform, that comes from the player directly 
            If we ARE on platform, that comes from the platform integration... which takes time 
            Ok, so, if we're on platform, delay boot until platform is ready

        */

        void OnEnable() //TODO, should be awake, but then doesn't get called when ExecuteInEditMode
        {
            /*
                 The player rig needs its user settings 
                 So, these either come from the 
            */
            Debug.Log("1");
            if (!Application.isPlaying)
            {
                ViRSECoreServiceLocator.Instance.PlayerAppearanceOverridesProvider = this;
                return;
            }
            Debug.Log("2");
            GameObject localPlayerRigGO = GameObject.Find(LOCAL_PLAYER_RIG_PREFAB_PATH);
            if (localPlayerRigGO == null)
            {
                GameObject localPlayerRigPrefab = Resources.Load("LocalPlayerRig") as GameObject;
                GameObject localPlayerRig = Instantiate(localPlayerRigPrefab, transform.position, transform.rotation);

                localPlayerRig.GetComponent<Player>().Initialize(playerStateConfig, UserSettings);
                //TODO, also need to wire in some VR dependency, so that the sync module can track the VR position, head, hands, etc
            }
        }
    }
}

/*
When a platform integration is present, these settings should appear in the platform integration inspector instead 
We'll need some button, that button should emit some "player settings changed" event, both the platform and the instance will need to be informed of this 
When we register with the platform, we send certain settings, same deal when we register to the instance, so we'll want to send off that same appearance message again 
So the player itself will need to have some event, the platform service, and the instance service will both need to listen to this, and send the update to their respective servers 


*/



/*
We want the ClientID to persist between server connections, 
The server needs to be the one to create IDs though, is the only thing... 
Well, the server will have to deal with DarkRiftIDs, the client should never see these, only ClientIDs 
This ID wants to be a short... but not sured how we then distinguish guest and non guest?
We don't need to, it can be a short either way. 
The server doesn't give you an ID until AFTER the 

*/