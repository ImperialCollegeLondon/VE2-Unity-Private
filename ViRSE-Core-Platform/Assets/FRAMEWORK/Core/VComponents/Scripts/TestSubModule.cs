using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class TestSubModule
{
    public int testInt;
    [SerializeField] private int testInt2;

    //[SerializeField, ShowInInspector, HideLabel]
    private TestSubSubModule testSubSubModule = null;


    //[InfoBox("This is some info!")]
    public UnityEvent OnTest = new();

    public TestSubModule()
    {
        Debug.Log("Test sub mod");
        testInt = 3;
        testInt2 = 4;

        testSubSubModule = new();
    }

    public TestSubModule(string testString)
    {
        Debug.Log("Made module - " + testString);
    }
}

[Serializable]
public class TestSubSubModule
{
    public int testSubInt;
    [SerializeField] private int testSubInt2;


    //[InfoBox("This is some info!")]
    public UnityEvent OnTest = new();

    public TestSubSubModule()
    {
        Debug.Log("Test sub SUB mod");
        testSubInt = 3;
        testSubInt2 = 4;
    }
}
