using System;
using UnityEngine;
using VE2.Core.VComponents.API;

namespace VE2.Core.VComponents.Internal
{
    [Serializable]
    public class GeneralInteractionConfig
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
        public bool AdminOnly { get { return _config.AdminOnly; } set { _config.AdminOnly = value; } }
        public bool EnableControllerVibrations { get => _config.EnableControllerVibrations; set => _config.EnableControllerVibrations = value; }
        public bool ShowTooltipsAndHighlight { get => _config.ShowTooltipsAndHighlight; set => _config.ShowTooltipsAndHighlight = value; }
        private readonly GeneralInteractionConfig _config;

        public GeneralInteractionModule(GeneralInteractionConfig config)
        {
            _config = config;
        }
    }
}