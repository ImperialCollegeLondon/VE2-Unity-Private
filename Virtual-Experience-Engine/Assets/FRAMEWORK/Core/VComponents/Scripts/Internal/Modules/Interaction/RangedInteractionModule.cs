using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using VE2.Common.Shared;
using VE2.Core.VComponents.API;
using VE2.Core.VComponents.Shared;

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
        [SerializeField, PropertyOrder(2)] public UnityEvent OnLocalHoverExit = new();

        [Space(15)]
        [SerializeField, PropertyOrder(3)] public bool EnableOutline = true;
        [SerializeField, PropertyOrder(4), ShowIf(nameof(EnableOutline), true)] public float OutlineThickness = 2.5f;
        [SerializeField, PropertyOrder(5), ShowIf(nameof(EnableOutline), true)] public Color DefaultOutlineColor = Color.white;
        [SerializeField, PropertyOrder(6), ShowIf(nameof(EnableOutline), true)] public Color InteractedOutlineColor = new Color(1f, 0.5f, 0f, 1f);
        [EndGroup]
        [SerializeField, PropertyOrder(7), ShowIf(nameof(EnableOutline), true)] public Color HoveredOutlineColor = Color.yellow;
    }

    internal class RangedInteractionModule : GeneralInteractionModule, IRangedInteractionModule
    {
        public float InteractRange { get => _rangedConfig.InteractionRange; set => _rangedConfig.InteractionRange = value; }

        private readonly RangedInteractionConfig _rangedConfig;
        private IInteractableOutline _grabbableOutline;
        private List<InteractorID> _hoveringInteractors = new();

        internal event Action OnLocalInteractorEnterHover;
        internal event Action OnLocalInteractorExitHover;

        private bool _isCurrentlyInteractedWith = false;

        public RangedInteractionModule(RangedInteractionConfig config, IInteractableOutline interactableOutline, GeneralInteractionConfig generalInteractionConfig) : base(generalInteractionConfig)
        {
            _rangedConfig = config;
            _grabbableOutline = interactableOutline;
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
                    OnLocalInteractorEnterHover?.Invoke();

                    if (_grabbableOutline != null)
                    {
                        if (!_isCurrentlyInteractedWith)
                            _grabbableOutline.OutlineColor = _rangedConfig.HoveredOutlineColor;
                        else
                            _grabbableOutline.OutlineColor = _rangedConfig.DefaultOutlineColor;
                    }
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
            if (_hoveringInteractors.Contains(interactorID))
                _hoveringInteractors.Remove(interactorID);

            if (_hoveringInteractors.Count == 0)
            {
                try
                {
                    _rangedConfig.OnLocalHoverExit?.Invoke();
                    OnLocalInteractorExitHover?.Invoke();

                    if (_grabbableOutline != null)
                        _grabbableOutline.OutlineColor = _rangedConfig.DefaultOutlineColor;
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error invoking OnHoverExit event - {e.Message} - {e.StackTrace}");
                }
            }
        }
        
        public void HandleInteraction(bool isBeingInteractedWith)
        {
            _isCurrentlyInteractedWith = isBeingInteractedWith;

            if (_grabbableOutline == null)
                return;

            if (isBeingInteractedWith)
                _grabbableOutline.OutlineColor = _rangedConfig.InteractedOutlineColor;
            else
                _grabbableOutline.OutlineColor = _rangedConfig.HoveredOutlineColor;
        }
    }
}
