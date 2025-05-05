using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using VE2.Core.Common;
using VE2.Core.VComponents.API;
using VE2.Core.Player.API;
using VE2.NonCore.Instancing.API;

namespace VE2.Common.API
{
    [ExecuteAlways]
    [AddComponentMenu("")] // Prevents this MonoBehaviour from showing in the Add Component menu
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
                    _instance._localClientIdWrapper.ClientID = 0;
                //Otherwise, instancing will be in charge of setting the client ID
            }
        }

        #region PlayerAPI
        //########################################################################################################

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

        //########################################################################################################
        #endregion

        #region InputAPI
        //########################################################################################################

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

        //########################################################################################################
        #endregion

        #region InstancingAPI
        //########################################################################################################

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

        //########################################################################################################
        #endregion

        #region Internal Containers and Wrappers
        //########################################################################################################
        //Note - these don't need to be serialized, registrations will repeat on reload

        private WorldStateSyncableContainer _worldStateSyncableContainer = new();
        internal static IWorldStateSyncableContainer WorldStateSyncableContainer => Instance._worldStateSyncableContainer;

        private LocalPlayerSyncableContainer _localPlayerSyncableContainer = new();
        internal static ILocalPlayerSyncableContainer LocalPlayerSyncableContainer => Instance._localPlayerSyncableContainer;

        private GrabInteractablesContainer _grabInteractablesContainer = new();
        internal static IGrabInteractablesContainer GrabInteractablesContainer => Instance._grabInteractablesContainer;

        //########################################################################################################
        #endregion
    }

    //TODO: Find a proper home for the objects below

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

        public interface IClientIDWrapperInternal : IClientIDWrapper
    {
        new ushort ClientID { get; set; } 
    }

    public interface IClientIDWrapper
    {
        ushort ClientID { get; }
        public bool IsClientIDReady { get; }
        event Action<ushort> OnClientIDReady;
        public bool IsLocal { get; }
        public bool IsRemote { get; }
    }

    [Serializable] 
    public class ClientIDWrapper : IClientIDWrapperInternal
    { 
        [SerializeField] private ushort _clientID = ushort.MaxValue;

        public ushort ClientID 
        {
            get => _clientID;
            set 
            {
                _clientID = value;
                OnClientIDReady?.Invoke(value);
            }
        } 
        
        public event Action<ushort> OnClientIDReady;
        public bool IsClientIDReady => _clientID != ushort.MaxValue;

        [SerializeField] public bool IsLocal {get; set;}
        public bool IsRemote => !IsLocal;

        public ClientIDWrapper(ushort clientID, bool isLocal)
        {
            _clientID = clientID;
            IsLocal = isLocal;
        }
    }

    [Serializable]
    public class InterfaceReference<T> where T : class //TODO: Move into its own file
    {
        [SerializeField] private GameObject _gameObject;

        private T cached;

        public T Interface
        {
            get
            {
                if (cached == null && _gameObject != null)
                    cached = _gameObject.GetComponent(typeof(T)) as T;
                return cached;
            }
        }

        public static implicit operator T(InterfaceReference<T> reference) => reference?.Interface;
    }


}