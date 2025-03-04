using System;
using System.Collections.Generic;
using UnityEngine;
using VE2.Core.Common;
using static VE2.Core.Player.API.PlayerSerializables;

namespace VE2.Core.Player.API
{
    internal interface IPlayerServiceInternal : IPlayerService
    {
        public bool RememberPlayerSettings { get; set; }

        public PlayerTransformData PlayerTransformData { get; }

        /// <summary>
        /// call MarkPlayerSettingsUpdated after modifying this property
        /// </summary>
        public OverridableAvatarAppearance OverridableAvatarAppearance { get; }
        public void MarkPlayerSettingsUpdated() { }
        public event Action<OverridableAvatarAppearance> OnOverridableAvatarAppearanceChanged;

        public List<GameObject> HeadOverrideGOs { get; }
        public List<GameObject> TorsoOverrideGOs { get; }

        public TransmissionProtocol TransmissionProtocol { get; }
        public float TransmissionFrequency { get; }

        public AndroidJavaObject AddArgsToIntent(AndroidJavaObject intent);
    }
}