using UnityEngine;

internal class BaseSyncableVComponent : MonoBehaviour
{
    [SerializeField, HideInInspector] protected GameObjectIDWrapper _idWrapper = new();

    protected string _vComponentID = "VComponent-";
    
    protected virtual void FixedUpdate()
    {
        if (!_idWrapper.HasBeenSetup)
        {
            _idWrapper.ID = _vComponentID + gameObject.name;
            _idWrapper.HasBeenSetup = true;
        }
    }
}

