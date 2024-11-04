using UnityEngine.Events;
using VE2.Core.VComponents.NonInteractableInterfaces;
using VE2.Core.VComponents.InteractableInterfaces;

namespace VE2.Core.VComponents.PluginInterfaces
{
    public interface IV_ToggleActivatable 
    {
        #region State Module Interface
        protected ISingleInteractorActivatableStateModule _StateModule{ get; }

        public UnityEvent OnActivate => _StateModule.OnActivate;
        public UnityEvent OnDeactivate => _StateModule.OnDeactivate;

        public bool IsActivated { get { return _StateModule.IsActivated; } set { _StateModule.IsActivated = value; } }
        public ushort MostRecentInteractingClientID => _StateModule.MostRecentInteractingClientID;
        #endregion

        #region Ranged Interaction Module Interface
        protected IRangedClickInteractionModule _RangedClickModule{ get; }
        public float InteractRange { get => _RangedClickModule.InteractRange; set => _RangedClickModule.InteractRange = value; }
        #endregion

        #region General Interaction Module Interface
        //We have two General Interaction Modules here, it doesn't matter which one we point to, both share the same General Interaction Config object!
        public bool AdminOnly {get => _RangedClickModule.AdminOnly; set => _RangedClickModule.AdminOnly = value; }
        public bool EnableControllerVibrations { get => _RangedClickModule.EnableControllerVibrations; set => _RangedClickModule.EnableControllerVibrations = value; }
        public bool ShowTooltipsAndHighlight { get => _RangedClickModule.ShowTooltipsAndHighlight; set => _RangedClickModule.ShowTooltipsAndHighlight = value; }
        #endregion
    }
}