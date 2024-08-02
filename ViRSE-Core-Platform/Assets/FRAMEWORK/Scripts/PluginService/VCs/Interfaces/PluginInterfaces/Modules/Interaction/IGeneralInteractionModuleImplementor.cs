using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IGeneralInteractionModuleImplementor 
{
    protected IGeneralInteractionModule module { get; } //Not visible to customer 

    public bool AdminOnly {
        get {
            return module.AdminOnly;
        }
        set {
            module.AdminOnly = value;
        }
    }
}

public interface IGeneralInteractionModule
{
    public bool AdminOnly { get; set; }
}
