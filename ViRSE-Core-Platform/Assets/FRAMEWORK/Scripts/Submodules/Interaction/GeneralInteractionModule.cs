using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//TODO, maybe rename to e.g GeneralInteractionConfig?
//We DON'T want this to be the superclass of all interaction modules
//We want each VC to only have one of these, although it may want multiple interaction modules 
//We may want to have the VC inject this GeneralInteractionConfig into the interaction modules
//Or the interaction modules may not even need this data! We'll have to see
[Serializable]
public class GeneralInteractionModule
{ 
    [VerticalGroup("GeneralInteractionModule_VGroup")]
    [FoldoutGroup("GeneralInteractionModule_VGroup/General Interaction Settings")]
    [SerializeField] private bool enableControllerVibrations = true;
    [HideInInspector] public bool EnableControllerVibrations => enableControllerVibrations;

    [FoldoutGroup("GeneralInteractionModule_VGroup/General Interaction Settings")]
    [SerializeField] private bool showTooltipsAndHighlight = true;
    [HideInInspector] public bool ShowTooltipsAndHighlight => showTooltipsAndHighlight;
}
