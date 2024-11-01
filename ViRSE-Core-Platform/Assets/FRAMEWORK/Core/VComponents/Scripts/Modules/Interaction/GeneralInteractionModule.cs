using System;
using UnityEngine;
using UnityEngine.Events;
using ViRSE.Core.VComponents.InternalInterfaces;


namespace ViRSE.Core.VComponents
{
    [Serializable]
    internal class GeneralInteractionConfig
    {
        [BeginGroup(Style = GroupStyle.Round)]
        [Title("General Interation Settings")]
        [SerializeField] public bool AdminOnly = false;

        [SerializeField] public bool EnableControllerVibrations = true;

        [EndGroup]
        [SerializeField] public bool ShowTooltipsAndHighlight = true;
    }

    internal abstract class GeneralInteractionModule : IGeneralInteractionModule
    {
        public bool AdminOnly { get { return Config.AdminOnly; } set { UpdateAdminOnly(value); } }
        public bool VibrateControllers => Config.EnableControllerVibrations;
        public bool ShowTooltips => Config.ShowTooltipsAndHighlight;

        public event Action OnBecomeAdminOnly; //E.G if grabbable, the VC needs to know to force drop
        public readonly GeneralInteractionConfig Config;

        public GeneralInteractionModule(GeneralInteractionConfig config)
        {
            Config = config;
        }

        private void UpdateAdminOnly(bool newAdminOnly)
        {
            bool oldAdminOnly = Config.AdminOnly;
            Config.AdminOnly = newAdminOnly;

            if (newAdminOnly && !oldAdminOnly)
                OnBecomeAdminOnly.Invoke();
        }
    }
}