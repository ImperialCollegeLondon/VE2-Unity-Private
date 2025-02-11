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

*/
public class PlayerLocator : MonoBehaviour
{
    private static PlayerLocator _instance;
    public static PlayerLocator Instance
    { //Reload-proof singleton
        get
        {
            if (_instance == null)
                _instance = FindFirstObjectByType<PlayerLocator>();

            if (_instance == null && !Application.isPlaying)
                _instance = new GameObject($"PlayerLocator{SceneManager.GetActiveScene().name}").AddComponent<PlayerLocator>();

            return _instance;
        }
    }

    //TODO: Don't expose this publicly - it's only the player that needs it(?)
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

    //TODO: Remove this - can just live on the V_PlayerSpawner inspector, no need for a separate mono
    [SerializeField, HideInInspector] public string PlayerOverridesProviderGOName;
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

    public PlayerStateModuleContainer PlayerStateModuleContainer { get; private set; } = new();
    public InteractorContainer InteractorContainer { get; private set; } = new();

    private void Awake()
    {
        _instance = this;
        gameObject.hideFlags = HideFlags.HideInHierarchy; //To hide
       //gameObject.hideFlags &= ~HideFlags.HideInHierarchy; //To show
    }

    private void OnDestroy()
    {
        PlayerStateModuleContainer.Reset();
    }
}

public class PlayerStateModuleContainer : BaseStateModuleContainer
{
    public IPlayerStateModule PlayerStateModule { get; private set; }
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