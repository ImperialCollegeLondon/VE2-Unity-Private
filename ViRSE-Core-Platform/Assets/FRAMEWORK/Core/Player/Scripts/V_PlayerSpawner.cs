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

    //TODO, consolidate all this into one config class?

    [ExecuteInEditMode]
    public class V_PlayerSpawner : MonoBehaviour, IPlayerSpawner //Should this be called "PlayerIntegration"?
    {
        #region domain-reload-tolerant data
        [SerializeField] public bool enableVR;
        [SerializeField] public bool enable2D;
        [SerializeField, IgnoreParent] public PlayerStateConfig playerStateConfig = new();

        [SerializeField, HideInInspector] private bool startingPositionSet = false;
        [SerializeField, HideInInspector] private Vector3 playerStartPosition;
        [SerializeField, HideInInspector] private Quaternion playerStartRotation;
        #endregion

        private const string LOCAL_PLAYER_RIG_PREFAB_PATH = "LocalPlayerRig";
        private GameObject _localPlayerRig;
        private Player _player;

        #region Player Spawner Interfaces
        public bool IsEnabled {get; private set;} = false;
        public string GameObjectName => gameObject.name;
        public event Action OnEnabledStateChanged;
        #endregion

        void OnEnable() 
        {
            if (!Application.isPlaying)
            {
                ViRSECoreServiceLocator.Instance.PlayerSpawner = this;
                return;
            }

            IsEnabled = true;
            OnEnabledStateChanged?.Invoke();

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
            _player.Initialize(playerStateConfig, ViRSECoreServiceLocator.Instance.PlayerSettingsProvider, ViRSECoreServiceLocator.Instance.PlayerAppearanceOverridesProvider);

            _player.RootPosition = playerStartPosition;
            _player.RootRotation = playerStartRotation;
            //TODO, also need to wire in some VR dependency, so that the sync module can track the VR position, head, hands, etc
        }

        private void OnDisable() 
        {
            if (!Application.isPlaying)
                return;

            IsEnabled = false;
            OnEnabledStateChanged?.Invoke();

            if (_localPlayerRig != null) 
            {
                playerStartPosition = _player.RootPosition;
                playerStartRotation = _player.RootRotation;
                
                DestroyImmediate(_localPlayerRig);
            }
        }
    }
}
