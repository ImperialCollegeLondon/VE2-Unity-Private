using Sirenix.OdinInspector;
using System;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class RangedInteractionConfig
{
    [VerticalGroup("RangedInteractionModule_VGroup")]
    [FoldoutGroup("RangedInteractionModule_VGroup/Ranged Interaction Settings")]
    [SuffixLabel("metres")]
    [SerializeField] public float InteractionRange = 5;

    [PropertySpace(SpaceBefore = 5)]
    [FoldoutGroup("RangedInteractionModule_VGroup/Ranged Interaction Settings")]
    [SerializeField] public UnityEvent OnLocalHoverEnter;

    [FoldoutGroup("RangedInteractionModule_VGroup/Ranged Interaction Settings")]
    [SerializeField] public UnityEvent OnLocalHoverExit;
}

public class RangedInteractionModule : MonoBehaviour, IRangedPlayerInteractable, IRangedInteractionModule
{
    #region Plugin Interfaces
    public float InteractRange { get => _config.InteractionRange; set => _config.InteractionRange = value; }
    #endregion

    #region Player Rig Interfaces 
    public bool IsPositionWithinInteractRange(Vector3 position) => (Vector3.Distance(position, gameObject.transform.position) < _config.InteractionRange);
    #endregion

    private RangedInteractionConfig _config;
    private GeneralInteractionModule generalInteractionModule;

    public void Initialize(RangedInteractionConfig config) 
    {
        _config = config;
    }

    public void OnLocalInteractorHoverEnter()
    {

    }

    public void OnLocalInteractorHoverExit()
    {

    }
}
