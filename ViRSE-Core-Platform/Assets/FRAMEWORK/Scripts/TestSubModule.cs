using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class TestSubModule
{
    public int testInt;
    public int testInt2;
    public int testInt3;


    [InfoBox("This is some info!")]
    public UnityEvent OnTest = new();
}
