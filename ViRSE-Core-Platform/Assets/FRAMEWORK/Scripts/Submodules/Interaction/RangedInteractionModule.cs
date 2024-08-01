using Sirenix.OdinInspector;
using System;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class RangedInteractionConfig
{
    //As a customer, how do I programmatically increase grab range
    //these could have script interfaces on them that let you change the data from the plugin 
    //If these are classes, you can even have events on them that the actual module listens to, like "OnPluginForceDrop"

    //The interactor CAN do GetComponent on the InteractionModule, because it IS a MonoBehaviour
    //
    [VerticalGroup("RangedInteractionModule_VGroup")]
    [FoldoutGroup("RangedInteractionModule_VGroup/Ranged Interaction Settings")]
    [SuffixLabel("metres")]
    [SerializeField] public float interactionRange = 5;

    [PropertySpace(SpaceBefore = 5)]
    [FoldoutGroup("RangedInteractionModule_VGroup/Ranged Interaction Settings")]
    [SerializeField] public UnityEvent OnLocalHoverEnter;

    [FoldoutGroup("RangedInteractionModule_VGroup/Ranged Interaction Settings")]
    [SerializeField] public UnityEvent OnLocalHoverExit;
}

//VCs will only ever have 0 or 1 modules that are derived from this 
//Any ranged interaction module will ALWAYS want these settings, so throw it in this base class
public class RangedInteractionModule : IRangedInteractionModule
{
    private RangedInteractionConfig rangedInteractionConfig;
    private GameObject gameObject;

    public RangedInteractionModule(RangedInteractionConfig rangedInteractionConfig, GameObject gameObject)
    {
        this.rangedInteractionConfig = rangedInteractionConfig;
        this.gameObject = gameObject;
    }

    public bool IsPositionWithinInteractRange(Vector3 position) => (Vector3.Distance(position, gameObject.transform.position) < rangedInteractionConfig.interactionRange);

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

    public void OnLocalInteractorHoverEnter()
    {

    }

    public void OnLocalInteractorHoverExit()
    {

    }
}
