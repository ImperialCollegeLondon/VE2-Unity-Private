using System;
using static ViRSE.Common.CoreCommonSerializables;

namespace ViRSE.Common
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
