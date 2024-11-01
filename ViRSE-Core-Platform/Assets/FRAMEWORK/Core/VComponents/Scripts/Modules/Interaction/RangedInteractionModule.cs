using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using ViRSE.Core.VComponents.InternalInterfaces;

namespace ViRSE.Core.VComponents
{
    [Serializable]
    internal class RangedInteractionConfig
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

    internal class RangedInteractionModule : GeneralInteractionModule, IRangedInteractionModule
    {
        public float InteractRange { get => _rangedConfig.InteractionRange; set => _rangedConfig.InteractionRange = value; }

        private readonly RangedInteractionConfig _rangedConfig;

        public RangedInteractionModule(RangedInteractionConfig config, GeneralInteractionConfig generalInteractionConfig) : base(generalInteractionConfig)
        {
            _rangedConfig = config;
        }

        public void OnLocalInteractorHoverEnter()
        {

        }

        public void OnLocalInteractorHoverExit()
        {

        }
    }
}
