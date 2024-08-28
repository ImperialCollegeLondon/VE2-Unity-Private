using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestVC : MonoBehaviour
{
    //[SerializeField, ShowInInspector, HideLabel]
    private TestSubModule testSubModule;

    //[OnInspectorInit]
    private void Reset()
    {
        if (testSubModule == null)
        {
            Debug.Log("Test sub is null");
            testSubModule = new TestSubModule("test!!!");
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}


//public class TestSubModule
//{
//    public int testInt;
//}
