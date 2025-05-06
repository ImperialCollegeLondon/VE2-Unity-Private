using System;
using UnityEngine;
using VE2.Common.Shared;

namespace VE2.Core.VComponents.API
{
    internal interface IRigidbodyWrapper
    { 
        protected Rigidbody _Rigidbody { get; }

        //Deviating from our naming convention here to match Unity's API
        public bool isKinematic { get => _Rigidbody.isKinematic; set => _Rigidbody.isKinematic = value; }
        public Vector3 linearVelocity { get => _Rigidbody.linearVelocity; set => _Rigidbody.linearVelocity = value; }
        public Vector3 angularVelocity { get => _Rigidbody.angularVelocity; set => _Rigidbody.angularVelocity = value; }
        public Vector3 position { get => _Rigidbody.position; set => _Rigidbody.position = value; }
        public Quaternion rotation { get => _Rigidbody.rotation; set => _Rigidbody.rotation = value; }
        public ITransformWrapper transform { get => new TransformWrapper(_Rigidbody.transform); }

        public Renderer renderer { get => _Rigidbody.GetComponent<Renderer>(); }

        public bool Equals (Rigidbody other)
        {
            return _Rigidbody == other;
        }

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

