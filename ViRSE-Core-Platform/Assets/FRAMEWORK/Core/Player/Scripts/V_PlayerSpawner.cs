using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ViRSE.Core.Player
{

    [Serializable]
    public class PlayerSpawnConfig
    {
        [SerializeField] public bool enableVR;
        [SerializeField] public bool enable2D;
        [SerializeField] public bool LoadControlConfigFromPlayerPrefs = true;

        [SerializeField, HideLabel] public Player2DControlConfig Player2DControlConfig;
        [SerializeField, HideLabel] public PlayerVRControlConfig PlayerVRControlConfig;
    }

    [Serializable]
    public class Player2DControlConfig
    {
        //2D Control settings
        [SerializeField] public float MouseSensitivity = 1;
        [SerializeField] public bool CrouchHold = true;
        //public float IsControlPromptTicked = "isControlPromptTicked";
    }

    [Serializable]
    public class PlayerVRControlConfig
    {
        //VR Control Settings
        [SerializeField] public float DragSpeed = 1;
        [SerializeField] public float TurnAmount = 1;
        [SerializeField] public bool ControllerVibration = true;
        [SerializeField] public bool ControllerLabels = true;
        [SerializeField] public float WristLookPrecision = 1;

        //VR Comfort settings 
        [SerializeField] public bool DragDarkening = false;
        [SerializeField] public bool TeleportBlink = false;
        [SerializeField] public bool SnapTurnBlink = false;

        //public static string avatarTransparency = "avatarTransparency";
    }

    [Serializable]
    public class PlayerAvatarConfig
    {
        [SerializeField] public bool LoadAvatarConfigFromPlayerPrefs = true;

        [SerializeField] public List<GameObject> avatarHeads;
        [SerializeField] public List<GameObject> avatarBodies;
    }

    [Serializable]
    public class PlayerPresentationConfig
    {
        public string PlayerName;
        public string AvatarHeadType;
        public string AvatarBodyType;
        public Color AvatarColor;
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
