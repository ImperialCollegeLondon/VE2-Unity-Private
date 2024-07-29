using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//TODO, maybe rename to e.g GeneralInteractionConfig?
//We DON'T want this to be the superclass of all interaction modules
//We want each VC to only have one of these, although it may want multiple interaction modules 
//We may want to have the VC inject this GeneralInteractionConfig into the interaction modules
//Or the interaction modules may not even need this data! We'll have to see
[AllowGUIEnabledForReadonly]
[HideMonoScript]
public class GeneralInteractionModule : MonoBehaviour
{
    [VerticalGroup("GeneralInteractionModule_VGroup")]
    //[FoldoutGroup("GeneralInteractionModule_VGroup/GeneralInteractionModule")]
    [DisableIf("@isProgrammatic")]
    [SerializeField] protected bool enableControllerVibrations = true;

    [VerticalGroup("GeneralInteractionModule_VGroup")]
    [SerializeField] protected bool showTooltipsAndHighlight = true;

    public void TearDown()
    {
        DestroyImmediate(this);
    }
}
