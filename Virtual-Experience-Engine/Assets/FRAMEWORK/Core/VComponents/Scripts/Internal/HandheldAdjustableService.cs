using System;
using UnityEditor.PackageManager;
using UnityEngine;
using VE2.Common;
using VE2.Core.VComponents.InteractableInterfaces;
using VE2.Core.VComponents.NonInteractableInterfaces;
using static VE2.Common.CommonSerializables;

namespace VE2.Core.VComponents.Internal
{
    [Serializable]
    public class HandheldAdjustableConfig
    {
        [SerializeField, IgnoreParent] public AdjustableStateConfig StateConfig = new();
        [SerializeField, IgnoreParent] public HandheldAdjustableServiceConfig HandheldAdjustableServiceConfig = new();
        [SpaceArea(spaceAfter: 10), SerializeField, IgnoreParent] public GeneralInteractionConfig GeneralInteractionConfig = new();
    }

    [Serializable]
    public class HandheldAdjustableServiceConfig
    {
        [BeginGroup(Style = GroupStyle.Round)]
        [Title("Scroll Settings")]
        [SerializeField] public bool LoopValues = false;

        [EndGroup, SerializeField] public float IncrementPerScrollTick = 1;

        // [SerializeField] public bool SinglePressScroll = false;
        // [ShowIf("SinglePressScroll", false)]
        // [EndGroup, SerializeField] public float IncrementPerSecondVRStickHeld = 4;
    }
    public class HandheldAdjustableService
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

        public HandheldAdjustableService(HandheldAdjustableConfig config, VE2Serializable state, string id, WorldStateModulesContainer worldStateModulesContainer)
        {
            _StateModule = new(state, config.StateConfig, id, worldStateModulesContainer);
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
            float targetValue = _StateModule.OutputValue + _handheldAdjustableServiceConfig.IncrementPerScrollTick;

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
            float targetValue = _StateModule.OutputValue - _handheldAdjustableServiceConfig.IncrementPerScrollTick;

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

