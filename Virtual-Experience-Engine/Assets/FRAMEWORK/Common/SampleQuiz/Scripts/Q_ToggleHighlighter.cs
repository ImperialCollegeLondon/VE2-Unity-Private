
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Q_ToggleButtonHighlighter : MonoBehaviour
{
    //public Image backgroundImage;
    public GameObject onIndicator;
    public GameObject offIndicator;
    private bool isSelected = false;

    public bool changeToggleColour = true;

    //public Image switchImage;

    public void SetSelected(bool selected)
    {
        if (isSelected == selected)
            return;

        isSelected = selected;

        onIndicator.SetActive(selected);
        offIndicator.SetActive(!selected);

        if (changeToggleColour)
        {
            Color backgroundColour = selected ? Color.green : Color.blue;
            
            ColorBlock newButtonColor = new();
            newButtonColor.normalColor = backgroundColour;
            newButtonColor.highlightedColor = Color.magenta;
            newButtonColor.selectedColor = backgroundColour;
            newButtonColor.pressedColor = GetComponent<Toggle>().colors.pressedColor;
            newButtonColor.colorMultiplier = 1;

            GetComponent<Toggle>().colors = newButtonColor;
        }

        EventSystem.current.SetSelectedGameObject(null);

    }

    public bool IsSelected() => isSelected;
}
