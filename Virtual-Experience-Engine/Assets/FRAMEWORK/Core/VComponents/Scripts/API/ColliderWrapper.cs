using System;
using UnityEngine;
using VE2.Common.Shared;

namespace VE2.Core.VComponents.API
{
    internal interface IColliderWrapper
    {
        // bit jank to expose the actual collider, but the 2d locomotor needs to see it so it can pass it to the unity API
        public Collider Collider { get; }

        public bool enabled
        {
            get => Collider.enabled;
            set => Collider.enabled = value;
        }

        public bool isTrigger
        {
            get => Collider.isTrigger;
            set => Collider.isTrigger = value;
        }

        public Vector3 ClosestPoint(Vector3 position) => Collider.ClosestPoint(position);

        public Bounds bounds => Collider.bounds;

        public PhysicsMaterial material
        {
            get => Collider.material;
            set => Collider.material = value;
        }

        public ITransformWrapper transform => new TransformWrapper(Collider.transform);

        public GameObject gameObject => Collider.gameObject;

        public bool Equals(Collider other) => Collider == other;
    }

    public class ColliderWrapper : IColliderWrapper
    {
        Collider IColliderWrapper.Collider => _collider;

        private readonly Collider _collider;

        public ColliderWrapper(Collider collider)
        {
            _collider = collider;
        }
    }
}
