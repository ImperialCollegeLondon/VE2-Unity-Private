using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using VE2.Core.Common;
using static VE2.Common.CommonSerializables;
using VE2.Core.VComponents.InteractableInterfaces;

namespace VE2.Common
{
    /* A number of these service references should exist at editor time, so inspectors can respond to their presence 
    *  These service integrations have their GameObject name recorded in the locator so they can be re-located after a domain reload
    *  Services/containers that do NOT record GO names exist at runtime only */

    [ExecuteInEditMode]
    public class VE2CoreServiceLocator : MonoBehaviour
    {
        private static VE2CoreServiceLocator _instance;
        public static VE2CoreServiceLocator Instance { //Reload-proof singleton
            get {
                if (_instance == null)
                    _instance = FindFirstObjectByType<VE2CoreServiceLocator>();

                if (_instance == null && !Application.isPlaying)
                    _instance = new GameObject($"VE2CoreServiceLocator{SceneManager.GetActiveScene().name}").AddComponent<VE2CoreServiceLocator>();

                return _instance;
            }
        }

        //################################ MULTIPLAYER SUPPORT ########################################
        //#############################################################################################

        //Record the gameobject name so we can re-locate multiplayer support after a domain reload
        [SerializeField, HideInInspector] private string _multiplayerSupportGOName;
        private IMultiplayerSupport _multiPlayerSupport;
        public IMultiplayerSupport MultiplayerSupport {
            get {
                if (_multiPlayerSupport == null && !string.IsNullOrEmpty(_multiplayerSupportGOName))
                    _multiPlayerSupport = GameObject.Find(_multiplayerSupportGOName)?.GetComponent<IMultiplayerSupport>();

                if (_multiPlayerSupport == null || !_multiPlayerSupport.IsEnabled)
                    return null;
                else
                    return _multiPlayerSupport;
            }
            set //Will need to be called externally
            {
                _multiPlayerSupport = value;

                if (value != null)
                    _multiplayerSupportGOName = value.GameObjectName;
            }
        }

        //############################## PLAYER SETTINGS PROVIDER #####################################
        //#############################################################################################

        [SerializeField, HideInInspector] public string PlayerSettingsProviderGOName; //{ get; private set; }
        private IPlayerSettingsProvider _playerSettingsProvider;
        public IPlayerSettingsProvider PlayerSettingsProvider {
            get {

                if (_playerSettingsProvider == null && !string.IsNullOrEmpty(PlayerSettingsProviderGOName))
                    _playerSettingsProvider = GameObject.Find(PlayerSettingsProviderGOName)?.GetComponent<IPlayerSettingsProvider>();

                if (_playerSettingsProvider == null || !_playerSettingsProvider.IsEnabled)
                    return null;
                else 
                    return _playerSettingsProvider;
            }
            set //Will need to be called externally
            {
                _playerSettingsProvider = value;

                if (value != null)
                {
                    PlayerSettingsProviderGOName = value.GameObjectName;
                }
            }
        }

        //############################## PLAYER OVERRIDES PROVIDER ####################################
        //#############################################################################################

        [SerializeField, HideInInspector] public string PlayerOverridesProviderGOName; // { get; private set; }
        private IPlayerAppearanceOverridesProvider _playerOverridesProvider;
        public IPlayerAppearanceOverridesProvider PlayerAppearanceOverridesProvider
        {
            get
            {
                if (_playerOverridesProvider == null && !string.IsNullOrEmpty(PlayerOverridesProviderGOName))
                    _playerOverridesProvider = GameObject.Find(PlayerOverridesProviderGOName)?.GetComponent<IPlayerAppearanceOverridesProvider>();

                if (_playerOverridesProvider == null || !_playerOverridesProvider.IsEnabled)
                    return null;
                else
                    return _playerOverridesProvider;
            }
            set //Will need to be called externally
            {
                _playerOverridesProvider = value;

                if (value != null)
                    PlayerOverridesProviderGOName = value.GameObjectName;
            }
        }

        //############################### STATE MODULE CONTAINERS #####################################
        //#############################################################################################

        public WorldStateModulesContainer WorldStateModulesContainer {get; private set;} = new();
        public PlayerStateModuleContainer PlayerStateModuleContainer {get; private set;} = new();

        //##################################### INPUT HANDLER #########################################
        //#############################################################################################

        private IInputHandler _inputHandler;
        public IInputHandler InputHandler //Returns the default InputHandler 
        {
            get
            {
                if (!Application.isPlaying)
                {
                    Debug.LogError("InputHandler is only available at runtime");   
                    return null;
                }

                _inputHandler ??= FindFirstObjectByType<InputHandler>();
                _inputHandler ??= new GameObject("V_InputHandler").AddComponent<InputHandler>();
                return _inputHandler;
            }
        }

        //#################################### RAYCAST PROVIDER #######################################
        //#############################################################################################

        // private IRaycastProvider _raycastProvider;
        // public IRaycastProvider RaycastProvider { get { //Returns the default RaycastProvider
        //     _raycastProvider ??= new RaycastProvider();
        //     return _raycastProvider;
        // }}

        //#################################### XR MANAGER WRAPPER #####################################
        //#############################################################################################

        private IXRManagerWrapper _xrManagerWrapper;
        public IXRManagerWrapper XRManagerWrapper { get {
            _xrManagerWrapper ??= new XRManagerWrapper(); //Returns the default XRManager
            return _xrManagerWrapper;
        }}

        //################################## MONOBEHAVIOUR METHODS ####################################
        //#############################################################################################

        private void Awake()
        {
            _instance = this;
            gameObject.hideFlags = HideFlags.HideInHierarchy; //To hide
            //gameObject.hideFlags &= ~HideFlags.HideInHierarchy; //To show
        }

        private void OnDestroy() 
        {
            WorldStateModulesContainer.Reset();
            PlayerStateModuleContainer.Reset();
        }
    }

    public interface IPlayerAppearanceOverridesProvider
    {
        public AvatarAppearanceOverrideType HeadOverrideType { get; }
        public AvatarAppearanceOverrideType TorsoOverrideType { get; }
        public bool IsEnabled { get; }
        public string GameObjectName { get; }

        public void NotifyProviderOfChangeAppearanceOverrides();
        public event Action OnAppearanceOverridesChanged;

        public List<GameObject> HeadOverrideGOs { get; }
        public List<GameObject> TorsoOverrideGOs { get; }
    }

    public class WorldStateModulesContainer : BaseStateModuleContainer
    {
        private List<IWorldStateModule> _worldstateSyncableModules = new();
        public IReadOnlyList<IWorldStateModule> WorldstateSyncableModules => _worldstateSyncableModules.AsReadOnly();
        public event Action<IWorldStateModule> OnWorldStateModuleRegistered;
        public event Action<IWorldStateModule> OnWorldStateModuleDeregistered;

        public override void RegisterStateModule(IBaseStateModule moduleBase)
        {
            IWorldStateModule module = (IWorldStateModule)moduleBase;
            _worldstateSyncableModules.Add(module);
            OnWorldStateModuleRegistered?.Invoke(module);
        }

        public override void DeregisterStateModule(IBaseStateModule moduleBase)
        {
            IWorldStateModule module = (IWorldStateModule)moduleBase;
            _worldstateSyncableModules.Remove(module);
            OnWorldStateModuleDeregistered?.Invoke(module);
        }

        public override void Reset() => _worldstateSyncableModules.Clear();
    }

    public class PlayerStateModuleContainer : BaseStateModuleContainer
    {
        public IPlayerStateModule PlayerStateModule {get; private set;}
        public event Action<IPlayerStateModule> OnPlayerStateModuleRegistered;
        public event Action<IPlayerStateModule> OnPlayerStateModuleDeregistered;

        public override void RegisterStateModule(IBaseStateModule moduleBase)
        {
            PlayerStateModule = (IPlayerStateModule)moduleBase;
            OnPlayerStateModuleRegistered?.Invoke(PlayerStateModule);
        }

        public override void DeregisterStateModule(IBaseStateModule moduleBase)
        {
            PlayerStateModule = null;
            OnPlayerStateModuleDeregistered?.Invoke((IPlayerStateModule)moduleBase);
        }

        public override void Reset() => PlayerStateModule = null;
    }

    public abstract class BaseStateModuleContainer
    {   
        public abstract void RegisterStateModule(IBaseStateModule module);
        public abstract void DeregisterStateModule(IBaseStateModule module);

        //Doesn't emit events, used on exit playmode 
        public abstract void Reset();
    }

    public class InteractorContainer
    {
        private Dictionary<InteractorID, IInteractor> _interactors = new();
        public IReadOnlyDictionary<InteractorID, IInteractor> Interactors => _interactors;

        public void RegisterInteractor(InteractorID interactorID, IInteractor interactor)
        {
            _interactors[interactorID] = interactor;
        }

        public void DeregisterInteractor(InteractorID interactorID)
        {
            _interactors.Remove(interactorID);
        }

        public void Reset() => _interactors.Clear();
    }
}
