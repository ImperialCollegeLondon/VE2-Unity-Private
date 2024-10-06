using System;
using System.Collections.Generic;
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

    [Serializable]
    public class PlayerConfig
    {
        #region Utlity
        public bool PlayerSettingsProviderPresent => PlayerSettingsProvider != null;
        public IPlayerSettingsProvider PlayerSettingsProvider => ViRSECoreServiceLocator.Instance.PlayerSettingsProvider;
        [SerializeField, HideInInspector] public bool SettingsProviderPresent => PlayerSettingsProvider != null;
        #endregion


        [SerializeField] public bool enableVR;
        [SerializeField] public bool enable2D;
        [SpaceArea(spaceAfter: 5)]


        [DynamicHelp("_settingsMessage")] //TODO below button should be removed when the UI is in 
        [EditorButton(nameof(UpdatePlayerSettings), "Update player settings", activityType: ButtonActivityType.OnPlayMode, ApplyCondition = true)]
        [SerializeField, IgnoreParent, HideIf(nameof(PlayerSettingsProviderPresent), true)] public UserSettings playerSettings;

        [HideInInspector] public event Action OnLocalChangeToPlayerSettings;
        public void UpdatePlayerSettings() => OnLocalChangeToPlayerSettings?.Invoke();


        [Space(5)]
        [Title("Avatar Presentation Overrides" /*, ApplyCondition = true */)]
        [EditorButton(nameof(UpdateAvatarOverrides), "Update overrides", activityType: ButtonActivityType.OnPlayMode, ApplyCondition = true)]
        [SerializeField, IgnoreParent] public PlayerPresentationOverrides PresentationOverrides = new();

        [HideInInspector] public event Action OnLocalChangeToAvatarOverrides;
        public void UpdateAvatarOverrides() => OnLocalChangeToAvatarOverrides?.Invoke();
    }

    [Serializable]
    public class UserSettings 
    {
        [Space(5)]
        [Title("2D Control Settings")]
        [SerializeField, IgnoreParent] public Player2DControlConfig Player2DControlConfig;

        [Space(5)]
        [Title("VR Control Settings")]
        [SerializeField, IgnoreParent] public PlayerVRControlConfig PlayerVRControlConfig;

        [Space(5)]
        [Title("Avatar Presentation Settings")]
        [SerializeField, IgnoreParent] public PlayerPresentationConfig PresentationConfig = new();
    }

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

    public class V_PlayerSpawner : MonoBehaviour //Should this be called "PlayerIntegration"?
    {
        [SerializeField, IgnoreParent] public PlayerConfig SpawnConfig;
        [SerializeField, IgnoreParent] public PlayerStateConfig playerStateConfig;

        private const string LOCAL_PLAYER_RIG_PREFAB_PATH = "LocalPlayerRig";

# region proxies to nested class
        private void UpdatePlayerSettings() => SpawnConfig.UpdatePlayerSettings();
        private void UpdateAvatarOverrides() => SpawnConfig.UpdateAvatarOverrides();
        private string _settingsMessage => SpawnConfig.PlayerSettingsProviderPresent ?
    $"Debug control and appearance settings can modified on the {SpawnConfig.PlayerSettingsProvider.GameObjectName} gameobject" :
    "Create a V_PlayerPrefsInterface to save/load control and appearance settings to player prefs, or a V_PlatformIntegration to ";
    #endregion

        public 

        void Awake()
        {
            GameObject localPlayerRigGO = GameObject.Find(LOCAL_PLAYER_RIG_PREFAB_PATH);
            if (localPlayerRigGO == null)
            {
                GameObject localPlayerRigPrefab = Resources.Load("LocalPlayerRig") as GameObject;
                GameObject localPlayerRig = Instantiate(localPlayerRigPrefab, transform.position, transform.rotation);
                localPlayerRig.GetComponent<Player>().Initialize(SpawnConfig, playerStateConfig);
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