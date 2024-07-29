using Sirenix.OdinInspector.Editor;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[AllowGUIEnabledForReadonly]
public class ColliderInteractionModule : MonoBehaviour
{
    public bool test;
    public UnityEvent<InteractorID> OnCollideEnter { get; private set; } = new UnityEvent<InteractorID>();

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void InvokeOnCollideEnter(InteractorID id)
    {
        OnCollideEnter.Invoke(id);
    }

    //It should probably be the actual interactor detecting this???
    //Yeah, because the interactor has to know if it should vibrate anyway

    //Or maybe it should be OnTriggerEnter
    private void OnCollisionEnter(Collision collision)
    {
        
    }

    public void TearDown()
    {
        DestroyImmediate(this);
    }

}
