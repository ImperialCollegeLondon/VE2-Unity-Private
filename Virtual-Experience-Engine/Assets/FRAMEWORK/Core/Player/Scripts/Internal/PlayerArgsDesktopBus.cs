using UnityEngine;
using UnityEngine.SceneManagement;
using static VE2.Common.CommonSerializables;

/// <summary>
/// If on desktop, and about to go to a new world, update the bus with the infomation to pass to that scene 
/// Of on android, start the intent for the new world with the settings as arguments 
// /// </summary>
// public class PlayerArgsDesktopBus : MonoBehaviour //TODO: move into PlayerSettingsHandler, maybe rename PlayerPersistentDataHandler
// {
//     public static PlayerArgsDesktopBus _instance;
//     public static PlayerArgsDesktopBus Instance
//     {
//         get
//         {
//             if (_instance == null)

//                 _instance = FindFirstObjectByType<PlayerArgsDesktopBus>();
        
//             if (_instance == null)
//             {
//                 GameObject go = new GameObject($"PlayerArgsDesktopBus{SceneManager.GetActiveScene().name}");
//                 _instance = go.AddComponent<PlayerArgsDesktopBus>();
//             }

//             return _instance;
//         }
//     }

//     [SerializeField] public bool HasArgs = false;
//     [SerializeField, HideIf(nameof(HasArgs), false)] public PlayerPresentationConfig PlayerPresentationConfig;
//     [SerializeField] public bool RememberPlayerSettings = false;

//     private void Awake()
//     {
//         DontDestroyOnLoad(this);
//     }
// }

/*

    We can't let the player settings be changed when outside the hub - because they wont save to a player prefs file...
    We need some shared settings file, really... but that's a bit of a pain to implement
    We'll just show a warning to say "settings wont be saved until you return to the hub"

*/


/*
    Is there any reason for this arguments bus thing to live outside the player settings handler?
    I don't think so - before, we thought we'd have e.g platform and instancing talk to the settings handler, meaning it had to be in the scene at edit time 
    But now we've reworked service init, they can all just go through the service 
    Default settings live on the actual service monobehaviour anyway - which only creates a new settings handler at runtime if there isn't already one present

    We do need to be able to expose the arg names through the API 
    Arg names could just live in a different class though, in API? 
    platform needs arg names...
    Maybe not... maybe the platform can just pass the "intent" directly to the PlayerService (and thus the PlayerSettingsHandler)
    */