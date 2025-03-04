using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VE2.Core.VComponents.API
{
    //Internal as plugin doesn't talk to this - it talks to the interfaces on the VCs directly
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

        private InteractorContainer _interactorContainer = new();
        /// <summary>
        /// Contains all interactors (local or otherwise) in the scene, allows grabbables to perform validation on grab
        /// </summary>
        internal static InteractorContainer InteractorContainer { get => Instance._interactorContainer; private set => Instance._interactorContainer = value; }

        private void Awake()
        {
            _instance = this;
            //gameObject.hideFlags = HideFlags.HideInHierarchy; //To hide
            gameObject.hideFlags &= ~HideFlags.HideInHierarchy; //To show
        }

        private void OnDestroy()
        {
            InteractorContainer.Reset();
        }
    }

    //Note, the interactor stuff needs to live in the VC API rather than the Player API 
    //This is because the VC interfaces need to be passed interactor info
    internal class InteractorContainer
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
}
