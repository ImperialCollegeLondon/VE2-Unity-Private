using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VE2.Common.API;
using VE2.Common.Shared;
using VE2.Core.VComponents.API;

namespace VE2.Core.Player.Internal
{
    internal class FeetInteractor
    {
        public IReadOnlyList<string> HeldNetworkedActivatableIDs => _heldActivatableIDsAgainstNetworkFlags.Where(kvp => kvp.Value).Select(kvp => kvp.Key).ToList();


        private readonly Dictionary<string, bool> _heldActivatableIDsAgainstNetworkFlags = new();
        private InteractorID _interactorID => _localClientIDWrapper.IsClientIDReady ? new InteractorID(_localClientIDWrapper.Value, _InteractorType) : null;

        public readonly ICollisionDetector _collisionDetector;
        private readonly InteractorType _InteractorType;
        private readonly ILocalClientIDWrapper _localClientIDWrapper;
        private readonly ILocalAdminIndicator _localAdminIndicator;

        internal FeetInteractor(ICollisionDetectorFactory collisionDetectorFactory, ColliderType colliderType, Collider collider, InteractorType interactorType, 
            ILocalClientIDWrapper localClientIDWrapper, ILocalAdminIndicator localAdminIndicator, PlayerInteractionConfig interactionConfig)
        {
            _collisionDetector = collisionDetectorFactory.CreateCollisionDetector(collider, colliderType, interactionConfig.InteractableLayers);
            _InteractorType = interactorType;
            _localClientIDWrapper = localClientIDWrapper;
            _localAdminIndicator = localAdminIndicator;
        }
        protected bool IsInteractableAllowed(IGeneralInteractionModule interactable)
        {
            return interactable != null && (!interactable.AdminOnly || _localAdminIndicator.IsLocalAdmin);
        }
        public virtual void HandleOnEnable()
        {
            _collisionDetector.OnCollideStart += HandleCollideStart;
            _collisionDetector.OnCollideEnd += HandleCollideEnd;

            _heldActivatableIDsAgainstNetworkFlags.Clear();

            if (!_localClientIDWrapper.IsClientIDReady)
                _localClientIDWrapper.OnClientIDReady += HandleLocalClientIDReady;
            else
                HandleLocalClientIDReady(_localClientIDWrapper.Value);
        }

        public virtual void HandleOnDisable()
        {
            _collisionDetector.OnCollideStart -= HandleCollideStart;
            _collisionDetector.OnCollideEnd -= HandleCollideEnd;

            _heldActivatableIDsAgainstNetworkFlags.Clear();

            _localClientIDWrapper.OnClientIDReady -= HandleLocalClientIDReady;
        }

        private void HandleLocalClientIDReady(ushort clientID)
        {
            _localClientIDWrapper.OnClientIDReady -= HandleLocalClientIDReady;
        }

        private void HandleCollideStart(ICollideInteractionModule collideInteractionModule)
        {
            if (_localClientIDWrapper.IsClientIDReady &&
                IsInteractableAllowed(collideInteractionModule) &&
                collideInteractionModule.CollideInteractionType == CollideInteractionType.Feet)
            {
                collideInteractionModule.InvokeOnCollideEnter(_interactorID);

                if (collideInteractionModule.IsNetworked)
                    _heldActivatableIDsAgainstNetworkFlags.Add(collideInteractionModule.ID, collideInteractionModule.IsNetworked);
            }
        }

        private void HandleCollideEnd(ICollideInteractionModule collideInteractionModule)
        {
            if (_localClientIDWrapper.IsClientIDReady &&
                IsInteractableAllowed(collideInteractionModule) &&
                collideInteractionModule.CollideInteractionType == CollideInteractionType.Feet)
            {
                collideInteractionModule.InvokeOnCollideExit(_interactorID);

                if (collideInteractionModule.IsNetworked)
                    _heldActivatableIDsAgainstNetworkFlags.Remove(collideInteractionModule.ID);
            }
        }
    }
}
