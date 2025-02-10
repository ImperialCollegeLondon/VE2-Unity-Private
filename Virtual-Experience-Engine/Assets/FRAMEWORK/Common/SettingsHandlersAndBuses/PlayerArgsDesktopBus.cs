using UnityEngine;
using UnityEngine.SceneManagement;
using static VE2.Common.CommonSerializables;

/// <summary>
/// If on desktop, and about to go to a new world, update the bus with the infomation to pass to that scene 
/// Of on android, start the intent for the new world with the settings as arguments 
/// </summary>
public class PlayerArgsDesktopBus : MonoBehaviour //TODO: Move to service locator!
{
    public static PlayerArgsDesktopBus _instance;
    public static PlayerArgsDesktopBus Instance
    {
        get
        {
            if (_instance == null)

                _instance = FindFirstObjectByType<PlayerArgsDesktopBus>();
        
            if (_instance == null)
            {
                GameObject go = new GameObject($"PlayerArgsDesktopBus{SceneManager.GetActiveScene().name}");
                _instance = go.AddComponent<PlayerArgsDesktopBus>();
            }

            return _instance;
        }
    }

    [SerializeField] public bool HasArgs = false;
    [SerializeField, HideIf(nameof(HasArgs), false)] public PlayerPresentationConfig PlayerPresentationConfig;
    [SerializeField] public bool RememberPlayerSettings = false;

    private void Awake()
    {
        DontDestroyOnLoad(this);
    }
}

/*

    We can't let the player settings be changed when outside the hub - because they wont save to a file...
    We need some shared settings file, really... but that's a bit of a pain to implement
    We'll just show a warning to say "settings wont be saved until you return to the hub"

*/