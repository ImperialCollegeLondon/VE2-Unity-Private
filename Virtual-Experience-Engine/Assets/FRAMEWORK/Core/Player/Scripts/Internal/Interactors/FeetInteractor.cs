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

        /// <summary>
        /// key is the interaction module, value is whether we are actually interacting with it 
        /// Used to handle changes to admin rights. If we're colliding but not interacting, and we find ourselves to be admin, we should start interacting 
        /// If we're colliding and we ARE interacting, but we find ourselves to be non-admin, we should stop interacting.
        /// This also applies to the case of gaining our local client ID after the collision starts
        /// </summary>
        private Dictionary<ICollideInteractionModule, bool> _currentCollidingInteractionModules = new();

        public readonly ICollisionDetector _collisionDetector;
        private readonly InteractorType _InteractorType;
        private readonly ILocalClientIDWrapper _localClientIDWrapper;
        private readonly ILocalAdminIndicator _localAdminIndicator;

        internal FeetInteractor(ICollisionDetectorFactory collisionDetectorFactory, ColliderType colliderType, Collider collider, InteractorType interactorType,
            ILocalClientIDWrapper localClientIDWrapper, ILocalAdminIndicator localAdminIndicator, PlayerInteractionConfig interactionConfig)
        {
            _collisionDetector = collisionDetectorFactory.CreateCollisionDetector(collider, colliderType, interactionConfig);
            _InteractorType = interactorType;
            _localClientIDWrapper = localClientIDWrapper;
            _localAdminIndicator = localAdminIndicator;
        }

        protected bool IsInteractionAllowed(IGeneralInteractionModule interactable)
        {
            return interactable != null && interactable.IsInteractable && (!interactable.AdminOnly || _localAdminIndicator.IsLocalAdmin);
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
            if (collideInteractionModule.CollideInteractionType != CollideInteractionType.Feet)
                return;

            bool canInteract = _localClientIDWrapper.IsClientIDReady && IsInteractionAllowed(collideInteractionModule);

            if (canInteract)
                StartInteractingWithModule(collideInteractionModule);

            _currentCollidingInteractionModules.Add(collideInteractionModule, canInteract);
        }

        private void StartInteractingWithModule(ICollideInteractionModule collideInteractionModule)
        {
            collideInteractionModule.InvokeOnCollideEnter(_interactorID);

            if (collideInteractionModule.IsNetworked)
                _heldActivatableIDsAgainstNetworkFlags.Add(collideInteractionModule.ID, collideInteractionModule.IsNetworked);
        }

        private void HandleCollideEnd(ICollideInteractionModule collideInteractionModule)
        {
            if (collideInteractionModule.CollideInteractionType != CollideInteractionType.Feet)
                return;

            bool canInteract = _localClientIDWrapper.IsClientIDReady && IsInteractionAllowed(collideInteractionModule);

            if (canInteract)
            StopInteractingWithModule(collideInteractionModule);

            _currentCollidingInteractionModules.Remove(collideInteractionModule);
        }

        private void StopInteractingWithModule(ICollideInteractionModule collideInteractionModule)
        {
            collideInteractionModule.InvokeOnCollideExit(_interactorID);

            if (collideInteractionModule.IsNetworked)
                _heldActivatableIDsAgainstNetworkFlags.Remove(collideInteractionModule.ID);
        }

        internal void HandleUpdate()
        {
            if (!_localClientIDWrapper.IsClientIDReady)
                return;

            foreach (var kvp in _currentCollidingInteractionModules.ToList())
            {
                ICollideInteractionModule interactionModule = kvp.Key;
                bool isCurrentlyInteracting = kvp.Value;

                //If we are colliding with the interaction module, but not interacting with it, and we are admin, we should start interacting
                if (!isCurrentlyInteracting && IsInteractionAllowed(interactionModule))
                {
                    StartInteractingWithModule(interactionModule);
                    _currentCollidingInteractionModules[interactionModule] = true;
                }
                //If we are colliding with the interaction module, and we are interacting with it, but we are not admin, we should stop interacting
                else if (isCurrentlyInteracting && !IsInteractionAllowed(interactionModule))
                {
                    StopInteractingWithModule(interactionModule);
                    _currentCollidingInteractionModules[interactionModule] = false;
                }
            }
        }
    }
}
