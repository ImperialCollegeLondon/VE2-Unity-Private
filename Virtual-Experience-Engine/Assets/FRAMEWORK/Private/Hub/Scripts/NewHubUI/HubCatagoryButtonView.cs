using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

internal class HubCatagoryButtonView : MonoBehaviour
{
    [SerializeField] private Button _categoryButton;
    [SerializeField] private TMP_Text _categoryNameText;
    [SerializeField] private Image _categoryIcon;

    public event Action<WorldCategory> OnCategoryClicked;

    private WorldCategory _categoryDetails;

    public void SetupView(WorldCategory categoryDetails)
    {
        _categoryDetails = categoryDetails;

        _categoryNameText.text = categoryDetails.CategoryName;
        //_categoryIcon.sprite = categoryDetails.Icon; TODO!

        _categoryButton.onClick.AddListener(() => OnCategoryClicked?.Invoke(_categoryDetails));
    }
}
