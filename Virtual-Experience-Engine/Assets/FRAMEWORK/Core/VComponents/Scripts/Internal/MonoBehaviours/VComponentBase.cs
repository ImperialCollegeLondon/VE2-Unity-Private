using UnityEngine;
using VE2.Common;

public class VComponentBase : MonoBehaviour
{
    protected virtual void Reset()
    {
        //Kicks off the lazy init for the VCLocator instance
        //TODO: DOn't think we need this, locator can just be created once accessed
        //var reference = VComponents_Locator.WorldStateSyncService;
    }
}
