using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using VE2.Common;
using VE2.Core.Common;
using VE2.Core.VComponents.InteractableInterfaces;
using static VE2.Common.CommonSerializables;

public class PlayerAPI : MonoBehaviour 
{
    private static PlayerAPI _instance;
    private static PlayerAPI Instance
    { //Reload-proof singleton
        get
        {
            //if we've moved to a different scene, this will be null, so we can find the locator for the new scene
            if (_instance == null)
                _instance = FindFirstObjectByType<PlayerAPI>();

            if (_instance == null && !Application.isPlaying)
                _instance = new GameObject($"PlayerAPI{SceneManager.GetActiveScene().name}").AddComponent<PlayerAPI>();

            return _instance;
        }
    }

    public static IPlayerService Player => PlayerServiceProvider.PlayerService;

    [SerializeField, HideInInspector] private string PlayerServiceProviderGOName;
    private IPlayerServiceProvider _playerServiceProvder;
    internal static IPlayerServiceProvider PlayerServiceProvider
    {
        get
        {
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

    public static bool HasMultiPlayerSupport => LocalClientIDProvider != null && LocalClientIDProvider.IsEnabled;
    [SerializeField, HideInInspector] private string _playerSyncProviderGOName;
    private ILocalClientIDProvider _playerSyncProvider;
    internal static ILocalClientIDProvider LocalClientIDProvider{
        get 
        {
            if (Instance._playerSyncProvider == null && !string.IsNullOrEmpty(Instance._playerSyncProviderGOName))
                Instance._playerSyncProvider = GameObject.Find(Instance._playerSyncProviderGOName)?.GetComponent<ILocalClientIDProvider>();

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
    /// <summary>
    /// Contains all interactors (local or otherwise) in the scene, allows grabbables to perform validation on grab
    /// </summary>
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
