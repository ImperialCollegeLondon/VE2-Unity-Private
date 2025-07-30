using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using VE2.Common.API;
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
        [SerializeField, PropertyOrder(3)] public bool EnableOutline = false;
        [SerializeField, PropertyOrder(4), ShowIf(nameof(EnableOutline), true)] public float OutlineThickness = 2.5f;
        [SerializeField, PropertyOrder(5), ShowIf(nameof(EnableOutline), true)] public Color DefaultOutlineColor = Color.white;
        [SerializeField, PropertyOrder(6), ShowIf(nameof(EnableOutline), true)] public Color InteractedOutlineColor = new Color(1f, 0.5f, 0f, 1f);
        [EndGroup]
        [SerializeField, PropertyOrder(7), ShowIf(nameof(EnableOutline), true)] public Color HoveredOutlineColor = Color.yellow;
    }

    internal class RangedInteractionModule : GeneralInteractionModule, IRangedInteractionModule
    {
        public float InteractRange { get => _rangedConfig.InteractionRange; set => _rangedConfig.InteractionRange = value; }

        protected bool _isAllowedToInteract => IsInteractable && (!AdminOnly || VE2API.LocalAdminIndicator.IsLocalAdmin);
        private readonly RangedInteractionConfig _rangedConfig;
        public IInteractableOutline _interactableOutline { get; private set; }
        private List<InteractorID> _hoveringInteractors = new();

        internal event Action OnLocalInteractorEnterHover;
        internal event Action OnLocalInteractorExitHover;

        public RangedInteractionModule(RangedInteractionConfig config, IInteractableOutline interactableOutline, GeneralInteractionConfig generalInteractionConfig) : base(generalInteractionConfig)
        {
            _rangedConfig = config;
            _interactableOutline = interactableOutline;

            VE2API.LocalAdminIndicator.OnLocalAdminStatusChanged += OnLocalAdminStatusChanged; //to change our outline color on admin status change

            if (_interactableOutline != null)
            {
                _interactableOutline.OutlineWidth = _rangedConfig.OutlineThickness;

                if (_isAllowedToInteract)
                    SetOutlineColor(_rangedConfig.DefaultOutlineColor);
                else
                    SetOutlineColor(Color.red);
            }
        }

        public void EnterHover(InteractorID interactorID)
        {
            if (_hoveringInteractors.Contains(interactorID))
                return;

            _hoveringInteractors.Add(interactorID);

            //Debug.Log($"Number of hovering interactors: {_hoveringInteractors.Count}");

            if (_hoveringInteractors.Count > 0)
            {
                try
                {
                    _rangedConfig.OnLocalHoverEnter?.Invoke();
                    OnLocalInteractorEnterHover?.Invoke();

                    HandleOultineOnHoverEnter(true);
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

            //Debug.Log($"Number of hovering interactors: {_hoveringInteractors.Count}");

            if (_hoveringInteractors.Count == 0)
            {
                try
                {
                    _rangedConfig.OnLocalHoverExit?.Invoke();
                    OnLocalInteractorExitHover?.Invoke();

                    HandleOultineOnHoverEnter(false);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error invoking OnHoverExit event - {e.Message} - {e.StackTrace}");
                }
            }
        }

        public void OnInteractedWith(bool isInteracted)
        {
            if (_interactableOutline == null)
                return;

            if (!_isAllowedToInteract)
            {
                SetOutlineColor(Color.red);
                return;
            }

            if (isInteracted)
                SetOutlineColor(_rangedConfig.InteractedOutlineColor);
            else
                SetOutlineColor(_rangedConfig.HoveredOutlineColor);
        }

        public virtual void HandleOultineOnHoverEnter(bool ishovering)
        {
            if (_interactableOutline == null)
                return;

            if (!_isAllowedToInteract)
            {
                SetOutlineColor(Color.red);
                return;
            }

            if (ishovering)
                SetOutlineColor(_rangedConfig.HoveredOutlineColor);
            else
                SetOutlineColor(_rangedConfig.DefaultOutlineColor);
        }

        private void OnLocalAdminStatusChanged(bool isLocalAdmin)
        {
            if (_interactableOutline == null)
                return;

            //Debug.Log($"OnLocalAdminStatusChanged - isLocalAdmin: {isLocalAdmin}");

            if (_isAllowedToInteract)
                SetOutlineColor(_rangedConfig.DefaultOutlineColor);
            else
                SetOutlineColor(Color.red);

        }

        public void SetOutlineColor(Color color) => _interactableOutline.OutlineColor = color;

    }
}
