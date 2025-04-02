using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VE2.Core.VComponents.API
{
    //Internal as plugin doesn't talk to this - it talks to the interfaces on the VCs directly
    [ExecuteAlways]
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

        internal static bool HasMultiPlayerSupport => WorldStateSyncProvider != null && WorldStateSyncProvider.IsEnabled;
        internal static IWorldStateSyncService WorldStateSyncService => HasMultiPlayerSupport ? WorldStateSyncProvider?.WorldStateSyncService : null;

        [SerializeField, HideInInspector] private string _worldStateSyncProviderGOName;
        private IWorldStateSyncProvider _worldStateSyncProvider;
        internal static IWorldStateSyncProvider WorldStateSyncProvider {
            get 
            {
                if (Instance._worldStateSyncProvider == null && !string.IsNullOrEmpty(Instance._worldStateSyncProviderGOName))
                    Instance._worldStateSyncProvider = GameObject.Find(Instance._worldStateSyncProviderGOName)?.GetComponent<IWorldStateSyncProvider>();

                return Instance._worldStateSyncProvider;
            }
            set
            {
                Instance._worldStateSyncProvider = value;

                if (value != null)
                    Instance._worldStateSyncProviderGOName = value.GameObjectName;
            }
        }

        private HandInteractorContainer _interactorContainer = new();

        private ActivatableGroupsContainer _activatableGroupsContainer = new(); 

        /// <summary>
        /// Contains all interactors (local or otherwise) in the scene, allows grabbables to perform validation on grab
        /// </summary>
        internal static HandInteractorContainer InteractorContainer { get => Instance._interactorContainer; private set => Instance._interactorContainer = value; }

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

        private void OnDestroy()
        {
            InteractorContainer?.Reset();
        }
    }

    //Note, the interactor stuff needs to live in the VC API rather than the Player API 
    //This is because the VC interfaces need to be passed interactor info
    internal class HandInteractorContainer
    {
        private Dictionary<string, IInteractor> _interactors = new();
        public IReadOnlyDictionary<string, IInteractor> Interactors => _interactors;

        public void RegisterInteractor(string interactorID, IInteractor interactor)
        {
            _interactors[interactorID] = interactor;
        }

        public void DeregisterInteractor(string interactorID)
        {
            _interactors.Remove(interactorID);
        }

        public void Reset() => _interactors.Clear();
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
