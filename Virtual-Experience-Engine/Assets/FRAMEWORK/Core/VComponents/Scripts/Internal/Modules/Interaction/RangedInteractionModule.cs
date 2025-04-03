using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using VE2.Core.VComponents.API;

namespace VE2.Core.VComponents.Internal
{
    [Serializable]
    public class RangedInteractionConfig
    {
        [BeginGroup(Style = GroupStyle.Round)]
        [Space(5)]
        [Title("Ranged Interation Settings")]
        [SerializeField] public float InteractionRange = 5;

        [Space(5)]
        [SerializeField] public UnityEvent OnLocalHoverEnter;

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

        public void EnterHover()
        {
            try
            {
                _rangedConfig.OnLocalHoverEnter.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogError($"Error invoking OnHoverEnter event - {e.Message} - {e.StackTrace}");
            }
        }

        public void ExitHover()
        {
            try
            {
                _rangedConfig.OnLocalHoverExit.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogError($"Error invoking OnHoverExit event - {e.Message} - {e.StackTrace}");
            }
        }
    }
}
