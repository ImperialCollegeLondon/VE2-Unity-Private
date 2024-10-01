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

    [Serializable]
    public class PlayerSpawnConfig
    {
        [SerializeField] public bool enableVR;
        [SerializeField] public bool enable2D;
        [SerializeField] public bool LoadControlConfigFromPlayerPrefs = true; //TODO, this needs to be disabled if on platform.... but how?

        [SerializeField, HideInInspector] public bool SettingsProviderPresent => SettingsProvider != null;
        public IPlayerSettingsProvider SettingsProvider => ViRSECoreServiceLocator.Instance.PlayerSettingsProvider;

        [BeginGroup(Style = GroupStyle.Round), EndGroup]
        [Space(5)]
        [Title("2D Control Settings" /*, ApplyCondition = true */)]
        [SerializeField, IgnoreParent] public Player2DControlConfig Player2DControlConfig;


        [BeginGroup(Style = GroupStyle.Round), EndGroup]
        [Space(5)]
        [Title("VR Control Settings" /*, ApplyCondition = true */)]
        [SerializeField, IgnoreParent] public PlayerVRControlConfig PlayerVRControlConfig;

        [BeginGroup(Style = GroupStyle.Round), EndGroup]
        [Space(5)]
        [Title("Avatar Presentation Settings" /*, ApplyCondition = true */)]
        [SerializeField, IgnoreParent] public PlayerPresentationConfigWrapper PlayerPresentationConfigWrapper;
    }

    [Serializable]
    public class PlayerPresentationConfigWrapper
    {
        [EditorButton(nameof(UpdateAvatar), "Update avatar", activityType: ButtonActivityType.OnPlayMode)]
        [SerializeField, IgnoreParent] public PlayerPresentationConfig PresentationConfig = new(); //TODO - Hide when a provider is present 
        [SerializeField, IgnoreParent] public PlayerPresentationOverrides PresentationOverrides = new();

        [HideInInspector] public event Action OnLocalChangeToPlayerPresentation;

        public void UpdateAvatar() => OnLocalChangeToPlayerPresentation?.Invoke();
    }

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
        [SerializeField, IgnoreParent] public PlayerSpawnConfig SpawnConfig;
        [SerializeField, IgnoreParent] public PlayerStateConfig playerStateConfig;

        private const string LOCAL_PLAYER_RIG_PREFAB_PATH = "LocalPlayerRig";

        private void UpdateAvatar() => SpawnConfig.PlayerPresentationConfigWrapper.UpdateAvatar(); //Proxy to nested class

        void Awake()
        {
            GameObject localPlayerRigGO = GameObject.Find(LOCAL_PLAYER_RIG_PREFAB_PATH);
            if (localPlayerRigGO == null)
            {
                GameObject localPlayerRigPrefab = Resources.Load("LocalPlayerRig") as GameObject;
                GameObject localPlayerRig = Instantiate(localPlayerRigPrefab, transform.position, transform.rotation);
                localPlayerRig.GetComponent<Player>().Initialize(SpawnConfig, SpawnConfig.PlayerPresentationConfigWrapper, playerStateConfig);
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
