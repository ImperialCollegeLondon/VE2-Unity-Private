using UnityEngine;

namespace VE2.Core.VComponents.API
{
    public interface IRigidbodyWrapper
    {
        protected Rigidbody _Rigidbody { get; }

        public bool isKinematic { get => _Rigidbody.isKinematic; set => _Rigidbody.isKinematic = value; }
        public Vector3 linearVelocity { get => _Rigidbody.linearVelocity; set => _Rigidbody.linearVelocity = value; }
        public Vector3 angularVelocity { get => _Rigidbody.angularVelocity; set => _Rigidbody.angularVelocity = value; }
        public Vector3 position { get => _Rigidbody.position; set => _Rigidbody.position = value; }
        public Quaternion rotation { get => _Rigidbody.rotation; set => _Rigidbody.rotation = value; }
    }
    public class RigidbodyWrapper : IRigidbodyWrapper
    {
        Rigidbody IRigidbodyWrapper._Rigidbody => _rigidbody;

        private readonly Rigidbody _rigidbody;

        public RigidbodyWrapper(Rigidbody rigidbody)
        {
            _rigidbody = rigidbody;
        }
    }
}

