using System;
using System.Collections.Generic;
using UnityEngine;
using VE2.Common;
using VE2.NonCore.Instancing.VComponents.NonInteractableInterfaces;
using static VE2.Common.CommonSerializables;

namespace VE2.NonCore.Instancing.VComponents.Internal
{

    public class RigidbodySyncableService
    {
        #region Interfaces
        public IRigidbodySyncableStateModule StateModule => _StateModule;
        #endregion

        #region Modules
        private readonly RigidbodySyncableStateModule _StateModule;
        #endregion

        private IRigidbodyWrapper _rigidbody;
        private bool _isKinematicOnStart;
        private List<(uint, RigidbodySyncableState)> _rbStates;

        public RigidbodySyncableService(RigidbodySyncableStateConfig config, VE2Serializable state, string id, WorldStateModulesContainer worldStateModulesContainer, RigidbodyWrapper rigidbodyWrapper)
        {
            _StateModule = new(state, config, id, worldStateModulesContainer);
            _rigidbody = rigidbodyWrapper;
            _isKinematicOnStart = _rigidbody.isKinematic;
            _rbStates = new(); // TODO: store received states in a list, including a pseudo arrival-time, for interpolation by non-host
        }

        public void HandleFixedUpdate()
        {
            _StateModule.HandleFixedUpdate();
        }

        public void TearDown()
        {
            _StateModule.TearDown();
        }
    }
}
