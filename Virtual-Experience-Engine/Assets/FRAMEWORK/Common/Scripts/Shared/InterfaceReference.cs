using System;
using UnityEngine;

namespace VE2.Common.Shared
{
    [Serializable]
    public class InterfaceReference<T> where T : class 
    {
        [SerializeField] private GameObject _gameObject;
        public GameObject GameObject => _gameObject;

        private T cached;

        public T Interface
        {
            get
            {
                if (cached == null && _gameObject != null)
                    cached = _gameObject.GetComponent(typeof(T)) as T;
                return cached;
            }
        }


        public static implicit operator T(InterfaceReference<T> reference) => reference?.Interface;
    }
}

