using System.Collections.Generic;
using UnityEngine;
using VE2.Common.Shared;
using VE2.Core.VComponents.API;

namespace VE2.Core.Player.Internal
{
    internal class FeetInteractor
    {
        public List<string> HeldActivatableIDs => _heldActivatableIDs;

        private List<string> _heldActivatableIDs = new();
        private InteractorID _interactorID => _localClientIDWrapper.IsClientIDReady ? new InteractorID(_localClientIDWrapper.Value, _InteractorType) : null;

        public ICollisionDetector _collisionDetector;
        private readonly InteractorType _InteractorType;
        private readonly ILocalClientIDWrapper _localClientIDWrapper;

        internal FeetInteractor(ICollisionDetectorFactory collisionDetectorFactory, ColliderType colliderType, Collider collider, InteractorType interactorType, 
            ILocalClientIDWrapper localClientIDWrapper, PlayerInteractionConfig interactionConfig)
        {
            _collisionDetector = collisionDetectorFactory.CreateCollisionDetector(collider, colliderType, interactionConfig.InteractableLayers);
            _InteractorType = interactorType;
            _localClientIDWrapper = localClientIDWrapper;
        }

        public virtual void HandleOnEnable()
        {
            _collisionDetector.OnCollideStart += HandleCollideStart;
            _collisionDetector.OnCollideEnd += HandleCollideEnd;

            _heldActivatableIDs = new();

            if (!_localClientIDWrapper.IsClientIDReady)
                _localClientIDWrapper.OnClientIDReady += HandleLocalClientIDReady;
            else
                HandleLocalClientIDReady(_localClientIDWrapper.Value);
        }

        public virtual void HandleOnDisable()
        {
            _collisionDetector.OnCollideStart -= HandleCollideStart;
            _collisionDetector.OnCollideEnd -= HandleCollideEnd;

            _heldActivatableIDs = new();

            _localClientIDWrapper.OnClientIDReady -= HandleLocalClientIDReady;
        }

        private void HandleLocalClientIDReady(ushort clientID)
        {
            _localClientIDWrapper.OnClientIDReady -= HandleLocalClientIDReady;
        }

        private void HandleCollideStart(ICollideInteractionModule collideInteractionModule)
        {
            if (_localClientIDWrapper.IsClientIDReady && !collideInteractionModule.AdminOnly && collideInteractionModule.CollideInteractionType == CollideInteractionType.Feet)
            {
                collideInteractionModule.InvokeOnCollideEnter(_interactorID);
                _heldActivatableIDs.Add(collideInteractionModule.ID);
            }
        }

        private void HandleCollideEnd(ICollideInteractionModule collideInteractionModule)
        {
            if (_localClientIDWrapper.IsClientIDReady && !collideInteractionModule.AdminOnly && collideInteractionModule.CollideInteractionType == CollideInteractionType.Feet)
            {
                collideInteractionModule.InvokeOnCollideExit(_interactorID);
                _heldActivatableIDs.Remove(collideInteractionModule.ID);
            }
        }
    }
}
