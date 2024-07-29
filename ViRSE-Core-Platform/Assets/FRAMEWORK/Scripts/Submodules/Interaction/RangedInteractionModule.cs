using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

//TODO rename to RangedInteractionModuleBase
//VCs will only ever have 0 or 1 modules that are derived from this 
//Any ranged interaction module will ALWAYS want these settings, so throw it in the base 
[AllowGUIEnabledForReadonly]
public class RangedInteractionModule : MonoBehaviour
{
    [VerticalGroup("RangedInteractionModule_VGroup")]
    [SerializeField]
    [SuffixLabel("metres")]
    private float interactionRange = 2.0f;

    [VerticalGroup("RangedInteractionModule_VGroup")]
    public UnityEvent OnLocalHoverEnter;

    [VerticalGroup("RangedInteractionModule_VGroup")]
    public UnityEvent OnLocalHoverExit;

    public bool IsInRangeFromPosition(Vector3 position) => (Vector3.Distance(position, transform.position) < interactionRange);
        //FAor grabbables, these need to be calclated based off attach point too...
        //So the grabbable interaction module has to override this 
        //The override makes me nervous, is that a sign our inheritance is wrong?
        //To be fair, really it's extending rather than modifying....
        //..the base class (this) can check disatance to transform 


        //Or the VC could justa return grab range, and the interactor could use the hit point 
        //We also need the VC to return 
        //Haptics
        //Tooltips 
        //admin only 
        //Maybe a separate "is allowed to interact"

        //are we overthinking all of this - maybe just from transform position is fine?? How long are things really gna be 
    

    public void OnLocalInteractorHoverEnter()
    {

    }

    public void OnLocalInteractorHoverExit()
    {

    }

    public void TearDown()
    {
        DestroyImmediate(this);
    }
}
