using System;
using UnityEngine;
using static VE2.Common.CommonSerializables;

public interface IPlayerService
{
    /// <summary>
    /// call MarkPlayerSettingsUpdated after modifying this property
    /// </summary>
    public PlayerPresentationConfig PlayerPresentationConfig { get; }

    public event Action<PlayerPresentationConfig> OnPlayerPresentationConfigChanged;

    public bool VRModeActive { get; }

    public string GameObjectName { get; }
}
