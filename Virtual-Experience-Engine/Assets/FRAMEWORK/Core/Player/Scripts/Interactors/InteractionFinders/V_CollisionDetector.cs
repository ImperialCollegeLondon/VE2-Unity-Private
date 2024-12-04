using System;
using UnityEngine;
using VE2.Core.VComponents.InteractableFindables;
using VE2.Core.VComponents.InteractableInterfaces;

namespace VE2.Core.Player.InteractionFinders
{
    public interface ICollisionDetector
    {
        public event Action<ICollideInteractionModule> OnCollideStart;
        public event Action<ICollideInteractionModule> OnCollideEnd;
    }

    public class V_CollisionDetector : MonoBehaviour, ICollisionDetector
    {
        public event Action<ICollideInteractionModule> OnCollideStart;
        public event Action<ICollideInteractionModule> OnCollideEnd;

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.TryGetComponent(out ICollidePlayerInteractableIntegrator collidable))
            {
                OnCollideStart?.Invoke(collidable.CollideInteractionModule);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.gameObject.TryGetComponent(out ICollidePlayerInteractableIntegrator collidable))
            {
                OnCollideEnd?.Invoke(collidable.CollideInteractionModule);
            }
        }
    }
}
