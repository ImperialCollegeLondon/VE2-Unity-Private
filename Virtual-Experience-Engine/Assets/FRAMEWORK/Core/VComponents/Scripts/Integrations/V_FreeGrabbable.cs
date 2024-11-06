using System;
using UnityEngine;
using VE2.Common;
using VE2.Core.VComponents.NonInteractableInterfaces;
using VE2.Core.VComponents.InteractableFindables;
using VE2.Core.VComponents.PluginInterfaces;
using VE2.Core.VComponents.InteractableInterfaces;
using VE2.Core.VComponents.Internal;

namespace VE2.Core.VComponents.Integration
{
    internal class V_FreeGrabbable : MonoBehaviour, IRangedGrabPlayerInteractableIntegrator //IV_ToggleActivatable, ICollidePlayerInteractableIntegrator
    {
        [SerializeField, HideLabel, IgnoreParent] private FreeGrabbableConfig _config = new();
        [SerializeField, HideInInspector] private FreeGrabbableState _state = new();

        #region Plugin Interfaces
        //ISingleInteractorActivatableStateModule IV_ToggleActivatable._StateModule => _freeGrabbable.StateModule;
        //IRangedClickInteractionModule IV_ToggleActivatable._RangedClickModule => _freeGrabbable.RangedClickInteractionModule;
        #endregion

        #region Player Interfaces
        //ICollideInteractionModule ICollidePlayerInteractableIntegrator._CollideInteractionModule => _freeGrabbable.ColliderInteractionModule;
        IRangedInteractionModule IRangedPlayerInteractableIntegrator.RangedInteractionModule => _freeGrabbable.RangedGrabInteractionModule;
        #endregion

        private FreeGrabbable _freeGrabbable = null;

        private void OnEnable()
        {
            string id = "FreeGrabbable-" + gameObject.name;
            _freeGrabbable = new FreeGrabbable(_config, _state, id, VE2CoreServiceLocator.Instance.WorldStateModulesContainer,new GameObjectFindProvider(), GetComponent<Rigidbody>());
        }

        private void FixedUpdate()
        {
            _freeGrabbable.HandleFixedUpdate();
        }

        private void OnDisable()
        {
            _freeGrabbable.TearDown();
            _freeGrabbable = null;
        }
    }
}