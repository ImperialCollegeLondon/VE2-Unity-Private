using UnityEngine.Events;
using VE2.Common.Shared;

namespace VE2.Core.VComponents.API
{
    public interface IV_ToggleActivatable 
    {
        #region State Module Interface
        public UnityEvent OnActivate { get; }
        public UnityEvent OnDeactivate { get; }

        public bool IsActivated { get; }
        public void Activate();
        public void Deactivate();
        public void SetActivated(bool isActivated);
        public IClientIDWrapper MostRecentInteractingClientID { get; }

        public void SetNetworked(bool isNetworked);
        #endregion

        #region Ranged Interaction Module Interface
        public float InteractRange { get; set; }
        #endregion

        #region General Interaction Module Interface
        //We have two General Interaction Modules here, it doesn't matter which one we point to, both share the same General Interaction Config object!
        public bool AdminOnly { get; set; }
        public bool EnableControllerVibrations { get; set; }
        public bool ShowTooltipsAndHighlight { get; set; }
        #endregion
    }
}