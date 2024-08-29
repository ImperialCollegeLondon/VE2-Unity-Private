using System;
using UnityEngine;
using UnityEngine.Events;


namespace ViRSE.PluginRuntime.VComponents
{
    [Serializable]
    public class GeneralInteractionConfig
    {
        [BeginGroup("General Interaction Settings", Style = GroupStyle.Round)]
        [SerializeField] public bool AdminOnly = false;

        [SerializeField] public bool EnableControllerVibrations = true;

        [EndGroup]
        [SerializeField] public bool ShowTooltipsAndHighlight = true;
    }

    public class GeneralInteractionModule : IGeneralInteractionModule, IGeneralPlayerInteractable
    {
        #region Plugin Interfaces
        public bool AdminOnly { get { return Config.AdminOnly; } set { ReceiveNewAdminOnlyFromPlugin(value); } }
        #endregion

        public GeneralInteractionConfig Config { get; set; }
        public UnityEvent OnBecomeAdminOnly { get; private set; } = new(); //E.G if grabbable, the VC needs to know to force drop

        public InteractorID CurrentInteractor { get; set; }

        #region Player rig-facing Interfaces
        public bool VibrateControllers => Config.AdminOnly;

        public bool ShowTooltips =>Config.ShowTooltipsAndHighlight;
        #endregion

        public GeneralInteractionModule(GeneralInteractionConfig config)
        {
            Config = config;
        }

        private void ReceiveNewAdminOnlyFromPlugin(bool newAdminOnly)
        {
            bool oldAdminOnly = Config.AdminOnly;
            Config.AdminOnly = newAdminOnly;

            if (newAdminOnly && !oldAdminOnly)
            {
                OnBecomeAdminOnly.Invoke();
            }
        }
    }
}