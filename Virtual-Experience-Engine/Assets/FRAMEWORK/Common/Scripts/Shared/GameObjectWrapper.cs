using UnityEngine;

namespace VE2.Common.Shared
{
    internal interface IGameObjectWrapper 
    {
        public void SetActive(bool value);

        public GameObject GameObject { get; }
    }

    internal class GameObjectWrapper : IGameObjectWrapper
    {
        private readonly GameObject _gameObject;

        public GameObject GameObject => _gameObject;

        public GameObjectWrapper(GameObject gameObject) 
        {
            _gameObject = gameObject;
        }

        public void SetActive(bool value) => _gameObject.SetActive(value);
    }
}

