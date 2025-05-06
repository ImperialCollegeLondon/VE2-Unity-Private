using UnityEngine.Events;
using VE2.Common.Shared;

namespace VE2.Core.VComponents.API
{
    public interface IV_ToggleActivatable 
    {
        #region State Module Interface
        internal ISingleInteractorActivatableStateModule _StateModule { get; }

        public UnityEvent OnActivate => _StateModule.OnActivate;
        public UnityEvent OnDeactivate => _StateModule.OnDeactivate;

        public bool IsActivated  => _StateModule.IsActivated;
        public void Activate() => _StateModule.Activate();
        public void Deactivate() => _StateModule.Deactivate();
        public void SetActivated(bool isActivated) => _StateModule.SetActivated(isActivated);
        public IClientIDWrapper MostRecentInteractingClientID => _StateModule.MostRecentInteractingClientID;

        public void SetNetworked(bool isNetworked) => _StateModule.SetNetworked(isNetworked);
        #endregion

        #region Ranged Interaction Module Interface
        internal IRangedToggleClickInteractionModule _RangedToggleClickModule { get; }
        public float InteractRange { get => _RangedToggleClickModule.InteractRange; set => _RangedToggleClickModule.InteractRange = value; }
        #endregion

        #region General Interaction Module Interface
        //We have two General Interaction Modules here, it doesn't matter which one we point to, both share the same General Interaction Config object!
        public bool AdminOnly {get => _RangedToggleClickModule.AdminOnly; set => _RangedToggleClickModule.AdminOnly = value; }
        public bool EnableControllerVibrations { get => _RangedToggleClickModule.EnableControllerVibrations; set => _RangedToggleClickModule.EnableControllerVibrations = value; }
        public bool ShowTooltipsAndHighlight { get => _RangedToggleClickModule.ShowTooltipsAndHighlight; set => _RangedToggleClickModule.ShowTooltipsAndHighlight = value; }
        #endregion
    }
}