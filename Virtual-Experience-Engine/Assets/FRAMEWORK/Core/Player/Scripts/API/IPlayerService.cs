using System;
using UnityEngine;
using static VE2.Common.CommonSerializables;

public interface IPlayerService
{
    public bool VRModeActive { get; }
    public void SetAvatarHeadOverride(AvatarAppearanceOverrideType type);
    public void SetAvatarTorsoOverride(AvatarAppearanceOverrideType type);

    public void ClearAvatarHeadOverride() => SetAvatarHeadOverride(AvatarAppearanceOverrideType.None);
    public void ClearAvatarTorsoOverride() => SetAvatarTorsoOverride(AvatarAppearanceOverrideType.None);
}
