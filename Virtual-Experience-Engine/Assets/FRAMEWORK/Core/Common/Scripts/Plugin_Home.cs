using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VE2.Core.Common;
public class Plugin_Home : MonoBehaviour
{
    [Header("Feel free to add any variables your functions you want to this script so you can access them statically\n\n" +
    "This is NOT a framework-side script, so it CAN be changed :) ")]

    public static IVE2API ve2API;

    private void Awake()
    {
        ve2API = GetComponent<IVE2API>();
    }
}
