using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using static VE2.Core.Player.API.PlayerSerializables;

namespace VE2.Core.Player.API
{
    // public interface IPlayerGameObjectHandler
    // {
    //     public void SetBuiltInGameObjectEnabled(bool isEnabled);
    //     public void SetCustomGameObjectEnabled(bool isEnabled);
    //     public void SetCustomGameObjectIndex(ushort type);
    // }

    // public interface IPlayerGameObjectsHandler
    // {
    //     public IPlayerGameObjectHandler HeadHandler { get; }
    //     public IPlayerGameObjectHandler TorsoHandler { get; }
    //     // public IPlayerGameObjectsHandler HandVRRightHandler { get; }
    //     // public IPlayerGameObjectsHandler HandVRLeftHandler { get; }
    // }

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
