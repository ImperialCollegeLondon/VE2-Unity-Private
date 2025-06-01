using System;
using UnityEngine;
using UnityEngine.Events;
using static VE2.Core.Player.API.PlayerSerializables;

namespace VE2.Core.Player.API
{
    public interface IPlayerService //TODO - need to wire into the config
    {
        public bool IsVRMode { get; }
        public UnityEvent OnChangeToVRMode { get; }
        public UnityEvent OnChangeTo2DMode { get; }

        public void SetAvatarHeadOverride(ushort type);
        public void SetAvatarTorsoOverride(ushort type);

        public void ClearAvatarHeadOverride();
        public void ClearAvatarTorsoOverride();

        public Camera ActiveCamera { get; }

        public UnityEvent OnTeleport { get; }
        public UnityEvent OnSnapTurn { get; }
        public UnityEvent OnHorizontalDrag { get; }
        public UnityEvent OnVerticalDrag { get; }
        public UnityEvent OnJump2D { get; }
        public UnityEvent OnCrouch2D { get; }
        public UnityEvent OnResetViewVR { get; }

        public Vector3 PlayerPosition { get; set; }
    }
}
