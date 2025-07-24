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
    internal class Adjustable2DStateConfig
    {
        [BeginGroup(Style = GroupStyle.Round)]
        [Title("Adjustable State Settings", ApplyCondition = true)]
        [SerializeField, IgnoreParent] internal Adjustable2DStateDebug InspectorDebug = new();
        [SerializeField] public UnityEvent<Vector2> OnValueAdjusted = new();
        [SerializeField] public Vector2 MinimumOutputValue = new Vector2(0, 0);
        [SerializeField] public Vector2 MaximumOutputValue = new Vector2(1, 1);
        [SerializeField] public Vector2 StartingOutputValue = new Vector2(0.5f, 0.5f);
        [SerializeField] public bool EmitValueOnStart = true;
        [Title("Scroll Settings")]
        [EndGroup, SerializeField] public float IncrementPerScrollTick = 0.1f;
    }

    [Serializable]
    internal class Adjustable2DStateDebug
    {
        [Title("Debug Output", ApplyCondition = true, Order = 50), SerializeField, ShowDisabledIf(nameof(IsInPlayMode), true)] public Vector2 Value = new Vector2(0, 0);
        [SerializeField, ShowDisabledIf(nameof(IsInPlayMode), true)] public ushort ClientID = ushort.MaxValue;
        [EditorButton(nameof(handleDebugUpdateStatePressed), "Update State", activityType: ButtonActivityType.OnPlayMode, ApplyCondition = true, Order = 10), SpaceArea(spaceAfter: 15, ApplyCondition = true)]
        [Title("Debug Input", ApplyCondition = true), SerializeField, HideIf(nameof(IsInPlayMode), false)] private Vector2 _newOutputValue = new Vector2(0,0);

        public void handleDebugUpdateStatePressed()
        {
            Debug.Log($"Debug button pressed");
            OnDebugUpdateStatePressed?.Invoke(_newOutputValue);
        }
        internal event Action<Vector2> OnDebugUpdateStatePressed;

        protected bool IsInPlayMode => Application.isPlaying;
    }

    internal class Adjustable2DStateModule : BaseWorldStateModule, IAdjustable2DStateModule
    {
        public Vector2 OutputValue => _state.Value;
        public void SetOutputValue(Vector2 newValue) => SetOutputValueInternal(newValue, ushort.MaxValue);
        public UnityEvent<Vector2> OnValueAdjusted => _adjustable2DStateConfig.OnValueAdjusted;
        public IClientIDWrapper MostRecentInteractingClientID => _state.MostRecentInteractingClientID == ushort.MaxValue ? null :
            new ClientIDWrapper(_state.MostRecentInteractingClientID, _state.MostRecentInteractingClientID == _localClientIdWrapper.Value);

        public Vector2 MinimumOutputValue { get => _adjustable2DStateConfig.MinimumOutputValue; set => _adjustable2DStateConfig.MinimumOutputValue = value; }
        public Vector2 MaximumOutputValue { get => _adjustable2DStateConfig.MaximumOutputValue; set => _adjustable2DStateConfig.MaximumOutputValue = value; }
        private Adjustable2DState _state => (Adjustable2DState)State;

        //internal float Range => (MaximumOutputValue - MinimumOutputValue) + 1; //TODO - why +1 here?

        internal event Action<Vector2> OnValueChangedInternal;

        private readonly Adjustable2DStateConfig _adjustable2DStateConfig;
        private readonly IClientIDWrapper _localClientIdWrapper;

        public Adjustable2DStateModule(VE2Serializable state, Adjustable2DStateConfig adjustable2DStateConfig, WorldStateSyncConfig syncConfig, string id, IWorldStateSyncableContainer worldStateSyncableContainer, IClientIDWrapper localClientIdWrapper)
            : base(state, syncConfig, id, worldStateSyncableContainer)
        {
            _adjustable2DStateConfig = adjustable2DStateConfig;

            _adjustable2DStateConfig.InspectorDebug.OnDebugUpdateStatePressed += SetOutputValue;
            _adjustable2DStateConfig.InspectorDebug.Value = _state.Value;
            _adjustable2DStateConfig.InspectorDebug.ClientID = _state.MostRecentInteractingClientID;

            _localClientIdWrapper = localClientIdWrapper;
        }

        //Can't be called in the constructor, as this will emit events, that may trigger the plugin to access the state module before it is fully initialized.
        internal void InitializeStateWithStartingValue()
        {
            SetOutputValueInternal(_adjustable2DStateConfig.StartingOutputValue, ushort.MaxValue, _adjustable2DStateConfig.EmitValueOnStart);
        }

        internal void SetOutputValueInternal(Vector2 value, ushort clientID = ushort.MaxValue, bool shouldEmitPluginEvent = true)
        {
            if (_state.Value == value)
                return;

            if (value.x < _adjustable2DStateConfig.MinimumOutputValue.x || value.y < _adjustable2DStateConfig.MinimumOutputValue.y
                || value.x > _adjustable2DStateConfig.MaximumOutputValue.x || value.y > _adjustable2DStateConfig.MaximumOutputValue.y)
            {
                Debug.LogError($"Value ({value}) is beyond limits");
                return;
            }

            _state.Value = value;

            if (clientID != ushort.MaxValue)
                _state.MostRecentInteractingClientID = clientID;

            _state.StateChangeNumber++;

            _adjustable2DStateConfig.InspectorDebug.Value = _state.Value;
            _adjustable2DStateConfig.InspectorDebug.ClientID = _state.MostRecentInteractingClientID;

            InvokeOnValueAdjustedEvents(_state.Value, shouldEmitPluginEvent);
        }

        private void InvokeOnValueAdjustedEvents(Vector2 value, bool shouldEmitPluginEvent = true)
        {
            OnValueChangedInternal?.Invoke(value);

            if (shouldEmitPluginEvent)
            {
                try
                {
                    OnValueAdjusted?.Invoke(value);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error when emitting OnValueAdjusted from activatable with ID {ID} \n{e.Message}\n{e.StackTrace}");
                }
            }
        }

        protected override void UpdateBytes(byte[] newBytes)
        {
            Vector2 oldValue = _state.Value;
            State.Bytes = newBytes;

            if (oldValue != _state.Value)
                InvokeOnValueAdjustedEvents(_state.Value);
        }
    }

    [Serializable]
    internal class Adjustable2DState : VE2Serializable
    {
        public ushort StateChangeNumber { get; set; }
        public Vector2 Value { get; set; }
        public ushort MostRecentInteractingClientID { get; set; }

        public Adjustable2DState()
        {
            StateChangeNumber = 0;
            Value = new Vector2(float.MaxValue, float.MaxValue);
            MostRecentInteractingClientID = ushort.MaxValue;
        }

        public Adjustable2DState(Vector2 startingValue)
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
            writer.Write(Value.x);
            writer.Write(Value.y);
            writer.Write(MostRecentInteractingClientID);

            return stream.ToArray();
        }

        protected override void PopulateFromBytes(byte[] data)
        {
            using MemoryStream stream = new(data);
            using BinaryReader reader = new(stream);

            StateChangeNumber = reader.ReadUInt16();
            Value = new Vector2(reader.ReadSingle(), reader.ReadSingle());

            MostRecentInteractingClientID = reader.ReadUInt16();
        }
    }
}

