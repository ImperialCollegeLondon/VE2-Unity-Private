using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.SocialPlatforms;
using ViRSE.Core.Player;
using ViRSE.Core.Shared;
using static ViRSE.Core.Shared.CoreCommonSerializables;

namespace ViRSE.Core //TODO workout namespace... Core.Common? Or just ViRSE.Common?
{
    [ExecuteInEditMode]
    public class ViRSECoreServiceLocator : MonoBehaviour
    {
        private static ViRSECoreServiceLocator _instance;
        public static ViRSECoreServiceLocator Instance { //Reload-proof singleton
            get {
                if (_instance == null)
                    _instance = FindFirstObjectByType<ViRSECoreServiceLocator>();

                if (_instance == null && !Application.isPlaying)
                    _instance = new GameObject($"ViRSECoreServiceLocator{SceneManager.GetActiveScene().name}").AddComponent<ViRSECoreServiceLocator>();

                return _instance;
            }
        }

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

        //Record the gameobject name so we can re-locate multiplayer support after a domain reload
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

        //[SerializeField] private string testString;
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

        public WorldStateModulesContainer WorldStateModulesContainer {get; private set;} = new();
        public ViRSEPlayerStateModuleContainer ViRSEPlayerStateModuleContainer {get; private set;} = new();


        private void Awake()
        {
            _instance = this;
            gameObject.hideFlags = HideFlags.HideInHierarchy; //To hide
            //gameObject.hideFlags &= ~HideFlags.HideInHierarchy; //To show
        }

        private void OnDestroy() 
        {
            WorldStateModulesContainer.Reset();
            ViRSEPlayerStateModuleContainer.Reset();
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

        public List<GameObject> GetHeadOverrideGOs();
        public List<GameObject> GetTorsoOverrideGOs();
    }

    // public interface IPlayerSpawner
    // {
    //     public bool IsEnabled { get; }
    //     public string GameObjectName { get; }

    //     //Unlike the other components, the player spawner should be able to 
    //     //Activate and deactivate at runtime
    //     public event Action OnEnabledStateChanged;
    // }

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

    public class ViRSEPlayerStateModuleContainer : BaseStateModuleContainer
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
}
