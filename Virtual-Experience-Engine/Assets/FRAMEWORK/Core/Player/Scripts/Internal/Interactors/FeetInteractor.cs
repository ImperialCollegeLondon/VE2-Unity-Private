using System.Collections.Generic;
using UnityEngine;
using VE2.Core.Player.API;
using VE2.Core.VComponents.API;
namespace VE2.Core.Player.Internal
{
    internal class FeetInteractor
    {
        public List<string> HeldActivatableIDs => _heldActivatableIDs;

        protected List<string> _heldActivatableIDs = new();
        private ushort _localClientID => _localClientIDProvider == null ? (ushort)0 : _localClientIDProvider.LocalClientID;
        protected InteractorID _InteractorID => new(_localClientID, _InteractorType);
        protected bool _WaitingForLocalClientID => _localClientIDProvider != null && !_localClientIDProvider.IsClientIDReady;

        private readonly V_CollisionDetector _collisionDetector;
        private readonly InteractorType _InteractorType;
        private readonly ILocalClientIDProvider _localClientIDProvider;

        internal FeetInteractor(V_CollisionDetector collisionDetector, InteractorType interactorType, ILocalClientIDProvider localClientIDProvider)
        {
            _collisionDetector = collisionDetector;
            _InteractorType = interactorType;
            _localClientIDProvider = localClientIDProvider;
        }

        public virtual void HandleOnEnable()
        {
            _collisionDetector.OnCollideStart += HandleCollideStart;
            _collisionDetector.OnCollideEnd += HandleCollideEnd;

            _heldActivatableIDs = new();

            if (_WaitingForLocalClientID)
                _localClientIDProvider.OnClientIDReady += HandleLocalClientIDReady;
            else
                HandleLocalClientIDReady(_localClientID);
        }

        public virtual void HandleOnDisable()
        {
            _collisionDetector.OnCollideStart -= HandleCollideStart;
            _collisionDetector.OnCollideEnd -= HandleCollideEnd;

            _heldActivatableIDs = new();

            if (_localClientIDProvider != null)
                _localClientIDProvider.OnClientIDReady -= HandleLocalClientIDReady;
        }

        private void HandleLocalClientIDReady(ushort clientID)
        {
            if (_localClientIDProvider != null)
                _localClientIDProvider.OnClientIDReady -= HandleLocalClientIDReady;
        }

        private void HandleCollideStart(ICollideInteractionModule collideInteractionModule)
        {
            if (!_WaitingForLocalClientID && !collideInteractionModule.AdminOnly && collideInteractionModule.CollideInteractionType == CollideInteractionType.Feet)
            {
                collideInteractionModule.InvokeOnCollideEnter(_InteractorID);
                _heldActivatableIDs.Add(collideInteractionModule.ID);
            }

        }

        private void HandleCollideEnd(ICollideInteractionModule collideInteractionModule)
        {
            if (!_WaitingForLocalClientID && !collideInteractionModule.AdminOnly && collideInteractionModule.CollideInteractionType == CollideInteractionType.Feet)
            {
                collideInteractionModule.InvokeOnCollideExit(_InteractorID);
                _heldActivatableIDs.Remove(collideInteractionModule.ID);
            }
        }
    }
}
