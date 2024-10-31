using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

namespace ViRSE.Core.VComponents
{
    [Serializable]
    public class RangedInteractionConfig
    {
        //[VerticalGroup("RangedInteractionModule_VGroup")]
        //[FoldoutGroup("RangedInteractionModule_VGroup/Ranged Interaction Settings")]
        //[SuffixLabel("metres")]
        [BeginGroup(Style = GroupStyle.Round)]
        [Space(5)]
        [Title("Ranged Interation Settings")]
        [SerializeField] public float InteractionRange = 5;

        //[PropertySpace(SpaceBefore = 5)]
        //[FoldoutGroup("RangedInteractionModule_VGroup/Ranged Interaction Settings")]
        [Space(5)]
        [SerializeField] public UnityEvent OnLocalHoverEnter;

        //[FoldoutGroup("RangedInteractionModule_VGroup/Ranged Interaction Settings")]
        [EndGroup]
        [SerializeField] public UnityEvent OnLocalHoverExit;
    }

    public class RangedInteractionModule : IRangedPlayerInteractable, IRangedInteractionModule
    {
        #region Plugin and Player Rig Interfaces
        public float InteractRange { get => _rangedConfig.InteractionRange; set => _rangedConfig.InteractionRange = value; }

        public bool AdminOnly => _generalConfig.AdminOnly;
        public bool VibrateControllers => _generalConfig.EnableControllerVibrations;
        public bool ShowTooltips => _generalConfig.ShowTooltipsAndHighlight;
        #endregion

        private RangedInteractionConfig _rangedConfig;
        private GeneralInteractionConfig _generalConfig;

        public RangedInteractionModule(RangedInteractionConfig config, GeneralInteractionConfig generalInteractionConfig)
        {
            _rangedConfig = config;
            _generalConfig = generalInteractionConfig;
        }

        public void OnLocalInteractorHoverEnter()
        {

        }

        public void OnLocalInteractorHoverExit()
        {

        }
    }
}
