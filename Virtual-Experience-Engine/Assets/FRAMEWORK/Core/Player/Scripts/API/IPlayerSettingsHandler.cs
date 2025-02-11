using System;
using static VE2.Common.CommonSerializables;

namespace VE2.Common 
{
    public interface IPlayerSettingsHandler
    {
        public PlayerPresentationConfig DefaultPlayerPresentationConfig { get; }

        public bool RememberPlayerSettings { get; set; }


        /// <summary>
        /// call MarkPlayerSettingsUpdated after modifying this property
        /// </summary>
        public PlayerPresentationConfig PlayerPresentationConfig { get; set; }

        public event Action<PlayerPresentationConfig> OnPlayerPresentationConfigChanged;

        public void MarkPlayerSettingsUpdated() { }

        public string GameObjectName {get;}
    }
}
