using UnityEngine;

public class ReloadTest : MonoBehaviour
{
    private static int _staticCounter = 0;

    private int _nonStaticCounter; //Not marked with [SerializeField], but will be serialized during hot reload anyway!
    [SerializeField] private int _serializedCounter = 0;

    void Awake() //Called when entering play mode for the first time, NOT when script is reloaded during play 
    {
        Debug.Log($"Awake called - static counter = {_staticCounter} - non static counter = {_nonStaticCounter} serialized counter = {_serializedCounter}");
    }
    
    void OnEnable()
    {
        _staticCounter = 0; //statics MUST be reset when entering play mode when enter play mode settings are set to "disable domain reload"
        //^ however, this static field will be reset when reloading the domain DURING playmode 
        Debug.Log($"OnEnable called - static counter = {_staticCounter} - non static counter = {_nonStaticCounter} serialized counter = {_serializedCounter}");
    }

    void Start() //Called when entering play mode for the first time, NOT when script is reloaded during play 
    {
        Debug.Log($"Start called - static counter = {_staticCounter} - non static counter = {_nonStaticCounter} serialized counter = {_serializedCounter}");
    }

    void Update()
    {
        _staticCounter++;
        _nonStaticCounter++;
        _serializedCounter++;
    }

    void OnDisable()
    {
        Debug.Log($"OnDisable called -  static counter = {_staticCounter} - non static counter = {_nonStaticCounter}, serialized counter = {_serializedCounter}");
    }

    void OnDestroy() //Called when exiting play mode, NOT when script is reloaded during play 
    {
        Debug.Log($"OnDestroy called -  static counter = {_staticCounter} - non static counter = {_nonStaticCounter}, serialized counter = {_serializedCounter}");
    }
}
