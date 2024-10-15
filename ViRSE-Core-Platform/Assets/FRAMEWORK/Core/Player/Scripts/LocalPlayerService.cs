using System;
using UnityEngine;
using UnityEngine.UIElements;
using ViRSE.Core.Player;
using ViRSE.Core.Shared;
using static ViRSE.Core.Shared.CoreCommonSerializables;

namespace ViRSE.Core.Player
{
    //TODO, don't think this should be a mono, should just be a service, that we inject with the 2d and vr players
    public class Player : MonoBehaviour, ILocalPlayerRig
    {
        #region Plugin Runtime interfaces
        public Vector3 RootPosition { get => _activePlayer.RootPosition; set => _activePlayer.RootPosition = value; }
        public Quaternion RootRotation { get => _activePlayer.RootRotation; set => _activePlayer.RootRotation = value; }
        public Vector3 HeadPosition => Camera.main.transform.position;
        public Quaternion HeadRotation => Camera.main.transform.rotation;
        public TransmissionProtocol TransmissionProtocol => _stateConfig.RepeatedTransmissionConfig.TransmissionType;
        public float TransmissionFrequency => _stateConfig.RepeatedTransmissionConfig.TransmissionFrequency;
        #endregion

        [SerializeField, HideInInspector] private LocalPlayerMode _playerMode;
        [SerializeField] private PlayerController2D _2dPlayer;
        [SerializeField] private PlayerControllerVR _vrPlayer;
        private PlayerController _activePlayer => _playerMode == LocalPlayerMode.TwoD? _2dPlayer : _vrPlayer;

        private PlayerStateConfig _stateConfig;
        private IPlayerSettingsProvider _playerSettingsProvider;
        private IPlayerAppearanceOverridesProvider _appearanceOverridesProvider;

        public void Initialize(PlayerStateConfig playerStateConfig, IPlayerSettingsProvider playerSettingsProvider, IPlayerAppearanceOverridesProvider appearanceOverridesProvider)
        {
            _playerMode = LocalPlayerMode.TwoD; //TODO - support other modes 

            _stateConfig = playerStateConfig;
            _playerSettingsProvider = playerSettingsProvider;
            _appearanceOverridesProvider = appearanceOverridesProvider;

            //There MUST be a PlayerSettingsProvider to spawn the player rig 
            _playerSettingsProvider.OnLocalChangeToPlayerSettings += HandleSettingsChanged;

            if (_appearanceOverridesProvider != null)
                _appearanceOverridesProvider.OnAppearanceOverridesChanged += HandleOverridesChanged;
        }

        private void HandleSettingsChanged() 
        {
            Debug.Log("Player rig detected settings changed"); //TODO, change local avatar
        }

        private void HandleOverridesChanged() 
        {
            Debug.Log("Player rig detected avatar overrides changed"); //TODO, change local avatar
        }


        private void Start()
        {
            if (_stateConfig.IsNetworked && _stateConfig.MultiplayerSupportPresent)
            {
                _stateConfig.MultiplayerSupport.RegisterLocalPlayer(this);
            }
        }

        private void OnDestroy() 
        {
            if (_stateConfig.IsNetworked && _stateConfig.MultiplayerSupportPresent)
            {
                _stateConfig.MultiplayerSupport.DeregisterLocalPlayer();
            }
        }

        public PlayerState GetPlayerState()
        {
            return new PlayerState(transform.position, transform.rotation, Camera.main.transform.position, Camera.main.transform.rotation);
        }

        private void Update()
        {
            if (_playerMode == LocalPlayerMode.TwoD)
            {
                //Move vr player to 2d player
            }
        }

        private void SetPlayerPosition(Vector3 position)
        {
            _activePlayer.transform.position = position;

            //How do we move back to the start position? 
            //SartPosition lives in the PluginRuntime
            //But the ui button that respawns the player lives in PluginRuntime 

            //FrameworkRuntime will have to emit an event to PluginService to say "OnPlayerRequestRespawn"
        }

        private void SetPlayerRotation(Quaternion rotation)
        {
            _activePlayer.transform.rotation = rotation;
        }
    }

    public enum LocalPlayerMode
    {
        TwoD, 
        VR
    }

    /*
     * 
     *  For the VCs, we have a state module regardless of whether we're networked 
     *  We probably want that too for the player? Although, the state is tied to the transform
     *  The state module registers itself with the syncer 
     *  
     *  I'm not sure there's a reason to inject the state into the player?
     *  If networked, register with the syncer
     * 
     */
}
