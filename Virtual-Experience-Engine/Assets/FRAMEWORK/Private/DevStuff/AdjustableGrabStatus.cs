using TMPro;
using UnityEngine;
using VE2.Core.VComponents.API;

public class AdjustableGrabStatus : MonoBehaviour
{
    private TMP_Text _text => GetComponent<TMP_Text>();
    [SerializeField] private GameObject Adjustable;
    private IV_LinearAdjustable _linearAdjustable => Adjustable.GetComponent<IV_LinearAdjustable>();
    private IV_RotationalAdjustable _rotationalAdjustable => Adjustable.GetComponent<IV_RotationalAdjustable>();

    void Update()
    {
        if (_linearAdjustable != null && _linearAdjustable.IsGrabbed)
        {
            if (_linearAdjustable.IsLocallyGrabbed)
                UpdateValue("Grabbed Locally", Color.green);
            else
                UpdateValue("Grabbed Remotely", Color.yellow);
        }
        else if (_rotationalAdjustable != null && _rotationalAdjustable.IsGrabbed)
        {
            if (_rotationalAdjustable.IsLocallyGrabbed)
                UpdateValue("Grabbed Locally", Color.green);
            else
                UpdateValue("Grabbed Remotely", Color.yellow);
        }
        else
        {
            UpdateValue("Not Grabbed", Color.white);
        }
    }

    public void UpdateValue(string status, Color color)
    {
        _text.text = $"Grab Status: \n{status}";
        _text.color = color;
    }
}
