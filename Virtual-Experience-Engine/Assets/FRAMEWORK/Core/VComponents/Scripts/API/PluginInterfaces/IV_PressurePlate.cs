using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace VE2.Core.VComponents.API
{
    public interface IV_PressurePlate
    {
        #region State Module Interface
        internal IMultiInteractorActivatableStateModule _StateModule { get; }

        public UnityEvent OnActivate => _StateModule.OnActivate;
        public UnityEvent OnDeactivate => _StateModule.OnDeactivate;

        public bool IsActivated { get { return _StateModule.IsActivated; } }
        public ushort MostRecentInteractingClientID => _StateModule.MostRecentInteractingClientID;
        public List<ushort> CurrentlyInteractingClientIDs => _StateModule.CurrentlyInteractingClientIDs;
        #endregion

        #region Ranged Interaction Module Interface
        internal ICollideInteractionModule _ColliderModule { get; }
        #endregion

        #region General Interaction Module Interface
        //We have two General Interaction Modules here, it doesn't matter which one we point to, both share the same General Interaction Config object!
        public bool AdminOnly { get => _ColliderModule.AdminOnly; set => _ColliderModule.AdminOnly = value; }
        public bool EnableControllerVibrations { get => _ColliderModule.EnableControllerVibrations; set => _ColliderModule.EnableControllerVibrations = value; }
        public bool ShowTooltipsAndHighlight { get => _ColliderModule.ShowTooltipsAndHighlight; set => _ColliderModule.ShowTooltipsAndHighlight = value; }
        #endregion
    }
}
