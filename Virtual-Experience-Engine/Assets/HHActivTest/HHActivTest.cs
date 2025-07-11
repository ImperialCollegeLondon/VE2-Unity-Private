using UnityEngine;

public class HHActivTest : MonoBehaviour
{
    public void HandleActivate()
    {
        // This method is called when the object is activated
        Debug.Log("HHActivTest OnActivate called");
    }

    public void HandleDeactivate()
    {
        // This method is called when the object is deactivated
        Debug.Log("HHActivTest OnDeactivate called");
    }
}
