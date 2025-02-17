using System;
using UnityEngine;
using static VE2.Common.CommonSerializables;

internal interface IPlayerSettingsHandler
{
    public bool RememberPlayerSettings { get; set; }

    public PlayerPresentationConfig PlayerPresentationConfig { get; set; }

    //public event Action<PlayerPresentationConfig> OnPlayerPresentationConfigChanged;

    public void SavePlayerAppearance();
}
