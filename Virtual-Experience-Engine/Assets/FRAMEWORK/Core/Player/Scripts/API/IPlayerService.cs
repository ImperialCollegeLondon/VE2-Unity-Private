using System;
using UnityEngine;
using static VE2.Core.Player.API.PlayerSerializables;

namespace VE2.Core.Player.API
{
    public interface IPlayerService //TODO - need to wire into the config
    {
        public bool IsVRMode { get; }

        public void SetAvatarHeadOverride(AvatarAppearanceOverrideType type);
        public void SetAvatarTorsoOverride(AvatarAppearanceOverrideType type);

        public void ClearAvatarHeadOverride() => SetAvatarHeadOverride(AvatarAppearanceOverrideType.None);
        public void ClearAvatarTorsoOverride() => SetAvatarTorsoOverride(AvatarAppearanceOverrideType.None);

        public Camera ActiveCamera { get; }
    }
}
