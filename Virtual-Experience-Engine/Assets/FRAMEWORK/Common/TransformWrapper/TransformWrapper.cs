using UnityEngine;
namespace VE2.Common.TransformWrapper
{
    public interface ITransformWrapper
    {
        protected Transform _Transform { get; }

        public Vector3 localPosition { get => _Transform.localPosition; set => _Transform.localPosition = value; }
        public Vector3 position { get => _Transform.position; set => _Transform.position = value; }
        public Quaternion localRotation { get => _Transform.localRotation; set => _Transform.localRotation = value; }
        public Quaternion rotation { get => _Transform.rotation; set => _Transform.rotation = value; }
    }

    public class TransformWrapper : ITransformWrapper
    {
        Transform ITransformWrapper._Transform => _transform;

        private readonly Transform _transform;

        public TransformWrapper(Transform transform)
        {
            _transform = transform;
        }
    }
}

