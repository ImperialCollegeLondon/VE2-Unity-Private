using System;
using UnityEngine;
using static VE2.Common.CommonSerializables;

public interface IPlayerSettingsHandler
{
    public event Action<PlayerPresentationConfig> OnPlayerPresentationConfigChanged;
    public PlayerPresentationConfig PlayerPresentationConfig { get; }

    public PlayerPresentationConfig DefaultPlayerPresentationConfig { get; }
}
