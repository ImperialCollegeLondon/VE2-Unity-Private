using System;
using static VE2.Common.CoreCommonSerializables;

namespace VE2.Common
{
    public interface IPlayerSettingsProvider
    {
        public bool ArePlayerSettingsReady { get; }
        public event Action OnPlayerSettingsReady;
        public UserSettingsPersistable UserSettings { get; }
        public string GameObjectName { get; }
        public bool IsEnabled { get; }

        public void NotifyProviderOfChangeToUserSettings();
        public event Action OnLocalChangeToPlayerSettings;
    }
}
