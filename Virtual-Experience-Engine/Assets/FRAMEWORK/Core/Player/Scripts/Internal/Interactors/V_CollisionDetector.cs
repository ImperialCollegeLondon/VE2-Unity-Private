using System;
using UnityEngine;
using VE2.Core.VComponents.API;

namespace VE2.Core.Player.Internal
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
}
