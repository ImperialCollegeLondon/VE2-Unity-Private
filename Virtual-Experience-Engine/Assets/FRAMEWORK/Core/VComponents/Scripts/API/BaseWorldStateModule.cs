using System;
using UnityEngine;
using VE2.Core.Common;
using static VE2.Core.Common.CommonSerializables;

namespace VE2.Core.VComponents.API
{
    [Serializable]
    internal class BaseWorldStateConfig 
    {
        [BeginGroup(Style = GroupStyle.Round, ApplyCondition = true)]
        [Title("Transmission Settings", ApplyCondition = true)]
        [HideIf(nameof(MultiplayerSupportPresent), false)]
        [SerializeField] public bool IsNetworked = true;

        [HideIf(nameof(MultiplayerSupportPresent), false)]
        [DisableIf(nameof(IsNetworked), false)]
        [EndGroup(ApplyCondition = true, Order = 5)]
        [SpaceArea(spaceAfter: 10, Order = -1), SerializeField, IgnoreParent] public RepeatedTransmissionConfig RepeatedTransmissionConfig = new(TransmissionProtocol.UDP, 1);

        private bool MultiplayerSupportPresent => VComponentsAPI.HasMultiPlayerSupport;
    }

    //Note - this lives here so other packages can use it
    internal abstract class BaseWorldStateModule : IWorldStateModule
    {
        public VE2Serializable State { get; }
        protected BaseWorldStateConfig Config { get; private set; }
        private readonly IWorldStateSyncService _worldStateSyncService;

        private bool _wasNetworkedLastFrame;
        public bool IsNetworked => Config.IsNetworked;

        public TransmissionProtocol TransmissionProtocol => Config.RepeatedTransmissionConfig.TransmissionType;
        public float TransmissionFrequency => Config.RepeatedTransmissionConfig.TransmissionFrequency;

        public string ID { get; private set; }
        public byte[] StateAsBytes { get => State.Bytes; set => UpdateBytes(value); }
        protected abstract void UpdateBytes(byte[] newBytes);

        public BaseWorldStateModule(VE2Serializable state, BaseWorldStateConfig config, string id, IWorldStateSyncService worldStateSyncService)
        {
            ID = id;
            State = state;
            Config = config;

            _worldStateSyncService =  worldStateSyncService;

            //If we're networked, wait until FixedUpdate to register 
            //This allows for any initialization to complete before the module's state is queried 
            _wasNetworkedLastFrame = false;
        }

        public virtual void HandleFixedUpdate()
        {
            if (_worldStateSyncService == null)
                return;

            if (IsNetworked && !_wasNetworkedLastFrame)
                _worldStateSyncService.RegisterWorldStateModule(this);
            else if (!IsNetworked && _wasNetworkedLastFrame)
                _worldStateSyncService.DeregisterWorldStateModule(this);

            _wasNetworkedLastFrame = IsNetworked;
        }

        public virtual void TearDown() => _worldStateSyncService?.DeregisterWorldStateModule(this);
    }
}
