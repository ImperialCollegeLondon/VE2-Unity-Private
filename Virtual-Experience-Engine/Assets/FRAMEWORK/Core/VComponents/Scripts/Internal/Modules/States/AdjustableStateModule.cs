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
        [SerializeField] public float MinimumValue = 0;
        [SerializeField] public float MaximumValue = 1;
        [SerializeField] public float StartingValue = 0;
        [EndGroup, SerializeField] public bool EmitValueOnStart = true;
    }
    internal class AdjustableStateModule : BaseWorldStateModule, IAdjustableStateModule
    {
        public float Value { get => _state.Value; set => HandleExternalAdjust(value); }
        public UnityEvent<float> OnValueAdjusted => _config.OnValueAdjusted;
        public ushort MostRecentInteractingClientID => _state.MostRecentInteractingClientID;

        public float MinimumValue { get => _config.MinimumValue; set => _config.MinimumValue = value; }
        public float MaximumValue { get => _config.MaximumValue; set => _config.MaximumValue = value; }
        private AdjustableState _state => (AdjustableState)State;
        private AdjustableStateConfig _config => (AdjustableStateConfig)Config;

        internal float Range => (MaximumValue - MinimumValue) + 1;
        internal bool IsAtMinimumValue => Value == _config.MinimumValue;
        internal bool IsAtMaximumValue => Value == _config.MaximumValue;

        public AdjustableStateModule(CommonSerializables.VE2Serializable state, BaseWorldStateConfig config, string id, IWorldStateSyncService worldStateSyncService) : base(state, config, id, worldStateSyncService)
        {
            if (_config.EmitValueOnStart == true)
            {
                OnValueAdjusted?.Invoke(Value);
            }
        }


        private void HandleExternalAdjust(float newValue)
        {
            if (newValue != _state.Value)
                SetValue(newValue, ushort.MaxValue);
        }

        public void SetValue(float value, ushort clientID)
        {   
            if (value < _config.MinimumValue || value > _config.MaximumValue)
            {
                Debug.LogError("Value is beyond limits");
                return;
            }

            _state.Value = value;

            if (clientID != ushort.MaxValue)
                _state.MostRecentInteractingClientID = clientID;

            _state.StateChangeNumber++;

            InvokeCustomerOnValueAdjustedEvent(_state.Value);
        }

        private void InvokeCustomerOnValueAdjustedEvent(float value)
        {
            try
            {
                _config.OnValueAdjusted?.Invoke(value);
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
                InvokeCustomerOnValueAdjustedEvent(_state.Value);
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

