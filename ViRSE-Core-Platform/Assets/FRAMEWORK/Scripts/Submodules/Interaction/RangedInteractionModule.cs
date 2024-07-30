using Sirenix.OdinInspector;
using System;
using UnityEngine;
using UnityEngine.Events;

//VCs will only ever have 0 or 1 modules that are derived from this 
//Any ranged interaction module will ALWAYS want these settings, so throw it in this base class
[Serializable]
public class RangedInteractionModule : IRangedInteractionModule
{
    [SerializeField, HideInInspector] private GameObject gameObject;

    [VerticalGroup("RangedInteractionModule_VGroup")]
    [FoldoutGroup("RangedInteractionModule_VGroup/Ranged Interaction Settings")]
    [SuffixLabel("metres")]
    [SerializeField] private float interactionRange;

    [PropertySpace(SpaceBefore = 5)]
    [FoldoutGroup("RangedInteractionModule_VGroup/Ranged Interaction Settings")]
    [SerializeField] private UnityEvent OnLocalHoverEnter;

    [FoldoutGroup("RangedInteractionModule_VGroup/Ranged Interaction Settings")]
    [SerializeField] private UnityEvent OnLocalHoverExit;

    public bool IsPositionWithinInteractRange(Vector3 position) => (Vector3.Distance(position, gameObject.transform.position) < interactionRange);

    //public bool IsInRangeFromPosition(Vector3 position) => (Vector3.Distance(position, transform.position) < interactionRange);
    //FAor grabbables, these need to be calclated based off attach point too...
    //So the grabbable interaction module has to override this 
    //The override makes me nervous, is that a sign our inheritance is wrong?
    //To be fair, really it's extending rather than modifying....
    //..the base class (this) can check disatance to transform 
    //FOr the grabbable version, we can actually just pass down the attach point


    //Or the VC could justa return grab range, and the interactor could use the hit point 
    //We also need the VC to return 
    //Haptics
    //Tooltips 
    //admin only 
    //Maybe a separate "is allowed to interact"

    //are we overthinking all of this - maybe just from transform position is fine?? How long are things really gna be 

    public RangedInteractionModule(GameObject gameObject)
    {
        this.gameObject = gameObject;
        interactionRange = 5;
    }

    public void OnLocalInteractorHoverEnter()
    {

    }

    public void OnLocalInteractorHoverExit()
    {

    }
}
