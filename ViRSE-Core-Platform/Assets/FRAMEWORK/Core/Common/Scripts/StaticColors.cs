using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StaticColors : MonoBehaviour //TODO - this should probably be a ScriptableObject
{
    private static StaticColors _instance = null;
    public static StaticColors Instance {
        get {
            if (_instance == null)
                _instance = FindObjectOfType<StaticColors>();

            return _instance;
        }
        private set {
            _instance = value;
        }
    }

    [SerializeField] public Color lightBlue;
    [SerializeField] public Color processBlue;
    [SerializeField] public Color imperialBlue;
    [SerializeField] public Color navyBlue;
    [SerializeField] public Color virseGreen;
    [SerializeField] public Color tangerine;

    private void Awake()
    {
        Instance = this;
    }
}
