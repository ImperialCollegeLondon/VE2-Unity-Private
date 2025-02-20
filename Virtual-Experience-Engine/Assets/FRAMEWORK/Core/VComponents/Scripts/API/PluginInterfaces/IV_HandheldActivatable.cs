using UnityEngine.Events;

namespace VE2.Core.VComponents.API
{
    public interface IV_HandheldActivatable 
    {
        #region State Module Interface
        internal ISingleInteractorActivatableStateModule _StateModule { get; }

        public UnityEvent OnActivate => _StateModule.OnActivate;
        public UnityEvent OnDeactivate => _StateModule.OnDeactivate;

        public bool IsActivated { get { return _StateModule.IsActivated; } set { _StateModule.IsActivated = value; } }
        public ushort MostRecentInteractingClientID => _StateModule.MostRecentInteractingClientID;
        #endregion

        #region Handheld Interaction Module Interface
        internal IHandheldClickInteractionModule _HandheldClickModule{ get; }
        #endregion

        #region General Interaction Module Interface
        public bool AdminOnly {get => _HandheldClickModule.AdminOnly; set => _HandheldClickModule.AdminOnly = value; }
        public bool EnableControllerVibrations { get => _HandheldClickModule.EnableControllerVibrations; set => _HandheldClickModule.EnableControllerVibrations = value; }
        public bool ShowTooltipsAndHighlight { get => _HandheldClickModule.ShowTooltipsAndHighlight; set => _HandheldClickModule.ShowTooltipsAndHighlight = value; }
        #endregion
    }
}