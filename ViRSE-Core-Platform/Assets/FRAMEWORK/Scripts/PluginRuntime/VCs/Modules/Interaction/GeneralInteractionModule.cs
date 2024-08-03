using Sirenix.OdinInspector;
using System;
using UnityEngine;
using UnityEngine.Events;


namespace ViRSE.PluginRuntime.VComponents
{
    [Serializable]
    public class GeneralInteractionConfig
    {
        [FoldoutGroup("GeneralInteractionModule_VGroup/General Interaction Settings")]
        [SerializeField] public bool AdminOnly = false;

        [VerticalGroup("GeneralInteractionModule_VGroup")]
        [FoldoutGroup("GeneralInteractionModule_VGroup/General Interaction Settings")]
        [SerializeField] public bool EnableControllerVibrations = true;

        [FoldoutGroup("GeneralInteractionModule_VGroup/General Interaction Settings")]
        [SerializeField] public bool ShowTooltipsAndHighlight = true;
    }

    public class GeneralInteractionModule : IGeneralInteractionModule
    {
        #region Plugin Interfaces
        public bool AdminOnly { get { return Config.AdminOnly; } set { ReceiveNewAdminOnlyFromPlugin(value); } }
        #endregion

        public GeneralInteractionConfig Config { get; set; }
        public UnityEvent OnBecomeAdminOnly { get; private set; } = new(); //E.G if grabbable, the VC needs to know to force drop

        public InteractorID CurrentInteractor { get; set; }

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