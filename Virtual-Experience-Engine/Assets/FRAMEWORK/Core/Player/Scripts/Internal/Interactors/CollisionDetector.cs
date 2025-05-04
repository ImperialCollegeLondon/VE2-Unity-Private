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
        public ColliderType ColliderType { get; set; }
    }

    [AddComponentMenu("")] // Prevents this MonoBehaviour from showing in the Add Component menu
    internal class CollisionDetector : MonoBehaviour, ICollisionDetector
    {
        public event Action<ICollideInteractionModule> OnCollideStart;
        public event Action<ICollideInteractionModule> OnCollideEnd;
        public ColliderType ColliderType { get; set; }
        
        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.TryGetComponent(out ICollideInteractionModuleProvider collidable))
            {
                OnCollideStart?.Invoke(collidable.CollideInteractionModule);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.gameObject.TryGetComponent(out ICollideInteractionModuleProvider collidable))
            {
                OnCollideEnd?.Invoke(collidable.CollideInteractionModule);
            }
        }
    }

    public interface ICollisionDetectorFactory
    {
        internal ICollisionDetector CreateCollisionDetector(Collider collider, ColliderType colliderType);
    }

    public class CollisionDetectorFactory : ICollisionDetectorFactory
    {
        ICollisionDetector ICollisionDetectorFactory.CreateCollisionDetector(Collider collider, ColliderType colliderType)
        {
            var collisionDetector = collider.gameObject.AddComponent<CollisionDetector>();
            return collisionDetector;
        }
    }

    public enum ColliderType
    {
        Feet2D,
        FeetVR,
        HandVRLeft,
        HandVRRight,
        None
    }
}
