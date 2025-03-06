using UnityEngine;
using UnityEngine.Events;

namespace VE2.Core.VComponents.API
{
    public interface IV_HandheldAdjustable
    {
        #region State Module Interface
        internal IAdjustableStateModule _StateModule { get; }

        public UnityEvent<float> OnValueAdjusted => _StateModule.OnValueAdjusted;
        public float Value { get { return _StateModule.Value; } set { _StateModule.Value = value; } }
        public ushort MostRecentInteractingClientID => _StateModule.MostRecentInteractingClientID;
        #endregion

        #region Handheld Interaction Module Interface
        internal IHandheldScrollInteractionModule _HandheldScrollModule { get; }
        #endregion

        #region General Interaction Module Interface
        public bool AdminOnly { get => _HandheldScrollModule.AdminOnly; set => _HandheldScrollModule.AdminOnly = value; }
        public bool EnableControllerVibrations { get => _HandheldScrollModule.EnableControllerVibrations; set => _HandheldScrollModule.EnableControllerVibrations = value; }
        public bool ShowTooltipsAndHighlight { get => _HandheldScrollModule.ShowTooltipsAndHighlight; set => _HandheldScrollModule.ShowTooltipsAndHighlight = value; }
        #endregion
    }
}

