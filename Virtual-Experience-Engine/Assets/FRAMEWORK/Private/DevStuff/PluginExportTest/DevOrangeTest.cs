using UnityEngine;
using VE2.Common.Shared;
using VE2.Core.VComponents.API;

public class DevOrangeTest : MonoBehaviour
{
    [SerializeField] private InterfaceReference<IV_SlidingAdjustable> _linearAdjustable;

    void OnEnable()
    {
        _linearAdjustable.Interface.OnValueAdjusted.AddListener(OnAdjusted);
    }
    
    private void OnAdjusted(float value)
    {
        Debug.Log($"Adjusted value: {value}");
    }
}
