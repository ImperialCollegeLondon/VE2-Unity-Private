using System;
using UnityEngine;
using VE2.Common;
using static VE2.Common.CommonSerializables;

namespace VE2.Core.Common
{
    [Serializable]
    public class BaseStateConfig
    {
        [BeginGroup(Style = GroupStyle.Round, ApplyCondition = true)]
        [Title("Transmission Settings", ApplyCondition = true)]
        [HideIf(nameof(MultiplayerSupportPresent), false)]
        [SerializeField] public bool IsNetworked = true;

        [HideIf(nameof(MultiplayerSupportPresent), false)]
        [DisableIf(nameof(IsNetworked), false)]
        [EndGroup(ApplyCondition = true, Order = 5)]
        [SpaceArea(spaceAfter: 10, Order = -1), SerializeField, IgnoreParent] public RepeatedTransmissionConfig RepeatedTransmissionConfig = new();

        [SerializeField, HideInInspector] public bool MultiplayerSupportPresent => MultiplayerSupport != null;
        public IMultiplayerSupport MultiplayerSupport => VE2CoreServiceLocator.Instance.MultiplayerSupport;
    }

    public abstract class BaseStateModule : IBaseStateModule
    {
        public VE2Serializable State { get; }
        protected BaseStateConfig Config { get; private set; }
        private readonly BaseStateModuleContainer _baseStateContainer;

        //public event Action OnBytesUpdated;

        private bool _wasNetworkedLastFrame;
        public bool IsNetworked => Config.IsNetworked;

        public TransmissionProtocol TransmissionProtocol => Config.RepeatedTransmissionConfig.TransmissionType;
        public float TransmissionFrequency => Config.RepeatedTransmissionConfig.TransmissionFrequency;


        public BaseStateModule(VE2Serializable state, BaseStateConfig config, BaseStateModuleContainer baseStateContainer)
        {
            State = state;
            Config = config;
            _baseStateContainer = baseStateContainer;

            //If we're networked, wait until FixedUpdate to register 
            //This allows for any initialization to complete before the module's state is queried 
            _wasNetworkedLastFrame = false;
        }

        public void HandleFixedUpdate()
        {
            if (IsNetworked && !_wasNetworkedLastFrame)
                _baseStateContainer.RegisterStateModule(this);
            else if (!IsNetworked && _wasNetworkedLastFrame)
                _baseStateContainer.DeregisterStateModule(this);

            _wasNetworkedLastFrame = IsNetworked;
        }

        public virtual void TearDown() => _baseStateContainer.DeregisterStateModule(this);
    }

    public abstract class BaseWorldStateModule : BaseStateModule, IWorldStateModule
    {
        public string ID { get; private set; }
        public byte[] StateAsBytes { get => State.Bytes; set => UpdateBytes(value); }
        protected abstract void UpdateBytes(byte[] newBytes);

        public BaseWorldStateModule(VE2Serializable state, BaseStateConfig config, string id, WorldStateModulesContainer worldStateModulesContainer) : base(state, config, worldStateModulesContainer)
        {
            ID = id;
        }
    }
}
