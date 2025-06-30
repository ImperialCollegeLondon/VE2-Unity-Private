using UnityEngine;

namespace VE2.NonCore.Instancing.API
{
    public interface IV_GameObjectSpawnManager
    {
        public GameObject ObjectToSpawn { get; set; }
        public Transform SpawnPosition { get; set; }
        public void SpawnGameObject(string gameObjectName = "none");
        public GameObject SpawnAndReturnGameObject(string gameObjectName = "none");
        public void DespawnGameObject(GameObject gameobjectToDespawn);
    }
}
