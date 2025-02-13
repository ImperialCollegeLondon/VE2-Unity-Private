using System;
using UnityEngine;
using static VE2.Common.CommonSerializables;

public interface IPlayerSettingsHandler
{
    public PlayerPresentationConfig PlayerPresentationConfig { get; set; }

    public event Action<PlayerPresentationConfig> OnPlayerPresentationConfigChanged;
}
