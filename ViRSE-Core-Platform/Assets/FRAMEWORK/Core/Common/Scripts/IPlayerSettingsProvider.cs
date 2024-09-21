using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static ViRSE.Core.Shared.CoreCommonSerializables;

public interface IPlayerSettingsProvider
{
    public bool ArePlayerSettingsReady { get; }
    public event Action OnPlayerSettingsReady;
    public UserSettings UserSettings { get; }
    public string GameObjectName { get; }   
    public bool IsEnabled { get; }
}

public class UserSettings
{
    public PlayerPresentationConfig PlayerPresentationConfig { get; private set; }
    public PlayerVRControlConfig PlayerVRControlConfig { get; private set; }
    public Player2DControlConfig Player2DControlConfig { get; private set; }

    public UserSettings(PlayerPresentationConfig playerPresentationConfig, PlayerVRControlConfig playerVRControlConfig, Player2DControlConfig player2DControlConfig)
    {
        PlayerPresentationConfig = playerPresentationConfig;
        PlayerVRControlConfig = playerVRControlConfig;
        Player2DControlConfig = player2DControlConfig;
    }

    public UserSettings()
    {
        PlayerPresentationConfig = new PlayerPresentationConfig();
        PlayerVRControlConfig = new PlayerVRControlConfig();
        Player2DControlConfig = new Player2DControlConfig();
    }
}
