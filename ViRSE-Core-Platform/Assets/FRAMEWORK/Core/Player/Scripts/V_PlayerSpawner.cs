using System;
using System.Collections.Generic;
using UnityEngine;
using static ViRSE.Core.Shared.CoreCommonSerializables;

namespace ViRSE.Core.Player
{
    [Serializable]
    public class PlayerSpawnConfig
    {
        [SerializeField] public bool enableVR;
        [SerializeField] public bool enable2D;
        [SerializeField] public bool LoadControlConfigFromPlayerPrefs = true; //TODO, this needs to be disabled if on platform.... but how?

        [SerializeField, HideLabel] public Player2DControlConfig Player2DControlConfig;
        [SerializeField, HideLabel] public PlayerVRControlConfig PlayerVRControlConfig;
    }

    [Serializable]
    public class PlayerAvatarConfig
    {
        [SerializeField] public bool LoadAvatarConfigFromPlayerPrefs = true;

        [SerializeField] public List<GameObject> avatarHeads;
        [SerializeField] public List<GameObject> avatarBodies;
    }

    public class V_PlayerSpawner : MonoBehaviour
    {
        [SerializeField, HideLabel] public PlayerSpawnConfig SpawnConfig;
        [SerializeField, HideLabel] public PlayerPresentationConfig PresentationConfig;

        private const string LOCAL_PLAYER_RIG_PREFAB_PATH = "LocalPlayerRig";   

        void Awake()
        {
            GameObject localPlayerRigGO = GameObject.Find(LOCAL_PLAYER_RIG_PREFAB_PATH);
            if (localPlayerRigGO == null)
            {
                GameObject localPlayerRigPrefab = Resources.Load("LocalPlayerRig") as GameObject;
                GameObject localPlayerRig = Instantiate(localPlayerRigPrefab, transform.position, transform.rotation);
                localPlayerRig.GetComponent<Player>().Initialize(SpawnConfig, PresentationConfig);
            }
        }
    }
}
