using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using VE2.Common;
using VE2.Core.Common;
using VE2.Core.VComponents.InteractableInterfaces;
using static VE2.Common.CommonSerializables;

//What if we just put the IPlayer interface on this locator?? 
/*
    E.G for SetPosition, we call PlayerAPI.SetPosition(Vector3 position)
    PlayerAPI.SetPosition is static, and calls PlayerLocator.Instance.Player.SetPosition(Vector3 position)

    We'd need a public "SetPlayer" class... to make the initial link...
    Unless the ServiceLocator scane the entire scene?

    Lets stick to our current pattern
    At edit time, the V_PlayerSpawner will set the player on the locator 
    This setter grabs the GO off the interface and stores it in a private serialized field 
    The Locator's private Player Property has a lazy getter that returns the stored GO's interface if null 

    Yeah, lets reuse the outward facing interfaces on this PlayerLocator, "Facade Pattern"
*/
public class PlayerLocator : MonoBehaviour //TODO: Can't GONames be private??
{
    private static PlayerLocator _instance;
    private static PlayerLocator Instance
    { //Reload-proof singleton
        get
        {
            //if we've moved to a different scene, this will be null, so we can find the locator for the new scene
            if (_instance == null)
                _instance = FindFirstObjectByType<PlayerLocator>();

            if (_instance == null && !Application.isPlaying)
                _instance = new GameObject($"PlayerLocator{SceneManager.GetActiveScene().name}").AddComponent<PlayerLocator>();

            return _instance;
        }
    }

    public static IPlayerService Player => PlayerServiceProvider.PlayerService;

    [SerializeField, HideInInspector] public string PlayerServiceProviderGOName;
    private IPlayerServiceProvider _playerServiceProvder;
    internal static IPlayerServiceProvider PlayerServiceProvider
    {
        get
        {
            Debug.Log("Get PlayerServiceProvider");

            if (Instance._playerServiceProvder == null && !string.IsNullOrEmpty(Instance.PlayerServiceProviderGOName))
                Instance._playerServiceProvder = GameObject.Find(Instance.PlayerServiceProviderGOName)?.GetComponent<IPlayerServiceProvider>();

            return Instance._playerServiceProvder;
        }
        set //Will need to be called externally
        {
            Instance._playerServiceProvder = value;

            if (value != null)
                Instance.PlayerServiceProviderGOName = value.GameObjectName;
        }
    }

    // #region player interfaces
    // public static event Action<PlayerPresentationConfig> OnPlayerPresentationConfigChanged {
    //     add 
    //     {
    //         PlayerServiceProvider.PlayerService.OnPlayerPresentationConfigChanged += value;
    //     } 
    //     remove 
    //     {
    //         PlayerServiceProvider.PlayerService.OnPlayerPresentationConfigChanged -= value;
    //     }
    // }
    // public static PlayerPresentationConfig PlayerPresentationConfig => PlayerServiceProvider.PlayerService.PlayerPresentationConfig;
    // public static bool VRModeActive => PlayerServiceProvider.PlayerService.VRModeActive;
    // #endregion


    //TODO: Don't expose this publicly - it's only the player that needs it(?) Might be useful to have it here for testing though...
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

    public static bool HasMultiPlayerSupport => LocalClientIDProviderProvider != null;
    internal ILocalClientIDProvider WorldStateSyncService => LocalClientIDProviderProvider.LocalClientIDProvider;

    [SerializeField, HideInInspector] public string _playerSyncProviderGOName;
    private ILocalClientIDProviderProvider _playerSyncProvider;
    internal static ILocalClientIDProviderProvider LocalClientIDProviderProvider{
        get 
        {
            if (Instance._playerSyncProvider == null && !string.IsNullOrEmpty(Instance._playerSyncProviderGOName))
                Instance._playerSyncProvider = GameObject.Find(Instance._playerSyncProviderGOName)?.GetComponent<ILocalClientIDProviderProvider>();

            return Instance._playerSyncProvider;
        }
        set
        {
            Instance._playerSyncProvider = value;

            if (value != null)
                Instance._playerSyncProviderGOName = value.GameObjectName;
        }
    }

    private InteractorContainer _interactorContainer = new();
    public static InteractorContainer InteractorContainer { get => Instance._interactorContainer; private set => Instance._interactorContainer = value; }

    private void Awake()
    {
        _instance = this;
        gameObject.hideFlags = HideFlags.HideInHierarchy; //To hide
       //gameObject.hideFlags &= ~HideFlags.HideInHierarchy; //To show
    }

    private void OnDestroy()
    {
        InteractorContainer.Reset();
    }
}

public class InteractorContainer
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

//TODO: REMOVE!
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