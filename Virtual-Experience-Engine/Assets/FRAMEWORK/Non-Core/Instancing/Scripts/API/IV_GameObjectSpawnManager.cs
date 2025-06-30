using UnityEngine;

namespace VE2.NonCore.Instancing.API
{
    public interface IV_GameObjectSpawnManager
    {
        public GameObject ObjectToSpawn { get; set; }
        public Transform SpawnPosition { get; set; }
        public GameObject SpawnGameObject();
        public void DespawnGameObject(GameObject gameobjectToDespawn);
    }
}
