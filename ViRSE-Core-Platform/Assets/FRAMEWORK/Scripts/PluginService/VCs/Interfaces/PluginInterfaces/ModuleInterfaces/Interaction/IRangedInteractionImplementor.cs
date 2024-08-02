using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IRangedInteractionModuleImplementor
{
    protected IRangedInteractionModule module { get; } //Not visible to customer 

    public float InteractRange {
        get {
            return module.InteractRange;
        }
        set {
            module.InteractRange = value;
        }
    }
}

public interface IRangedInteractionModule
{
    public float InteractRange { get; set; }
}
