using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using VE2.Common.Shared;
using VE2.Core.VComponents.API;

namespace VE2.Core.VComponents.Internal
{
    [Serializable]
    internal class RangedInteractionConfig
    {
        [BeginGroup(Style = GroupStyle.Round)]
        [Space(5)]
        [Title("Ranged Interaction Settings")]
        [SerializeField, PropertyOrder(0)] public float InteractionRange = 50;

        [Space(5)]
        [SerializeField, PropertyOrder(1)] public UnityEvent OnLocalHoverEnter = new();

        [EndGroup]
        [SerializeField, PropertyOrder(2)] public UnityEvent OnLocalHoverExit = new();
    }

    internal class RangedInteractionModule : GeneralInteractionModule, IRangedInteractionModule
    {
        public float InteractRange { get => _rangedConfig.InteractionRange; set => _rangedConfig.InteractionRange = value; }

        private readonly RangedInteractionConfig _rangedConfig;
        private List<InteractorID> _hoveringInteractors = new();

        public RangedInteractionModule(RangedInteractionConfig config, GeneralInteractionConfig generalInteractionConfig) : base(generalInteractionConfig)
        {
            _rangedConfig = config;
        }

        public void EnterHover(InteractorID interactorID)
        {
            if (_hoveringInteractors.Contains(interactorID))
                return;

            _hoveringInteractors.Add(interactorID);

            if (_hoveringInteractors.Count > 0)
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
            if(_hoveringInteractors.Contains(interactorID))
                _hoveringInteractors.Remove(interactorID);

            if (_hoveringInteractors.Count == 0)
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
