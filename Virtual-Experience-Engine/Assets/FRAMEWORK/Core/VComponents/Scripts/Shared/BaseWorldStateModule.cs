using System;
using UnityEngine;
using VE2.Common.API;
using VE2.Common.Shared;
using VE2.Core.VComponents.API;
using static VE2.Common.Shared.CommonSerializables;

namespace VE2.Core.VComponents.Shared
{
    [Serializable]
    internal class WorldStateSyncConfig
    {
        [BeginGroup(Style = GroupStyle.Round, ApplyCondition = true)]
        [Title("Sync Settings", ApplyCondition = true)]
        [SerializeField] public bool IsNetworked = true;

        [IgnoreParent]
        [DisableIf(nameof(IsNetworked), false)]
        [Suffix("Hz"), Range(0.2f, 50f)]
        [SerializeField] public float TransmissionFrequency = 1;

        [DisableIf(nameof(IsNetworked), false)]
        [SerializeField, EndGroup] public TransmissionProtocol TransmissionType;
    }

    //Note - this lives here so other packages can use it
    internal abstract class BaseWorldStateModule : IWorldStateModule, IBaseStateModule //TOOD: Refactor into IWorldStateModule and IWorldStateModuleInternal
    {
        public VE2Serializable State { get; }
        protected WorldStateSyncConfig _SyncConfig { get; private set; }
        private readonly IWorldStateSyncableContainer _worldStateModulesContainer;

        private bool _wasNetworkedLastFrame;
        public bool IsNetworked => _SyncConfig.IsNetworked;

        public TransmissionProtocol TransmissionProtocol => _SyncConfig.TransmissionType;
        public float TransmissionFrequency => _SyncConfig.TransmissionFrequency;

        public string ID { get; private set; }
        public byte[] StateAsBytes { get => State.Bytes; set => UpdateBytes(value); }
        protected abstract void UpdateBytes(byte[] newBytes);

        public void SetNetworked(bool isNetworked) => _SyncConfig.IsNetworked = isNetworked;

        public BaseWorldStateModule(VE2Serializable state, WorldStateSyncConfig config, string id, IWorldStateSyncableContainer worldStateModulesContainer)
        {
            ID = id;
            State = state;
            _SyncConfig = config;

            _worldStateModulesContainer =  worldStateModulesContainer;

            //If we're networked, wait until FixedUpdate to register 
            //This allows for any initialization to complete before the module's state is queried 
            _wasNetworkedLastFrame = false;
        }

        public virtual void HandleFixedUpdate()
        {
            if (IsNetworked && !_wasNetworkedLastFrame)
                _worldStateModulesContainer.RegisterWorldStateSyncable(this);
            else if (!IsNetworked && _wasNetworkedLastFrame)
                _worldStateModulesContainer.DeregisterWorldStateSyncable(this);

            _wasNetworkedLastFrame = IsNetworked;
        }

        public virtual void TearDown() 
        {
            if (IsNetworked)
                _worldStateModulesContainer.DeregisterWorldStateSyncable(this);
        }
    }
}
