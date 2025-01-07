using TMPro;
using UnityEngine;
using VE2.Core.VComponents.PluginInterfaces;

public class AdjustableGrabStatus : MonoBehaviour
{
    private TMP_Text _text => GetComponent<TMP_Text>();
    [SerializeField] private GameObject Adjustable;
    private IV_LinearAdjustable _adjustable => Adjustable.GetComponent<IV_LinearAdjustable>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void OnEnable()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(_adjustable.IsGrabbed)
        {
            if(_adjustable.IsLocallyGrabbed)
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
