using System;
using UnityEngine;
using static VE2.Core.Player.API.PlayerSerializables;

namespace VE2.Core.Player.API
{
    public interface IPlayerService //TODO - need to wire into the config
    {
        public bool IsVRMode { get; }

        public void SetAvatarHeadOverride(ushort type);
        public void SetAvatarTorsoOverride(ushort type);

        public void ClearAvatarHeadOverride();
        public void ClearAvatarTorsoOverride();

        public Camera ActiveCamera { get; }
    }
}
