using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ViRSE.FrameworkRuntime.LocalPlayerRig
{
    public class LocalPlayerService : MonoBehaviour, ILocalPlayerRig
    {
        #region Plugin Runtime interfaces
        public Vector3 Position { get => _activePlayer.transform.position; set => SetPlayerPosition(value); }
        public Quaternion Rotation { get => _activePlayer.transform.rotation; set => throw new System.NotImplementedException(); }
        #endregion

        private LocalPlayerMode _playerMode;
        private PlayerController2D _2dPlayer;
        private PlayerControllerVR _vrPlayer;
        private PlayerController _activePlayer => _playerMode == LocalPlayerMode.TwoD? _2dPlayer : _vrPlayer;

        public void Initialize(PlayerSettings playerSettings)
        {

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
    }

    public enum LocalPlayerMode
    {
        TwoD, 
        VR
    }
}
