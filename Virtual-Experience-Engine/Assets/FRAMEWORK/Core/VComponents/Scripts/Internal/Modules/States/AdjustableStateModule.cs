using System;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using VE2.Core.Common;
using VE2.Core.VComponents.API;
using static VE2.Core.Common.CommonSerializables;

namespace VE2.Core.VComponents.Internal
{
    [Serializable]
    internal class AdjustableStateConfig : BaseWorldStateConfig
    {
        [BeginGroup(Style = GroupStyle.Round)]
        [Title("Adjustable State Settings", ApplyCondition = true)]
        [SerializeField] public UnityEvent<float> OnValueAdjusted = new();
        [SerializeField] public float MinimumOutputValue = 0;
        [SerializeField] public float MaximumOutputValue = 1;
        [SerializeField] public float StartingOutputValue = 0;
        [SerializeField] public bool EmitValueOnStart = true; 
        [Title("Scroll Settings")]
        [EndGroup, SerializeField] public float IncrementPerScrollTick = 0.1f;    

    }
    internal class AdjustableStateModule : BaseWorldStateModule, IAdjustableStateModule
    {
        public float OutputValue { get => _state.Value; set => HandleExternalAdjust(value); }
        public UnityEvent<float> OnValueAdjusted => _config.OnValueAdjusted;
        public ushort MostRecentInteractingClientID => _state.MostRecentInteractingClientID;

        public float MinimumOutputValue { get => _config.MinimumOutputValue; set => _config.MinimumOutputValue = value; }
        public float MaximumOutputValue { get => _config.MaximumOutputValue; set => _config.MaximumOutputValue = value; }
        private AdjustableState _state => (AdjustableState)State;
        private AdjustableStateConfig _config => (AdjustableStateConfig)Config;

        internal float Range => (MaximumOutputValue - MinimumOutputValue) + 1;
        internal bool IsAtMinimumValue => OutputValue == _config.MinimumOutputValue;
        internal bool IsAtMaximumValue => OutputValue == _config.MaximumOutputValue;

        internal event Action<float> OnValueChangedInternal;

        public AdjustableStateModule(CommonSerializables.VE2Serializable state, BaseWorldStateConfig config, string id, IWorldStateSyncService worldStateSyncService) : base(state, config, id, worldStateSyncService)
        {
            if (_config.EmitValueOnStart == true)
            {
                OnValueAdjusted?.Invoke(OutputValue);
            }
        }

        private void HandleExternalAdjust(float newValue)
        {
            SetValue(newValue, ushort.MaxValue);
        }

        public void SetValue(float value, ushort clientID)
        {   
            if (value < _config.MinimumOutputValue || value > _config.MaximumOutputValue)
            {
                Debug.LogError("Value is beyond limits");
                return;
            }

            _state.Value = value;

            if (clientID != ushort.MaxValue)
                _state.MostRecentInteractingClientID = clientID;

            _state.StateChangeNumber++;

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
    public class AdjustableState : VE2Serializable
    {
        public ushort StateChangeNumber { get; set; }
        public float Value { get; set; }
        public ushort MostRecentInteractingClientID { get; set; }

        public AdjustableState()
        {
            StateChangeNumber = 0;
            Value = 0;
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

