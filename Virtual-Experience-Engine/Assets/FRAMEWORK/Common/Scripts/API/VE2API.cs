using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using VE2.Core.VComponents.API;
using VE2.Core.Player.API;
using VE2.NonCore.Instancing.API;
using VE2.Common.Shared;

namespace VE2.Common.API
{
    [ExecuteAlways]
    [AddComponentMenu("")] // Prevents this MonoBehaviour from showing in the Add Component menu
    /// <summary>
    /// Central service locator for VE2 APIs
    /// </summary>
    public class VE2API : MonoBehaviour 
    {
        private static VE2API _instance;
        private static VE2API Instance //Reload-proof singleton
        { 
            get
            {
                //if we've moved to a different scene, this will be null, so we can find the locator for the new scene
                if (_instance == null)
                    _instance = FindFirstObjectByType<VE2API>();

                if (_instance == null)
                    _instance = new GameObject($"VE2API{SceneManager.GetActiveScene().name}").AddComponent<VE2API>();

                return _instance;
            }
        }

        private void Awake()
        {
            if (!Application.isPlaying)
            {
                if (FindObjectsByType<VE2API>(FindObjectsSortMode.None).Length > 1)
                {
                    Debug.LogError("There should only be one VE2API in the scene");
                    Destroy(gameObject);
                }

                _instance = this;
                gameObject.hideFlags = HideFlags.HideInHierarchy; //To hide
                //gameObject.hideFlags &= ~HideFlags.HideInHierarchy; //To show
            }
            else 
            {
                if (Instance._instancingServiceProvider == null)
                    _instance._localClientIdWrapper.Value = 0;
                //Otherwise, instancing will be in charge of setting the client ID
            }
        }

        //########################################################################################################
        #region PlayerAPI

        public static IPlayerService Player => PlayerServiceProvider.PlayerService;

        private IPlayerServiceProvider _playerServiceProvder;
        internal static IPlayerServiceProvider PlayerServiceProvider
        {
            private get
            {
                if (Instance._playerServiceProvder == null)
                {
                    foreach (GameObject go in FindObjectsByType<GameObject>(FindObjectsSortMode.None))
                    {
                        if (go.TryGetComponent(out IPlayerServiceProvider playerServiceProvider))
                        {
                            Instance._playerServiceProvder = playerServiceProvider;
                            break;
                        }
                    }
                }

                return Instance._playerServiceProvder;
            }
            set => Instance._playerServiceProvder = value;
        }

        [SerializeField, HideInInspector] private bool _preferVRMode = false;
        public static bool PreferVRMode { get => Instance._preferVRMode; set => Instance._preferVRMode = value; }

        [SerializeField, HideInInspector] private ClientIDWrapper _localClientIdWrapper = new(ushort.MaxValue, true);
        public static IClientIDWrapper LocalClientIdWrapper => Instance._localClientIdWrapper;

        #endregion
        //########################################################################################################
        //########################################################################################################
        #region InputAPI

        //Lives here so we can inject a stub for testing
        private IInputHandler _inputHandler;
        internal static IInputHandler InputHandler //Returns the default InputHandler 
        {
            get
            {
                if (!Application.isPlaying)
                {
                    Debug.LogError("InputHandler is only available at runtime");
                    return null;
                }
                    
                Instance._inputHandler ??= FindFirstObjectByType<InputHandler>();
                Instance._inputHandler ??= new GameObject("V_InputHandler").AddComponent<InputHandler>();
                return Instance._inputHandler;
            }
        }

        #endregion
        //########################################################################################################
        //########################################################################################################
        #region InstancingAPI

        public static IInstanceService InstanceService => InstancingServiceProvider?.InstanceService;

        private IInstanceProvider _instancingServiceProvider;
        internal static IInstanceProvider InstancingServiceProvider
        {
            get
            {
                if (Instance._instancingServiceProvider == null)
                {
                    foreach (GameObject go in FindObjectsByType<GameObject>(FindObjectsSortMode.None))
                    {
                        if (go.TryGetComponent(out IInstanceProvider instanceProvider))
                        {
                            Instance._instancingServiceProvider = instanceProvider;
                            break;
                        }
                    }
                }

                return Instance._instancingServiceProvider;

            }
            set => Instance._instancingServiceProvider = value;
        }

        public static bool HasMultiPlayerSupport => InstancingServiceProvider != null && InstancingServiceProvider.IsEnabled;

        #endregion
        //########################################################################################################
        //########################################################################################################
        #region Internal Containers and Wrappers
        //Note - these don't need to be serialized, registrations will repeat on reload

        /// <summary>
        /// Contains all networked WorldStateModules, allows them to be picked up by the syncer without tight coupling 
        /// </summary>
        internal static IWorldStateSyncableContainer WorldStateSyncableContainer => Instance._worldStateSyncableContainer;
        private WorldStateSyncableContainer _worldStateSyncableContainer = new();

        /// <summary>
        /// Contains a reference to the PlayerService, allows it to be picked up by the syncer without tight coupling
        /// </summary>
        internal static ILocalPlayerSyncableContainer LocalPlayerSyncableContainer => Instance._localPlayerSyncableContainer;
        private LocalPlayerSyncableContainer _localPlayerSyncableContainer = new();
        
        /// <summary>
        /// Contains all interactors (local or otherwise) in the scene, allows grabbables to perform validation on grab
        /// </summary>
        internal static HandInteractorContainer InteractorContainer => Instance._interactorContainer; 
        private HandInteractorContainer _interactorContainer = new();

        /// <summary>
        /// Contains all grab interactables in the scene, allows grabbables to notify interactors of grabs by just passing an ID
        /// </summary>
        internal static IGrabInteractablesContainer GrabInteractablesContainer => Instance._grabInteractablesContainer;
        private GrabInteractablesContainer _grabInteractablesContainer = new();

        //########################################################################################################
        #endregion
    }

    //TODO: Find a proper home for the objects below

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
    }

    internal interface IGrabInteractablesContainer
    {
        Dictionary<string, IRangedGrabInteractionModule> GrabInteractables { get; }
        void RegisterGrabInteractable(IRangedGrabInteractionModule grabInteractable, string id);
        void DeregisterGrabInteractable(string id);
    }

    internal class GrabInteractablesContainer : IGrabInteractablesContainer
    {
        public Dictionary<string, IRangedGrabInteractionModule> GrabInteractables { get; } = new();

        public void RegisterGrabInteractable(IRangedGrabInteractionModule grabInteractable, string id)
        {
            GrabInteractables.Add(id, grabInteractable);
        }

        public void DeregisterGrabInteractable(string id)
        {
            GrabInteractables.Remove(id);
        }
    }

    internal interface ILocalPlayerSyncableContainer
    {
        IPlayerServiceInternal LocalPlayerSyncable { get; }
        event Action<IPlayerServiceInternal> OnPlayerRegistered;
        event Action<IPlayerServiceInternal> OnPlayerDeregistered;
        void RegisterLocalPlayer(IPlayerServiceInternal playerServiceInternal);
        void DeregisterLocalPlayer();
    }

    internal class LocalPlayerSyncableContainer : ILocalPlayerSyncableContainer 
    {
        public IPlayerServiceInternal LocalPlayerSyncable { get; private set;}
        public event Action<IPlayerServiceInternal> OnPlayerRegistered;
        public event Action<IPlayerServiceInternal> OnPlayerDeregistered;

        public void RegisterLocalPlayer(IPlayerServiceInternal playerServiceInternal)
        {
            LocalPlayerSyncable = playerServiceInternal;
            OnPlayerRegistered?.Invoke(playerServiceInternal);
        }

        public void DeregisterLocalPlayer()
        {
            LocalPlayerSyncable = null;
            OnPlayerDeregistered?.Invoke(null);
        }
    }
}
