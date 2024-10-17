using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
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
        [SerializeField] public string PlayerSettingsProviderGOName; //{ get; private set; }
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
        [SerializeField] public string PlayerOverridesProviderGOName; // { get; private set; }
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

        private List<IStateModule> _worldstateSyncableModules = new();
        public IReadOnlyList<IStateModule> WorldstateSyncableModules => _worldstateSyncableModules.AsReadOnly();
        public event Action<IStateModule> OnStateModuleRegistered;
        public event Action<IStateModule> OnStateModuleDeregistered;
        public void RegisterStateModule(IStateModule module)
        {
            _worldstateSyncableModules.Add(module);
            OnStateModuleRegistered?.Invoke(module);
        }

        public void DeregisterStateModule(IStateModule module)
        {
            _worldstateSyncableModules.Remove(module);
            OnStateModuleDeregistered?.Invoke(module);  
        }

        private ILocalPlayerRig _localPlayerRig;
        public ILocalPlayerRig LocalPlayerRig {
            get => _localPlayerRig;
            set {
                _localPlayerRig = value;

                if (value != null)
                    OnLocalPlayerRigRegistered?.Invoke(value);
                else 
                    OnLocalPlayerRigDeregistered?.Invoke(value);
            }
        }
        public event Action<ILocalPlayerRig> OnLocalPlayerRigRegistered;
        public event Action<ILocalPlayerRig> OnLocalPlayerRigDeregistered;


        private void Awake()
        {
            _instance = this;
            //gameObject.hideFlags = HideFlags.HideInHierarchy; //To hide
            gameObject.hideFlags &= ~HideFlags.HideInHierarchy; //To show
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

    public interface IPlayerSpawner
    {
        public bool IsEnabled { get; }
        public string GameObjectName { get; }

        //Unlike the other components, the player spawner should be able to 
        //Activate and deactivate at runtime
        public event Action OnEnabledStateChanged;
    }
}
