using TMPro;
using UnityEngine;

public class AdjustableValueFeeder : MonoBehaviour
{
    [SerializeField] private bool roundValue = true;
    private TMP_Text _text => GetComponent<TMP_Text>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void OnEnable()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void UpdateValue(float value)
    {
        if(roundValue)
            _text.text = "Output Value: " + value.ToString("N0");
        else
            _text.text = "Output Value: " + value.ToString("F2");
    }
}
