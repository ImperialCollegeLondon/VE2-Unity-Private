using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class GeneralInteractionModuleConfig
{ 
    [VerticalGroup("GeneralInteractionModule_VGroup")]
    [FoldoutGroup("GeneralInteractionModule_VGroup/General Interaction Settings")]
    [SerializeField] private bool enableControllerVibrations = true;
    [HideInInspector] public bool EnableControllerVibrations => enableControllerVibrations;

    [FoldoutGroup("GeneralInteractionModule_VGroup/General Interaction Settings")]
    [SerializeField] private bool showTooltipsAndHighlight = true;
    [HideInInspector] public bool ShowTooltipsAndHighlight => showTooltipsAndHighlight;
}
