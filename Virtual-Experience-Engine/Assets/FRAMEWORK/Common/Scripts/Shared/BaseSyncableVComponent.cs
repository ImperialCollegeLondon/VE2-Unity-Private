using UnityEngine;

internal class BaseSyncableVComponent : MonoBehaviour
{
    [SerializeField, HideInInspector] protected GameObjectIDWrapper _idWrapper = new();

    protected virtual void FixedUpdate()
    {
        if (!_idWrapper.HasBeenSetup)
        {
            _idWrapper.ID = "FreeGrabbable-" + gameObject.name;
            _idWrapper.HasBeenSetup = true;
        }
    }
}
