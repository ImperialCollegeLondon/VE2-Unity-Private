using System;
using UnityEngine;
using static VE2.Common.CommonSerializables;

internal interface IPlayerServiceInternal : IPlayerService
{
    public bool RememberPlayerSettings { get; set; }

    /// <summary>
    /// call MarkPlayerSettingsUpdated after modifying this property
    /// </summary>
    public PlayerPresentationConfig PlayerPresentationConfig { set; }

    public void MarkPlayerSettingsUpdated() { }
}
