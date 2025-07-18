using UnityEngine;
using VE2.Core.VComponents.API;

public class ColorSwitcher : MonoBehaviour
{
    [SerializeField] private GameObject Adjustable;
    private IV_SlidingAdjustable _adjustable => Adjustable.GetComponent<IV_SlidingAdjustable>();
    private Light _light => GetComponent<Light>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(_adjustable.IsGrabbed)
        {
            if(_adjustable.IsLocallyGrabbed)
                _light.color = Color.green;
            else
                _light.color = Color.yellow;
        }
        else
        {
            _light.color = Color.white;
        }
    }
}
