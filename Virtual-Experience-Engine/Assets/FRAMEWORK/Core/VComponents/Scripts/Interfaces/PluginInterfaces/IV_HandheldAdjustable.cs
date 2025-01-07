using UnityEngine;
using UnityEngine.Events;
using VE2.Core.VComponents.InteractableInterfaces;
using VE2.Core.VComponents.NonInteractableInterfaces;

namespace VE2.Core.VComponents.PluginInterfaces
{
    public interface IV_HandheldAdjustable
    {
        #region State Module Interface
        protected IAdjustableStateModule _StateModule { get; }

        public UnityEvent<float> OnValueAdjusted => _StateModule.OnValueAdjusted;
        public float Value { get { return _StateModule.OutputValue; } set { _StateModule.OutputValue = value; } }
        public float MinimumValue { get => _StateModule.MinimumOutputValue; set => _StateModule.MinimumOutputValue = value; }
        public float MaximumValue { get => _StateModule.MaximumOutputValue; set => _StateModule.MaximumOutputValue = value; }
        public ushort MostRecentInteractingClientID => _StateModule.MostRecentInteractingClientID;
        #endregion

        #region Handheld Interaction Module Interface
        protected IHandheldScrollInteractionModule _HandheldScrollModule { get; }
        #endregion

        #region General Interaction Module Interface
        public bool AdminOnly { get => _HandheldScrollModule.AdminOnly; set => _HandheldScrollModule.AdminOnly = value; }
        public bool EnableControllerVibrations { get => _HandheldScrollModule.EnableControllerVibrations; set => _HandheldScrollModule.EnableControllerVibrations = value; }
        public bool ShowTooltipsAndHighlight { get => _HandheldScrollModule.ShowTooltipsAndHighlight; set => _HandheldScrollModule.ShowTooltipsAndHighlight = value; }
        #endregion
    }
}

