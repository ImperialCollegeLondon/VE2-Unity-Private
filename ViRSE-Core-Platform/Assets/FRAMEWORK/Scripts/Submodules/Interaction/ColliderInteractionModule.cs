using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class ColliderInteractionModule
{
    [VerticalGroup("ColliderInteractionModule_VGroup")]
    [FoldoutGroup("ColliderInteractionModule_VGroup/Collider Interaction Settings")]
    [SerializeField] private bool test;

    public UnityEvent<InteractorID> OnCollideEnter { get; private set; } = new UnityEvent<InteractorID>();


    public void InvokeOnCollideEnter(InteractorID id)
    {
        OnCollideEnter.Invoke(id);
    }
}
