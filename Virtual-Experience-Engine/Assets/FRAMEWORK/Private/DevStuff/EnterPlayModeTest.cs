using UnityEngine;

public class EnterPlayModeTest : MonoBehaviour
{
    [SerializeField] private bool _resetCounter;

    private static int _staticCounter = 0;
    private int _nonStaticCounter;

    //If you don't want to use a mono method
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init()
    {
        _staticCounter = 0;
    }

    void Awake() //Likely should use OnEnable for this instead... we'll see why in a moment...
    {
        if (_resetCounter)
            _staticCounter = 0; //statics MUST be reset when entering play mode when enter play mode settings are set to "disable domain reload"

        Debug.Log($"OnEnable called - static counter = {_staticCounter} - non static counter = {_nonStaticCounter}");
    }

    void Update()
    {
        _staticCounter++;
        _nonStaticCounter++;
    }

    void OnDestroy() 
    {
        Debug.Log($"OnDestroy called -  static counter = {_staticCounter} - non static counter = {_nonStaticCounter}");
    }
}