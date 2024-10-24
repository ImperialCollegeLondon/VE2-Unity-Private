using System;
using UnityEngine;
using ViRSE.Core.Shared;
using static ViRSE.Core.Shared.CoreCommonSerializables;

namespace ViRSE.Core.Player
{
    public class PlayerStateModule : BaseStateModule, IPlayerStateModule
    {
        public PlayerTransformData PlayerTransformData
        {
            get => (PlayerTransformData)State;
            set => State.Bytes = value.Bytes;
        }

        public ViRSEAvatarAppearance AvatarAppearance
        {
            get
            {
                if (_appearanceOverridesProvider != null)
                    return new(_playerSettingsProvider.UserSettings.PresentationConfig, _appearanceOverridesProvider.HeadOverrideType, _appearanceOverridesProvider.TorsoOverrideType);
                else
                    return new(_playerSettingsProvider.UserSettings.PresentationConfig, AvatarAppearanceOverrideType.None, AvatarAppearanceOverrideType.None);
            }
        }

        public event Action<ViRSEAvatarAppearance> OnAvatarAppearanceChanged;

        private readonly IPlayerSettingsProvider _playerSettingsProvider;
        private readonly IPlayerAppearanceOverridesProvider _appearanceOverridesProvider;

        public PlayerStateModule(PlayerTransformData state, BaseStateConfig config,
                ViRSEPlayerStateModuleContainer playerStateModuleContainer, IPlayerSettingsProvider playerSettingsProvider, IPlayerAppearanceOverridesProvider appearanceOverridesProvider)
                : base(state, config, playerStateModuleContainer)

        {
            PlayerTransformData = state;

            _playerSettingsProvider = playerSettingsProvider;
            _playerSettingsProvider.OnLocalChangeToPlayerSettings += HandleAvatarAppearanceChanged;

            _appearanceOverridesProvider = appearanceOverridesProvider;
            if (_appearanceOverridesProvider != null)
                _appearanceOverridesProvider.OnAppearanceOverridesChanged += HandleAvatarAppearanceChanged;
        }

        private void HandleAvatarAppearanceChanged()
        {
            OnAvatarAppearanceChanged?.Invoke(AvatarAppearance);
        }

        // private void SetPlayerPosition(Vector3 position)
        // {
        //     _activePlayer.transform.position = position;

        //     //How do we move back to the start position? 
        //     //SartPosition lives in the PluginRuntime
        //     //But the ui button that respawns the player lives in PluginRuntime 

        //     //FrameworkRuntime will have to emit an event to PluginService to say "OnPlayerRequestRespawn"
        // }

        // private void SetPlayerRotation(Quaternion rotation)
        // {
        //     _activePlayer.transform.rotation = rotation;
        // }

        public override void TearDown()
        {
            base.TearDown();

            _playerSettingsProvider.OnLocalChangeToPlayerSettings -= HandleAvatarAppearanceChanged;

            if (_appearanceOverridesProvider != null)
                _appearanceOverridesProvider.OnAppearanceOverridesChanged -= HandleAvatarAppearanceChanged;
        }
    }
}

