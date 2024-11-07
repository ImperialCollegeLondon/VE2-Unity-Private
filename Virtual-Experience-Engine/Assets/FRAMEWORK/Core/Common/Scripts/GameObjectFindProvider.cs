using UnityEngine;

namespace VE2.Common
{
    public interface IGameObjectFindProvider
    {
        public GameObject FindGameObject(string gameObjectName);

        public bool TryGetComponent<T>(GameObject gameObject, out T component) where T : class;
    }

    public class GameObjectFindProvider : IGameObjectFindProvider
    {
        public GameObject FindGameObject(string gameObjectName) => GameObject.Find(gameObjectName);

        public bool TryGetComponent<T>(GameObject gameObject, out T component) where T : class
         { 
            component = gameObject.GetComponent(typeof(T)) as T;
            return component != null; 
        }

    }
}
