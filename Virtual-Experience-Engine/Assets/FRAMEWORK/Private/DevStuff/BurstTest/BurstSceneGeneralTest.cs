using UnityEngine;
using VE2.Common.Shared;
using VE2.Core.VComponents.API;

public class BurstSceneGeneralTest : MonoBehaviour
{
    [SerializeField] InterfaceReference<IV_ToggleActivatable> toggleActivatable;
    [SerializeField] private GameObject goToActivate;

    private void Start()
    {
        toggleActivatable.Interface.OnActivate.AddListener(HandleActivate);
        toggleActivatable.Interface.OnDeactivate.AddListener(HandleDeactivate);
    }

    private void HandleActivate()
    {
        goToActivate.SetActive(true); ;
    }

    private void HandleDeactivate()
    {
        goToActivate.SetActive(false);
    }
}

