using System;
using System.Collections.Generic;
using UnityEngine;
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
    }

    [Serializable]
    public class PlayerAvatarConfig
    {
        [SerializeField] public bool LoadAvatarConfigFromPlayerPrefs = true;

        [SerializeField] public List<GameObject> avatarHeads;
        [SerializeField] public List<GameObject> avatarBodies;
    }

    //TODO, consolidate all this into one config class?

    public class V_PlayerSpawner : MonoBehaviour
    {
        [SerializeField, IgnoreParent] public PlayerSpawnConfig SpawnConfig;
        [BeginGroup(Style = GroupStyle.Round), EndGroup]
        [Space(5)]
        [Title("Avatar Presentation Settings" /*, ApplyCondition = true */)]
        [SerializeField, IgnoreParent] public PlayerPresentationConfig PresentationConfig;

        [SerializeField, IgnoreParent] public PlayerStateConfig playerStateConfig;

        private const string LOCAL_PLAYER_RIG_PREFAB_PATH = "LocalPlayerRig";   

        void Awake()
        {
            GameObject localPlayerRigGO = GameObject.Find(LOCAL_PLAYER_RIG_PREFAB_PATH);
            if (localPlayerRigGO == null)
            {
                GameObject localPlayerRigPrefab = Resources.Load("LocalPlayerRig") as GameObject;
                GameObject localPlayerRig = Instantiate(localPlayerRigPrefab, transform.position, transform.rotation);
                localPlayerRig.GetComponent<Player>().Initialize(SpawnConfig, PresentationConfig, playerStateConfig);

                //TODO, also need to wire in some VR dependency, so that the sync module can track the VR position, head, hands, etc
            }
        }
    }
}
