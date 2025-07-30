using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using VE2.Common.API;
using VE2.Common.Shared;
using VE2.Core.VComponents.API;

namespace VE2.Core.VComponents.Internal
{
    internal partial class V_HandheldActivatable : IV_HandheldActivatable
    {
        #region State Module Interface
        internal ISingleInteractorActivatableStateModule _StateModule => _Service.StateModule;

        public UnityEvent OnActivate => _StateModule.OnActivate;
        public UnityEvent OnDeactivate => _StateModule.OnDeactivate;

        public bool IsActivated  => _StateModule.IsActivated;
        public void Activate() => _StateModule.Activate();
        public void Deactivate() => _StateModule.Deactivate();
        public void SetActivated(bool isActivated) => _StateModule.SetActivated(isActivated);
        public IClientIDWrapper MostRecentInteractingClientID => _StateModule.MostRecentInteractingClientID;
        #endregion

        #region Handheld Interaction Module Interface
        internal IHandheldClickInteractionModule _HandheldClickModule => _Service.HandheldClickInteractionModule;
        #endregion

        #region General Interaction Module Interface
        public bool AdminOnly {get => _HandheldClickModule.AdminOnly; set => _HandheldClickModule.AdminOnly = value; }
        public bool EnableControllerVibrations { get => _HandheldClickModule.EnableControllerVibrations; set => _HandheldClickModule.EnableControllerVibrations = value; }
        public bool ShowTooltipsAndHighlight { get => _HandheldClickModule.ShowTooltipsAndHighlight; set => _HandheldClickModule.ShowTooltipsAndHighlight = value; }
        public bool IsInteractable { get => _HandheldClickModule.IsInteractable; set => _HandheldClickModule.IsInteractable = value; }
        #endregion
    }

    [RequireComponent(typeof(V_FreeGrabbable))]
    [DisallowMultipleComponent]
    internal partial class V_HandheldActivatable : MonoBehaviour
    {
        [SerializeField, HideLabel, IgnoreParent] private HandheldActivatableConfig _config = new();
        [SerializeField, HideInInspector] private SingleInteractorActivatableState _state = new();

        #region Player Interfaces
        internal IHandheldClickInteractionModule HandheldClickInteractionModule => _Service.HandheldClickInteractionModule;
        #endregion

        private HandheldActivatableService _service = null;
        private HandheldActivatableService _Service
        {
            get
            {
                if (_service == null)
                    OnEnable();
                return _service;
            }
        }

        private void OnEnable()
        {
            if (!Application.isPlaying || _service != null)
                return;

            IV_FreeGrabbable grabbable = null;

            if (TryGetComponent(out V_FreeGrabbable freeGrabbable))
                grabbable = freeGrabbable;

            string id = "HHActivatable-" + gameObject.name;
            _service = new(grabbable, _config, _state, id, VE2API.WorldStateSyncableContainer, VE2API.ActivatableGroupsContainer, VE2API.LocalClientIdWrapper);
        }

        private void Start() => _service?.HandleStart();
        private void FixedUpdate() => _service?.HandleFixedUpdate();

        private void OnDisable()
        {
            _service.TearDown(_isApplicationQuitting);
            _service = null;
        }

        private bool _isApplicationQuitting = false;
        private void OnApplicationQuit()
        {
            _isApplicationQuitting = true;
        }
    }
}

