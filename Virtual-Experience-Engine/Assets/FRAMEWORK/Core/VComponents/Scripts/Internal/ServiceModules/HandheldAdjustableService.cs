using System;
using UnityEngine;
using VE2.Core.VComponents.API;
using static VE2.Core.Common.CommonSerializables;

namespace VE2.Core.VComponents.Internal
{
    [Serializable]
    internal class HandheldAdjustableConfig
    {
        [SerializeField, IgnoreParent] public AdjustableStateConfig StateConfig = new();
        [SerializeField, IgnoreParent] public HandheldAdjustableServiceConfig HandheldAdjustableServiceConfig = new();
        [SpaceArea(spaceAfter: 10), SerializeField, IgnoreParent] public GeneralInteractionConfig GeneralInteractionConfig = new();
    }

    [Serializable]
    internal class HandheldAdjustableServiceConfig
    {
        [BeginGroup(Style = GroupStyle.Round)]
        [Title("Scroll Settings")]
        [SerializeField] public bool LoopValues = false;

        [EndGroup, SerializeField] public float IncrementPerScrollTick = 1;

        // [SerializeField] public bool SinglePressScroll = false;
        // [ShowIf("SinglePressScroll", false)]
        // [EndGroup, SerializeField] public float IncrementPerSecondVRStickHeld = 4;
    }
    internal class HandheldAdjustableService
    {
        #region Interfaces
        public IAdjustableStateModule StateModule => _StateModule;
        public IHandheldScrollInteractionModule HandheldScrollInteractionModule => _HandheldScrollInteractionModule;
        #endregion

        #region Modules
        private readonly AdjustableStateModule _StateModule;
        private readonly HandheldScrollInteractionModule _HandheldScrollInteractionModule;
        #endregion

        private readonly HandheldAdjustableServiceConfig  _handheldAdjustableServiceConfig;
        private HandheldAdjustableConfig config;
        private AdjustableState state;
        private string id;
        private IWorldStateSyncService worldStateSyncService;

        public HandheldAdjustableService(HandheldAdjustableConfig config, VE2Serializable state, string id, IWorldStateSyncService worldStateSyncService)
        {
            _StateModule = new(state, config.StateConfig, id, worldStateSyncService);
            _HandheldScrollInteractionModule = new(config.GeneralInteractionConfig);

            _handheldAdjustableServiceConfig = config.HandheldAdjustableServiceConfig;
            _HandheldScrollInteractionModule.OnScrollUp += HandleScrollUp;
            _HandheldScrollInteractionModule.OnScrollDown += HandleScrollDown;
        }

        public void HandleFixedUpdate()
        {
            _StateModule.HandleFixedUpdate();
        }

        private void HandleScrollUp(ushort clientID)
        {
            float targetValue = _StateModule.Value + _handheldAdjustableServiceConfig.IncrementPerScrollTick;

            if (_StateModule.IsAtMaximumValue)
            {
                if (_handheldAdjustableServiceConfig.LoopValues)
                {
                    targetValue -= _StateModule.Range;
                }
            }

            _StateModule.SetValue(targetValue, clientID);
            
        }

        private void HandleScrollDown(ushort clientID)
        {
            float targetValue = _StateModule.Value - _handheldAdjustableServiceConfig.IncrementPerScrollTick;

            if (_StateModule.IsAtMinimumValue)
            {
                if (_handheldAdjustableServiceConfig.LoopValues)
                {
                    targetValue += _StateModule.Range;
                }
            }

            _StateModule.SetValue(targetValue, clientID);
        }

        public void TearDown()
        {
            _StateModule.TearDown();
        }
    }
}

