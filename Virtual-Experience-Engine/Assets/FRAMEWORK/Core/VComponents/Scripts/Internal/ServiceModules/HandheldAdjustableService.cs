using System;
using UnityEngine;
using VE2.Common.API;
using VE2.Common.Shared;
using VE2.Core.VComponents.API;
using VE2.Core.VComponents.Shared;
using static VE2.Common.Shared.CommonSerializables;

namespace VE2.Core.VComponents.Internal
{
    [Serializable]
    internal class HandheldAdjustableConfig
    {
        public void OpenDocs() => Application.OpenURL("https://www.notion.so/V_HandHeldAdjustable-20f0e4d8ed4d81fb987efeb3ca70dd9e?source=copy_link");
        [EditorButton(nameof(OpenDocs), "Open Docs", PositionType = ButtonPositionType.Above)]
        [SerializeField, IgnoreParent] public AdjustableStateConfig StateConfig = new();
        [SerializeField, IgnoreParent] public HandheldAdjustableServiceConfig HandheldAdjustableServiceConfig = new();

        [SpaceArea(spaceAfter: 10), SerializeField, IgnoreParent] public GeneralInteractionConfig GeneralInteractionConfig = new();

        [HideIf(nameof(MultiplayerSupportPresent), false)]
        [SerializeField, IgnoreParent] public WorldStateSyncConfig SyncConfig = new();

        private bool MultiplayerSupportPresent => VE2API.HasMultiPlayerSupport;
    }

    [Serializable]
    internal class HandheldAdjustableServiceConfig
    {
        [BeginGroup(Style = GroupStyle.Round)]
        [Title("Handheld Adjustable Interaction Settings")]
        [EndGroup, SerializeField] public bool LoopValues = false;

        // [SerializeField] public bool SinglePressScroll = false;
        // [ShowIf("SinglePressScroll", false)]
        // [EndGroup, SerializeField] public float IncrementPerSecondVRStickHeld = 4; //If uncommenting, remove the above [EndGroup] attribute
    }
    internal class HandheldAdjustableService
    {
        #region Interfaces
        public IAdjustableStateModule StateModule => _stateModule;
        public IHandheldScrollInteractionModule HandheldScrollInteractionModule => _HandheldScrollInteractionModule;
        #endregion

        #region Modules
        private readonly AdjustableStateModule _stateModule;
        private readonly HandheldScrollInteractionModule _HandheldScrollInteractionModule;
        #endregion

        private readonly HandheldAdjustableServiceConfig  _handheldAdjustableServiceConfig;
        private readonly AdjustableStateConfig  _adjustableStateConfig;

        public HandheldAdjustableService(HandheldAdjustableConfig config, AdjustableState state, string id, IWorldStateSyncableContainer worldStateSyncableContainer, IClientIDWrapper localClientIdWrapper)
        {
            _stateModule = new(state, config.StateConfig, config.SyncConfig, id, worldStateSyncableContainer, localClientIdWrapper);
            _HandheldScrollInteractionModule = new(config.GeneralInteractionConfig);

            _handheldAdjustableServiceConfig = config.HandheldAdjustableServiceConfig;
            _adjustableStateConfig = config.StateConfig;
            _HandheldScrollInteractionModule.OnScrollUp += HandleScrollUp;
            _HandheldScrollInteractionModule.OnScrollDown += HandleScrollDown;
        }

        public void HandleStart() => _stateModule.InitializeStateWithStartingValue();

        public void HandleFixedUpdate()
        {
            _stateModule.HandleFixedUpdate();
        }

        private void HandleScrollUp(ushort clientID)
        {
            float targetValue = _stateModule.OutputValue + _adjustableStateConfig.IncrementPerScrollTick;

            if (targetValue > _stateModule.MaximumOutputValue)
            {
                if (_handheldAdjustableServiceConfig.LoopValues)
                    targetValue -= _stateModule.Range;
                else
                    targetValue = Mathf.Clamp(targetValue, _stateModule.MinimumOutputValue, _stateModule.MaximumOutputValue);
            }

            _stateModule.SetOutputValueInternal(targetValue, clientID);
        }

        private void HandleScrollDown(ushort clientID)
        {
            float targetValue = _stateModule.OutputValue - _adjustableStateConfig.IncrementPerScrollTick;

            if (targetValue < _stateModule.MinimumOutputValue)
            {
                if (_handheldAdjustableServiceConfig.LoopValues)
                    targetValue += _stateModule.Range;
                else
                    targetValue = Mathf.Clamp(targetValue, _stateModule.MinimumOutputValue, _stateModule.MaximumOutputValue);
            }

            _stateModule.SetOutputValueInternal(targetValue, clientID);
        }

        public void TearDown()
        {
            _stateModule.TearDown();
        }
    }
}

