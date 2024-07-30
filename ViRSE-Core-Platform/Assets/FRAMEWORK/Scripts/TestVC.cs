using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestVC : MonoBehaviour
{
    [SerializeField, ShowInInspector, HideLabel]
    private TestSubModule testSubModule;

    [OnInspectorInit]
    private void CreateData()
    {
        if (testSubModule == null)
        {
            testSubModule = new TestSubModule();
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
