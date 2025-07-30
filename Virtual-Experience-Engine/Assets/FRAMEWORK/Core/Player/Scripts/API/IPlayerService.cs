using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using static VE2.Core.Player.API.PlayerSerializables;

namespace VE2.Core.Player.API
{
    public interface IPlayerService
    {
        public bool IsVRMode { get; }
        public UnityEvent OnChangeToVRMode { get; }
        public UnityEvent OnChangeTo2DMode { get; }

        //public IPlayerGameObjectsHandler PlayerGameObjectsHandler { get; }
        public void SetBuiltInHeadEnabled(bool isEnabled);
        public void SetCustomHeadEnabled(bool isEnabled);
        public void SetCustomHeadIndex(ushort type);

        public void SetBuiltInTorsoEnabled(bool isEnabled);
        public void SetCustomTorsoEnabled(bool isEnabled);
        public void SetCustomTorsoIndex(ushort type);

        public void SetBuiltInRightHandVREnabled(bool isEnabled);
        public void SetCustomRightHandVREnabled(bool isEnabled);
        public void SetCustomRightHandVRIndex(ushort type);

        public void SetBuiltInLeftHandVREnabled(bool isEnabled);
        public void SetCustomLeftHandVREnabled(bool isEnabled);
        public void SetCustomLeftHandVRIndex(ushort type);

        public Camera ActiveCamera { get; }

        public UnityEvent OnTeleport { get; }
        public UnityEvent OnSnapTurn { get; }
        public UnityEvent OnHorizontalDrag { get; }
        public UnityEvent OnVerticalDrag { get; }
        //public UnityEvent OnFreeFlyModeEnter { get; }
        //public UnityEvent OnFreeFlyModeExit { get; }
        public UnityEvent OnJump2D { get; }
        public UnityEvent OnCrouch2D { get; }
        //public UnityEvent OnFreeFlyModeEnter2D { get; }
        //public UnityEvent OnFreeFlyModeExit2D { get; }
        public UnityEvent OnResetViewVR { get; }

        public Vector3 PlayerPosition { get; }
        public void SetPlayerPosition(Vector3 position);
        public Quaternion PlayerRotation { get; }
        public void SetPlayerRotation(Quaternion rotation);

        public Vector3 PlayerSpawnPoint { get; }

        public void ToggleFreeFlyMode(bool toggle);
    }
}
