using System.Collections.Generic;
using UnityEngine;
using VE2.Common.API;
using VE2.Common.Shared;
using VE2.Core.VComponents.API;
using VE2.NonCore.Instancing.API;

namespace VE2.NonCore.Instancing.Internal
{
    public class V_GameObjectSpawnManager : MonoBehaviour
    {
        [SerializeField] private V_NetworkObject _networkObject;
        [SerializeField] private GameObject gameobjectToSpawn;
        [SerializeField] private Transform spawnPosition;

        private Dictionary<string, GameObject> gameobjectsAgainstIDs = new();
        private int numberOfSpawnedgameobjects = 0;

        [SerializeField] private bool restrictNumberOfObjects = false;

        [SerializeField] private int maxNumberOfObjects = 15;

        [SerializeField] private GameObject tooManyObjectsMessage = null;

        private void Awake()
        {
            tooManyObjectsMessage?.SetActive(false);
        }

        public void SpawnGameObject()
        {
            SpawnAndReturnGameObject();
        }

        //Invoke this to spawn the GameObject
        public GameObject SpawnAndReturnGameObject()
        {
            //Since this function is called by an activatable, we know it'll be called on the host client
            //Since it's being called on the host client, we don't need it to be called on nonhosts too!
            if (!VE2API.InstanceService.IsHost)
                return null;

            if (restrictNumberOfObjects && gameobjectsAgainstIDs.Count >= maxNumberOfObjects)
            {
                return null;
            }

            GameObject newGO = SpawnNewGameObject();

            //Update the network object with the list of gameobjects
            //If we're non-host, this will go to host and sync back to everyone else 
            //If we're the host, this will go direct to all non-hosts 
            gameObject.GetComponent<IV_NetworkObject>().UpdateData(new List<string>(gameobjectsAgainstIDs.Keys));

            return newGO;
        }

        //Invoke this to delete the GameObject
        public void OnDespawnTriggered(GameObject gameobjectToDespawn)
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
        private GameObject SpawnNewGameObject(string goName = "none")
        {
            numberOfSpawnedgameobjects++;

            if (goName.Equals("none"))
                goName = "spawnedGameobject" + numberOfSpawnedgameobjects;

            GameObject newGO = Instantiate(gameobjectToSpawn, spawnPosition.position, spawnPosition.rotation);
            newGO.SetActive(false);
            newGO.name = goName;
            newGO.SetActive(true);
            gameobjectsAgainstIDs.Add(goName, newGO);

            if (restrictNumberOfObjects && gameobjectsAgainstIDs.Count >= maxNumberOfObjects)
            {
                tooManyObjectsMessage?.SetActive(true);
            }

            return newGO;
        }

        private void DespawnGameobject(GameObject goToDespawn)
        {
            gameobjectsAgainstIDs.Remove(goToDespawn.name);
            Destroy(goToDespawn);

            if (restrictNumberOfObjects && gameobjectsAgainstIDs.Count < maxNumberOfObjects)
            {
                tooManyObjectsMessage?.SetActive(false);
            }
        }

        public int GetNumberOfSpawnedGameObjects()
        {
            return numberOfSpawnedgameobjects;
        }
    }
}


