using System.Collections.Generic;
using UnityEngine;
using VE2.Common.API;
using VE2.Common.Shared;
using VE2.Core.VComponents.API;
using VE2.NonCore.Instancing.API;

namespace VE2.NonCore.Instancing.Internal
{
    internal class V_GameObjectSpawnManager : MonoBehaviour, IV_GameObjectSpawnManager
    {
        [SerializeField] private V_NetworkObject _networkObject;
        [SerializeField] private GameObject _gameobjectToSpawn;
        [SerializeField] private Transform _spawnPosition;
        [SerializeField] private bool restrictNumberOfObjects = false;
        [SerializeField] private int maxNumberOfObjects = 15;
        [SerializeField] private GameObject tooManyObjectsMessage = null;

        private Dictionary<string, GameObject> gameobjectsAgainstIDs = new();
        private int numberOfSpawnedgameobjects = 0;
        public GameObject ObjectToSpawn { get => _gameobjectToSpawn; set => _gameobjectToSpawn = value; }
        public Transform SpawnPosition { get => _spawnPosition; set => _spawnPosition = value; }

        private void Awake()
        {
            tooManyObjectsMessage?.SetActive(false);
        }

        //Invoke this to spawn the GameObject
        public GameObject SpawnGameObject()
        {
            //As is best practice with modifying network data, we only do so if we are the host of the instance.
            //If this function is called programmatically by the plugin, the plugin should ensure the call occurs on the host machine.
            //A good way of doing this is by ensuring the call happens on ALL machines,
            //which would be the case if the call was triggered by e.g a networked activatable.
            if (!VE2API.InstanceService.IsHost)
                return null;

            if (restrictNumberOfObjects && gameobjectsAgainstIDs.Count >= maxNumberOfObjects)
                return null;

            GameObject newGO = SpawnNewGameObject();

            //Update the network object with the list of gameobjects
            //If we're non-host, this will go to host and sync back to everyone else 
            //If we're the host, this will go direct to all non-hosts 
            gameObject.GetComponent<IV_NetworkObject>().UpdateData(new List<string>(gameobjectsAgainstIDs.Keys));

            return newGO;
        }

        //Invoke this to delete the GameObject
        public void DespawnGameObject(GameObject gameobjectToDespawn)
        {
            //The collision will happen on the host machine, so no need to call this on the non-hosts!
            if (!VE2API.InstanceService.IsHost)
                return;

            DespawnGameobject(gameobjectToDespawn);

            //Update the network object with the list of spawnedGameObjects
            gameObject.GetComponent<IV_NetworkObject>().UpdateData(new List<string>(gameobjectsAgainstIDs.Keys));
        }

        //Data received from network object - Link this to the V_NetworkObject in the inspector
        public void OnReceiveData(object obj)
        {
            //We receive the same list of gameobject IDs that we transmitted
            List<string> incomingGOIDList = (List<string>)obj;

            foreach (string id in incomingGOIDList)
            {
                //If there's a ball in the list that don't have, spawn it!
                if (!gameobjectsAgainstIDs.ContainsKey(id))
                    SpawnNewGameObject(id);
            }

            foreach (string localID in new List<string>(gameobjectsAgainstIDs.Keys))
            {
                if (incomingGOIDList.Contains(localID))
                    continue;

                //If there's a ball in our local list that isn't in the received list, despawn it!
                if (gameobjectsAgainstIDs.TryGetValue(localID, out GameObject goToDespawn))
                    DespawnGameobject(goToDespawn);
            }
        }

        //Will be "none" if the spawn has been triggered locally, rather than through sync data  
        private GameObject SpawnNewGameObject(string gameObjectName = "none")
        {
            numberOfSpawnedgameobjects++;

            if (gameObjectName.Equals("none") || gameobjectsAgainstIDs.ContainsKey(gameObjectName))
                gameObjectName = "spawnedGameobject" + numberOfSpawnedgameobjects;

            //A hack to disable onenable using a parent object and enabling once we rename
            GameObject boot = new GameObject(gameObjectName + "_boot");
            boot.SetActive(false);

            GameObject newGO = Instantiate(_gameobjectToSpawn, _spawnPosition.position, _spawnPosition.rotation, boot.transform);
            newGO.name = gameObjectName;
            newGO.transform.SetParent(null);
            Destroy(boot);
            newGO.SetActive(true);

            gameobjectsAgainstIDs.Add(gameObjectName, newGO);

            if (restrictNumberOfObjects && gameobjectsAgainstIDs.Count >= maxNumberOfObjects)
                tooManyObjectsMessage?.SetActive(true);

            return newGO;
        }

        private void DespawnGameobject(GameObject goToDespawn)
        {
            gameobjectsAgainstIDs.Remove(goToDespawn.name);
            Destroy(goToDespawn);

            if (restrictNumberOfObjects && gameobjectsAgainstIDs.Count < maxNumberOfObjects)
                tooManyObjectsMessage?.SetActive(false);
        }

        public int GetNumberOfSpawnedGameObjects()
        {
            return numberOfSpawnedgameobjects;
        }
    }
}


