using TMPro;
using UnityEngine;

public class AdjustableValueFeeder : MonoBehaviour
{
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
        _text.text = value.ToString("N0");
    }
}
