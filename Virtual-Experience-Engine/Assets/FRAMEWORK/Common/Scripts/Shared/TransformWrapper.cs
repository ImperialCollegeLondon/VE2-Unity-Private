using UnityEngine;

namespace VE2.Common.Shared
{
    internal interface ITransformWrapper
    {
        protected Transform _Transform { get; }

        public Vector3 localPosition { get => _Transform.localPosition; set => _Transform.localPosition = value; }
        public Vector3 position { get => _Transform.position; set => _Transform.position = value; }
        public Quaternion localRotation { get => _Transform.localRotation; set => _Transform.localRotation = value; }
        public Quaternion rotation { get => _Transform.rotation; set => _Transform.rotation = value; }
        public Vector3 forward { get => _Transform.forward; }
        public Vector3 right { get => _Transform.right; }
        public Vector3 up { get => _Transform.up; }
        public Vector3 InverseTransfromPoint(Vector3 point) => _Transform.parent.InverseTransformPoint(point);
    }

    internal class TransformWrapper : ITransformWrapper
    {
        Transform ITransformWrapper._Transform => _transform;

        private readonly Transform _transform;

        public TransformWrapper(Transform transform)
        {
            _transform = transform;
        }
    }
}

