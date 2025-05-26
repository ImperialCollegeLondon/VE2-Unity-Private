using System;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using VE2.Common.Shared;
using VE2.Core.VComponents.API;
using VE2.Core.VComponents.Shared;
using static VE2.Common.Shared.CommonSerializables;

namespace VE2.Core.VComponents.Internal
{
    [Serializable]
    internal class AdjustableStateConfig
    {
        [BeginGroup(Style = GroupStyle.Round)]
        [Title("Adjustable State Settings", ApplyCondition = true)]
        [SerializeField, IgnoreParent] internal AdjustableStateDebug InspectorDebug = new();
        [SerializeField] public UnityEvent<float> OnValueAdjusted = new();
        [SerializeField] public float MinimumOutputValue = 0;
        [SerializeField] public float MaximumOutputValue = 1;
        [SerializeField] public float StartingOutputValue = 0;
        [SerializeField] public bool EmitValueOnStart = true; 
        [Title("Scroll Settings")]
        [EndGroup, SerializeField] public float IncrementPerScrollTick = 0.1f;    
    }

    [Serializable]
    internal class AdjustableStateDebug
    {
        [Title("Debug Output", ApplyCondition = true, Order = 50), SerializeField, ShowDisabledIf(nameof(IsInPlayMode), true)] public float Value = 0;
        [SerializeField, ShowDisabledIf(nameof(IsInPlayMode), true)] public ushort ClientID = ushort.MaxValue;
        [EditorButton(nameof(handleDebugUpdateStatePressed), "Update State", activityType: ButtonActivityType.OnPlayMode, ApplyCondition = true, Order = 10), SpaceArea(spaceAfter:15, ApplyCondition = true)]
        [Title("Debug Input", ApplyCondition = true), SerializeField, HideIf(nameof(IsInPlayMode), false)] private float _newOutputValue = 0;

        public void handleDebugUpdateStatePressed() 
        {
            Debug.Log($"Debug button pressed");
            OnDebugUpdateStatePressed?.Invoke(_newOutputValue);
        }
        internal event Action<float> OnDebugUpdateStatePressed;

        protected bool IsInPlayMode => Application.isPlaying;
    }

    internal class AdjustableStateModule : BaseWorldStateModule, IAdjustableStateModule
    {
        public float OutputValue => _state.Value;
        public void SetOutputValue(float newValue) => SetValue(newValue, ushort.MaxValue);
        public UnityEvent<float> OnValueAdjusted => _adjustableStateConfig.OnValueAdjusted;
        public IClientIDWrapper MostRecentInteractingClientID => _state.MostRecentInteractingClientID == ushort.MaxValue ? null : 
            new ClientIDWrapper(_state.MostRecentInteractingClientID, _state.MostRecentInteractingClientID == _localClientIdWrapper.Value);

        public float MinimumOutputValue { get => _adjustableStateConfig.MinimumOutputValue; set => _adjustableStateConfig.MinimumOutputValue = value; }
        public float MaximumOutputValue { get => _adjustableStateConfig.MaximumOutputValue; set => _adjustableStateConfig.MaximumOutputValue = value; }
        private AdjustableState _state => (AdjustableState)State;

        internal float Range => (MaximumOutputValue - MinimumOutputValue) + 1;
        internal bool IsAtMinimumValue => OutputValue == _adjustableStateConfig.MinimumOutputValue;
        internal bool IsAtMaximumValue => OutputValue == _adjustableStateConfig.MaximumOutputValue;

        internal event Action<float> OnValueChangedInternal;

        private readonly AdjustableStateConfig _adjustableStateConfig;
        private readonly IClientIDWrapper _localClientIdWrapper;

        public AdjustableStateModule(VE2Serializable state, AdjustableStateConfig adjustableStateConfig, WorldStateSyncConfig syncConfig, string id, IWorldStateSyncableContainer worldStateSyncableContainer, IClientIDWrapper localClientIdWrapper) 
            : base(state, syncConfig, id, worldStateSyncableContainer)
        {
            _adjustableStateConfig = adjustableStateConfig;

            if (_adjustableStateConfig.EmitValueOnStart)
                InvokeOnValueAdjustedEvents(_state.Value);

            _adjustableStateConfig.InspectorDebug.OnDebugUpdateStatePressed += SetOutputValue;
            _adjustableStateConfig.InspectorDebug.Value = _state.Value;
            _adjustableStateConfig.InspectorDebug.ClientID = _state.MostRecentInteractingClientID;

            _localClientIdWrapper = localClientIdWrapper;
        }

        public void SetValue(float value, ushort clientID)
        {   
            if (value < _adjustableStateConfig.MinimumOutputValue || value > _adjustableStateConfig.MaximumOutputValue)
            {
                Debug.LogError($"Value ({value}) is beyond limits");
                return;
            }

            _state.Value = value;

            if (clientID != ushort.MaxValue)
                _state.MostRecentInteractingClientID = clientID;

            _state.StateChangeNumber++;

            _adjustableStateConfig.InspectorDebug.Value = _state.Value;
            _adjustableStateConfig.InspectorDebug.ClientID = _state.MostRecentInteractingClientID;

            InvokeOnValueAdjustedEvents(_state.Value);
        }

        private void InvokeOnValueAdjustedEvents(float value)
        {
            OnValueChangedInternal?.Invoke(value);

            try
            {
                OnValueAdjusted?.Invoke(value);
            }
            catch (Exception e)
            {
                Debug.Log($"Error when emitting OnValueAdjusted from activatable with ID {ID} \n{e.Message}\n{e.StackTrace}");
            }
        }

        protected override void UpdateBytes(byte[] newBytes)
        {
            float oldValue = _state.Value;
            State.Bytes = newBytes;

            if (oldValue != _state.Value) 
                InvokeOnValueAdjustedEvents(_state.Value);
        }
    }

    [Serializable]
    internal class AdjustableState : VE2Serializable
    {
        public ushort StateChangeNumber { get; set; }
        public float Value { get; set; }
        public ushort MostRecentInteractingClientID { get; set; }

        public AdjustableState()
        {
            StateChangeNumber = 0;
            Value = float.MaxValue;
            MostRecentInteractingClientID = ushort.MaxValue;
        }

        public AdjustableState(float startingValue)
        {
            StateChangeNumber = 0;
            Value = startingValue;
            MostRecentInteractingClientID = ushort.MaxValue;
        }
        protected override byte[] ConvertToBytes()
        {
            using MemoryStream stream = new();
            using BinaryWriter writer = new(stream);

            writer.Write(StateChangeNumber);
            writer.Write(Value);
            writer.Write(MostRecentInteractingClientID);

            return stream.ToArray();
        }

        protected override void PopulateFromBytes(byte[] data)
        {
            using MemoryStream stream = new(data);
            using BinaryReader reader = new(stream);

            StateChangeNumber = reader.ReadUInt16();
            Value = reader.ReadSingle();
            MostRecentInteractingClientID = reader.ReadUInt16();
        }
    }
}

