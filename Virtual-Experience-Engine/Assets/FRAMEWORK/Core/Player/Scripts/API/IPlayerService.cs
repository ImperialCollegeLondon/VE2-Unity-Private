using System;
using UnityEngine;
using UnityEngine.Events;
using static VE2.Core.Player.API.PlayerSerializables;

namespace VE2.Core.Player.API
{
    public interface IPlayerGameObjectsHandler
    {
        public void SetBuiltInGameObjectEnabled(bool isEnabled);
        public void SetCustomGameObjectEnabled(bool isEnabled);
        public void SetCustomGameObjectIndex(ushort type);
    }

    public interface IPlayerGameObjectHandler
    {
        public IPlayerGameObjectsHandler HeadHandler { get; }
        public IPlayerGameObjectsHandler TorsoHandler { get; }
        // public IPlayerGameObjectsHandler HandVRRightHandler { get; }
        // public IPlayerGameObjectsHandler HandVRLeftHandler { get; }
    }

    public interface IPlayerService
    {
        public bool IsVRMode { get; }
        public UnityEvent OnChangeToVRMode { get; }
        public UnityEvent OnChangeTo2DMode { get; }

        public IPlayerGameObjectsHandler PlayerGameObjectsHandler { get; }

        public Camera ActiveCamera { get; }

        public UnityEvent OnTeleport { get; }
        public UnityEvent OnSnapTurn { get; }
        public UnityEvent OnHorizontalDrag { get; }
        public UnityEvent OnVerticalDrag { get; }
        public UnityEvent OnJump2D { get; }
        public UnityEvent OnCrouch2D { get; }
        public UnityEvent OnResetViewVR { get; }

        public Vector3 PlayerPosition { get; }
        public void SetPlayerPosition(Vector3 position);
        public Quaternion PlayerRotation { get; }
        public void SetPlayerRotation(Quaternion rotation);
    }
}
