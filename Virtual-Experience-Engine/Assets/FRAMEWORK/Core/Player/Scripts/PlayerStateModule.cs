using System;
using System.IO;
using UnityEngine;
using VE2.Common;
using VE2.Core.Common;
using static VE2.Common.CommonSerializables;

namespace VE2.Core.Player
{
    public class PlayerStateModule : BaseStateModule, IPlayerStateModule //TODO - customer interfaces for changing player position/rotation
    {
        public PlayerTransformData PlayerTransformData
        {
            get => (PlayerTransformData)State;
            set => State.Bytes = value.Bytes;
        }

        public AvatarAppearance AvatarAppearance
        {
            get
            {
                if (_appearanceOverridesProvider != null)
                    return new(_playerSettingsProvider.UserSettings.PresentationConfig, _appearanceOverridesProvider.HeadOverrideType, _appearanceOverridesProvider.TorsoOverrideType);
                else
                    return new(_playerSettingsProvider.UserSettings.PresentationConfig, AvatarAppearanceOverrideType.None, AvatarAppearanceOverrideType.None);
            }
        }

        public event Action<AvatarAppearance> OnAvatarAppearanceChanged;

        private readonly IPlayerSettingsProvider _playerSettingsProvider;
        private readonly IPlayerAppearanceOverridesProvider _appearanceOverridesProvider;

        public PlayerStateModule(PlayerTransformData state, BaseStateConfig config,
                PlayerStateModuleContainer playerStateModuleContainer, IPlayerSettingsProvider playerSettingsProvider, IPlayerAppearanceOverridesProvider appearanceOverridesProvider)
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

        public override void TearDown()
        {
            base.TearDown();

            _playerSettingsProvider.OnLocalChangeToPlayerSettings -= HandleAvatarAppearanceChanged;

            if (_appearanceOverridesProvider != null)
                _appearanceOverridesProvider.OnAppearanceOverridesChanged -= HandleAvatarAppearanceChanged;
        }
    }

}

