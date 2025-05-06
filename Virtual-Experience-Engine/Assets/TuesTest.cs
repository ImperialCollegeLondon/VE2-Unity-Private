using UnityEngine;
using VE2.Common.Shared;
using VE2.Core.VComponents.API;

public class TuesTest : MonoBehaviour
{
    [SerializeField] InterfaceReference<IV_ToggleActivatable> toggleActivatable;

    void Start()
    {
        toggleActivatable.Interface.OnActivate.AddListener(OnActivated);
    }

    private void OnActivated()
    {
        Debug.Log($"Activated by...Is Local: {toggleActivatable.Interface.MostRecentInteractingClientID.IsLocal}");
    }
}
