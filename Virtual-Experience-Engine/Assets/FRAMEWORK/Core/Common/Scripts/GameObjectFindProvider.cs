using UnityEngine;

namespace VE2.Common
{
    public interface IGameObjectFindProvider
    {
        public GameObject FindGameObject(string gameObjectName);
    }

    public class GameObjectFindProvider : IGameObjectFindProvider
    {
        public GameObject FindGameObject(string gameObjectName) => GameObject.Find(gameObjectName);
    }
}
