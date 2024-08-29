using System;
using UnityEngine;
using UnityEngine.Events;

namespace ViRSE.PluginRuntime.VComponents
{
    [Serializable]
    public class RangedInteractionConfig
    {
        //[VerticalGroup("RangedInteractionModule_VGroup")]
        //[FoldoutGroup("RangedInteractionModule_VGroup/Ranged Interaction Settings")]
        //[SuffixLabel("metres")]
        [BeginGroup("Ranged Interaction Settings", Style = GroupStyle.Round)]
        [SerializeField] public float InteractionRange = 5;

        //[PropertySpace(SpaceBefore = 5)]
        //[FoldoutGroup("RangedInteractionModule_VGroup/Ranged Interaction Settings")]
        [SerializeField] public UnityEvent OnLocalHoverEnter;

        //[FoldoutGroup("RangedInteractionModule_VGroup/Ranged Interaction Settings")]
        [EndGroup]
        [SerializeField] public UnityEvent OnLocalHoverExit;
    }

    public class RangedInteractionModule : IRangedPlayerInteractable, IRangedInteractionModule
    {
        #region Plugin and Player Rig Interfaces
        public float InteractRange { get => _config.InteractionRange; set => _config.InteractionRange = value; }
        #endregion

        private RangedInteractionConfig _config;
        private GeneralInteractionModule generalInteractionModule;

        public RangedInteractionModule(RangedInteractionConfig config)
        {
            _config = config;
        }

        public void OnLocalInteractorHoverEnter()
        {

        }

        public void OnLocalInteractorHoverExit()
        {

        }
    }
}
