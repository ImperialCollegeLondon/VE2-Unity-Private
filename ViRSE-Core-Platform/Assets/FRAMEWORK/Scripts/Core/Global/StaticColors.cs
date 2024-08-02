using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StaticColors : MonoBehaviour
{
    public static StaticColors instance;

    [SerializeField] public Color lightBlue;
    [SerializeField] public Color processBlue;
    [SerializeField] public Color imperialBlue;
    [SerializeField] public Color navyBlue;
    [SerializeField] public Color virseGreen;
    [SerializeField] public Color tangerine;

    private void Awake()
    {
        instance = this;
    }
}
