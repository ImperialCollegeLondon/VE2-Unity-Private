using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using VE2.Core.VComponents.API;

namespace VE2.Core.VComponents.Internal
{
    [Serializable]
    internal class RangedInteractionConfig
    {
        [Title("Ranged Interation Settings")]
        [Space(5)]
        [BeginGroup(Style = GroupStyle.Round), SerializeField] public float InteractionRange = 50;

        [Space(5)]
        [SerializeField] public UnityEvent OnLocalHoverEnter = new();

        [EndGroup, SerializeField] public UnityEvent OnLocalHoverExit = new();
    }

    internal class RangedInteractionModule : GeneralInteractionModule, IRangedInteractionModule
    {
        public float InteractRange { get => _rangedConfig.InteractionRange; set => _rangedConfig.InteractionRange = value; }

        private readonly RangedInteractionConfig _rangedConfig;
        private List<InteractorID> hoveringInteractors = new();

        public RangedInteractionModule(RangedInteractionConfig config, GeneralInteractionConfig generalInteractionConfig) : base(generalInteractionConfig)
        {
            _rangedConfig = config;
        }

        public void EnterHover(InteractorID interactorID)
        {
            if (hoveringInteractors.Contains(interactorID))
                return;

            hoveringInteractors.Add(interactorID);

            if (hoveringInteractors.Count > 0)
            {
                try
                {
                    _rangedConfig.OnLocalHoverEnter?.Invoke();
                }
                catch (Exception e)
                {
                    Debug.LogError($"ERROR HOVER INVOKE config null?- {_rangedConfig == null}");
                    Debug.LogError($"Error invoking OnHoverEnter event - {e.Message} - {e.StackTrace}");
                }
            }

        }

        public void ExitHover(InteractorID interactorID)
        {
            if(hoveringInteractors.Contains(interactorID))
                hoveringInteractors.Remove(interactorID);

            if (hoveringInteractors.Count == 0)
            {
                try
                {
                    _rangedConfig.OnLocalHoverExit?.Invoke();
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error invoking OnHoverExit event - {e.Message} - {e.StackTrace}");
                }
            }
        }
    }
}
