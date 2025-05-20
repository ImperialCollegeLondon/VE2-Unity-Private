using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VE2.Core.VComponents.API
{
    //Internal as plugin doesn't talk to this - it talks to the interfaces on the VCs directly. 
    //This API is just for the 
    [ExecuteAlways]
    [AddComponentMenu("")] // Prevents this MonoBehaviour from showing in the Add Component menu
    internal class VComponentsAPI : MonoBehaviour
    {
        private static VComponentsAPI _instance;
        private static VComponentsAPI Instance
        { //Reload-proof singleton
            get
            {
                if (_instance == null)
                    _instance = FindFirstObjectByType<VComponentsAPI>();

                if (_instance == null)
                    _instance = new GameObject($"VComponentsAPI-{SceneManager.GetActiveScene().name}").AddComponent<VComponentsAPI>();

                return _instance;
            }
        }

        //Doesn't really belong here, only VC internals need this, but does no harm living here for now.
        //If we move this in future, we'll need another monobehaviour to preserve the groups during a reload, e.g VCInternalDataContainer or something
        private ActivatableGroupsContainer _activatableGroupsContainer = new(); 

        /// <summary>
        /// Contains all activatable groups in the scene, allows activatables to perform validation on activation  within their group 
        /// </summary>
        internal static ActivatableGroupsContainer ActivatableGroupsContainer { get => Instance._activatableGroupsContainer; private set => Instance._activatableGroupsContainer = value; }
        
        private void Awake()
        {
            _instance = this;
            gameObject.hideFlags = HideFlags.HideInHierarchy; //To hide
            //gameObject.hideFlags &= ~HideFlags.HideInHierarchy; //To show
        }
    }

    internal class ActivatableGroupsContainer
    {
        private Dictionary<string, List<ISingleInteractorActivatableStateModule>> _activatableGroups = new();
        public IReadOnlyDictionary<string, List<ISingleInteractorActivatableStateModule>> ActivatableGroups => _activatableGroups;

        public void RegisterActivatable(string activatableGroupID, ISingleInteractorActivatableStateModule singleInteractorActivatableStateModule)
        {
            if (!_activatableGroups.ContainsKey(activatableGroupID))
                _activatableGroups[activatableGroupID] = new List<ISingleInteractorActivatableStateModule>();

            _activatableGroups[activatableGroupID].Add(singleInteractorActivatableStateModule);
        }

        public void DeregisterActivatable(string activatableGroupID, ISingleInteractorActivatableStateModule singleInteractorActivatableStateModule)
        {
            if (_activatableGroups.ContainsKey(activatableGroupID))
                _activatableGroups[activatableGroupID].Remove(singleInteractorActivatableStateModule);
        }

        public List<ISingleInteractorActivatableStateModule> GetSingleInteractorActivatableStateModule(string activatableGroupID)
        {
            if (!_activatableGroups.ContainsKey(activatableGroupID))
                return new List<ISingleInteractorActivatableStateModule>();

            return _activatableGroups[activatableGroupID];
        }

        public void Reset() => _activatableGroups.Clear();
    }
}
