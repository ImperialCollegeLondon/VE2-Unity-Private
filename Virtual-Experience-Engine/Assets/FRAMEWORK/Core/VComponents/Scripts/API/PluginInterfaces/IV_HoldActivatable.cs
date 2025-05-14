using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using VE2.Common.Shared;

namespace VE2.Core.VComponents.API
{
    public interface IV_HoldActivatable
    {
        #region State Module Interface
        internal IMultiInteractorActivatableStateModule _StateModule { get; }

        public UnityEvent OnActivate => _StateModule.OnActivate;
        public UnityEvent OnDeactivate => _StateModule.OnDeactivate;

        public bool IsActivated { get { return _StateModule.IsActivated; } }
        public IClientIDWrapper MostRecentInteractingClientID => _StateModule.MostRecentInteractingClientID;
        public List<IClientIDWrapper> CurrentlyInteractingClientIDs => _StateModule.CurrentlyInteractingClientIDs;
        #endregion

        #region Ranged Interaction Module Interface
        internal IRangedHoldClickInteractionModule _RangedHoldClickModule { get; }
        public float InteractRange { get => _RangedHoldClickModule.InteractRange; set => _RangedHoldClickModule.InteractRange = value; }
        #endregion

        #region General Interaction Module Interface
        //We have two General Interaction Modules here, it doesn't matter which one we point to, both share the same General Interaction Config object!
        public bool AdminOnly { get => _RangedHoldClickModule.AdminOnly; set => _RangedHoldClickModule.AdminOnly = value; }
        public bool EnableControllerVibrations { get => _RangedHoldClickModule.EnableControllerVibrations; set => _RangedHoldClickModule.EnableControllerVibrations = value; }
        public bool ShowTooltipsAndHighlight { get => _RangedHoldClickModule.ShowTooltipsAndHighlight; set => _RangedHoldClickModule.ShowTooltipsAndHighlight = value; }
        #endregion
    }
}