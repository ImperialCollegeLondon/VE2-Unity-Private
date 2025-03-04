using UnityEngine;
using VE2.Core.Player.API;

namespace VE2.Private.Hub
{
    internal class DebugPlayerCust : MonoBehaviour
    {
        private IPlayerServiceInternal _playerServiceInternal => (IPlayerServiceInternal)PlayerAPI.Player;

        public void SetPlayerRed() 
        {
            _playerServiceInternal.OverridableAvatarAppearance.PresentationConfig.AvatarRed = 255;
            _playerServiceInternal.OverridableAvatarAppearance.PresentationConfig.AvatarGreen = 0;
            _playerServiceInternal.OverridableAvatarAppearance.PresentationConfig.AvatarBlue = 0;
            _playerServiceInternal.OverridableAvatarAppearance.PresentationConfig.PlayerName = "Red";
            _playerServiceInternal.MarkPlayerSettingsUpdated();
        }

        public void SetPlayerGreen() 
        {
            _playerServiceInternal.OverridableAvatarAppearance.PresentationConfig.AvatarRed = 0;
            _playerServiceInternal.OverridableAvatarAppearance.PresentationConfig.AvatarGreen = 255;
            _playerServiceInternal.OverridableAvatarAppearance.PresentationConfig.AvatarBlue = 0;
            _playerServiceInternal.OverridableAvatarAppearance.PresentationConfig.PlayerName = "Green";
            _playerServiceInternal.MarkPlayerSettingsUpdated();
        }

        public void SetPlayerBlue() 
        {
            _playerServiceInternal.OverridableAvatarAppearance.PresentationConfig.AvatarRed = 0;
            _playerServiceInternal.OverridableAvatarAppearance.PresentationConfig.AvatarGreen = 0;
            _playerServiceInternal.OverridableAvatarAppearance.PresentationConfig.AvatarBlue = 255;
            _playerServiceInternal.OverridableAvatarAppearance.PresentationConfig.PlayerName = "Blue";
            _playerServiceInternal.MarkPlayerSettingsUpdated();
        }
    }
}
