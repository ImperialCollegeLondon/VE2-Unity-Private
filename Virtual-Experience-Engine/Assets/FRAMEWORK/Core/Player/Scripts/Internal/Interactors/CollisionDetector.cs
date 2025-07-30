using System;
using System.Collections.Generic;
using UnityEngine;
using VE2.Core.VComponents.API;

namespace VE2.Core.Player.Internal
{
    internal interface ICollisionDetector
    {
        public event Action<ICollideInteractionModule> OnCollideStart;
        public event Action<ICollideInteractionModule> OnCollideEnd;
        //public ColliderType ColliderType { get; set; }
    }

    [AddComponentMenu("")] // Prevents this MonoBehaviour from showing in the Add Component menu
    internal class CollisionDetector : MonoBehaviour, ICollisionDetector
    {
        public event Action<ICollideInteractionModule> OnCollideStart;
        public event Action<ICollideInteractionModule> OnCollideEnd;

        //TODO: Why do we have this?
        private ColliderType _colliderType { get; set; }
        private PlayerInteractionConfig _interactionConfig;

        public void Setup(ColliderType colliderType, PlayerInteractionConfig interactionConfig)
        {
            _colliderType = colliderType;
            _interactionConfig = interactionConfig;
        }
        
        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.TryGetComponent(out ICollideInteractionModuleProvider collidable))
            {
                if (((1 << collidable.Layer) & _interactionConfig.InteractableLayers) != 0)
                    OnCollideStart?.Invoke(collidable.CollideInteractionModule);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.gameObject.TryGetComponent(out ICollideInteractionModuleProvider collidable))
            {
                if (((1 << collidable.Layer) & _interactionConfig.InteractableLayers) != 0)
                    OnCollideEnd?.Invoke(collidable.CollideInteractionModule);
            }
        }
    }

    internal interface ICollisionDetectorFactory
    {
        internal ICollisionDetector CreateCollisionDetector(Collider collider, ColliderType colliderType, PlayerInteractionConfig interactionConfig);
    }

    internal class CollisionDetectorFactory : ICollisionDetectorFactory
    {
        ICollisionDetector ICollisionDetectorFactory.CreateCollisionDetector(Collider collider, ColliderType colliderType, PlayerInteractionConfig interactionConfig)
        {
            if (collider == null)
            {
                Debug.LogError("Collider is null. Cannot create CollisionDetector.");
                return null;
            }

            CollisionDetector collisionDetector = collider.gameObject.AddComponent<CollisionDetector>();
            collisionDetector.Setup(colliderType, interactionConfig);
            return collisionDetector;
        }
    }

    internal enum ColliderType
    {
        Feet2D,
        FeetVR,
        HandVRLeft,
        HandVRRight,
        None
    }
}
