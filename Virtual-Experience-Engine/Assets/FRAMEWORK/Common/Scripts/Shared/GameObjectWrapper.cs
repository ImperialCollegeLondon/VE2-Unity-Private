using UnityEngine;

namespace VE2.Common.Shared
{
    public interface IGameObjectWrapper 
    {
        public void SetActive(bool value);
    }

    public class GameObjectWrapper : IGameObjectWrapper
    {
        private readonly GameObject _gameObject;

        public GameObjectWrapper(GameObject gameObject) 
        {
            _gameObject = gameObject;
        }

        public void SetActive(bool value) => _gameObject.SetActive(value);
    }
}

