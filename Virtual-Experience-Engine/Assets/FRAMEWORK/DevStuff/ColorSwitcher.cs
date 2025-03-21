using UnityEngine;
using VE2.Core.VComponents.API;

public class ColorSwitcher : MonoBehaviour
{
    [SerializeField] private GameObject Adjustable;
    private IV_LinearAdjustable _adjustable => Adjustable.GetComponent<IV_LinearAdjustable>();
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
