using UnityEngine;
using VE2.Common;

public class VComponentBase : MonoBehaviour
{
    protected virtual void Reset()
    {
        //Kicks off the lazy init for the VCLocator instance
        var reference = VComponents_Locator.Instance;
    }
}
