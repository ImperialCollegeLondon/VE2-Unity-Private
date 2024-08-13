using UnityEngine;
using UnityEngine.UIElements;

namespace ViRSE.FrameworkRuntime.LocalPlayerRig
{
    public class LocalPlayerService : MonoBehaviour, ILocalPlayerRig
    {
        [SerializeField] private GameObject _playerRig2DPrefab;

        #region Plugin Runtime interfaces
        public Vector3 Position { get => _activePlayer.transform.position; set => SetPlayerPosition(value); }
        public Quaternion Rotation { get => _activePlayer.transform.rotation; set => SetPlayerRotation(value); }
        #endregion

        private LocalPlayerMode _playerMode;
        private PlayerController2D _2dPlayer;
        private PlayerControllerVR _vrPlayer;
        private PlayerController _activePlayer => _playerMode == LocalPlayerMode.TwoD? _2dPlayer : _vrPlayer;

        public void Initialize(UserSettings playerSettings)
        {
            _playerMode = LocalPlayerMode.TwoD;
            _2dPlayer = Instantiate(_playerRig2DPrefab).GetComponent<PlayerController2D>();
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
            Debug.Log("here");
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
