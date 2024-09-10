using UnityEngine;
using UnityEngine.UIElements;
using ViRSE.Core.Player;
using ViRSE.Core.Shared;
using static ViRSE.Core.Shared.CoreCommonSerializables;

namespace ViRSE.Core.Player
{
    public class Player : MonoBehaviour, ILocalPlayerRig
    {
        #region Plugin Runtime interfaces
        public Vector3 Position { get => _activePlayer.transform.position; set => SetPlayerPosition(value); }
        public Quaternion Rotation { get => _activePlayer.transform.rotation; set => SetPlayerRotation(value); }
        #endregion

        [SerializeField, HideInInspector] private LocalPlayerMode _playerMode;
        [SerializeField] private PlayerController2D _2dPlayer;
        [SerializeField] private PlayerControllerVR _vrPlayer;
        private PlayerController _activePlayer => _playerMode == LocalPlayerMode.TwoD? _2dPlayer : _vrPlayer;

        public void Initialize(PlayerSpawnConfig playerSpawnConfig, PlayerPresentationConfig PresentationConfig)
        {
            _playerMode = LocalPlayerMode.TwoD;

            //TODO, process configs 
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
}
