using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static VE2.NonCore.Platform.API.PlatformPublicSerializables;

internal class HubCategoryPageView : MonoBehaviour
{
    [SerializeField] private Button _backButton;
    [SerializeField] private Image _categoryIcon;
    [SerializeField] private TMP_Text _categoryTitle;
    [SerializeField] private VerticalLayoutGroup _verticalWorldsGroup;
    [SerializeField] private GameObject _horizontalWorldsGroupPrefab;

    [SerializeField] private GameObject _worldButtonPrefab;

    private const int MAX_CATEGORIES_PER_ROW = 4;

    public event Action<HubWorldDetails> OnWorldClicked;
    public event Action OnBackClicked;

    public void SetupView(WorldCategory worldCategory)
    {
        _backButton.onClick.AddListener(() => OnBackClicked?.Invoke());
        _categoryTitle.text = worldCategory.CategoryName;
        //_categoryIcon.sprite = worldCategory.CategoryIcon; //TODO!

        if (worldCategory.Worlds.Count == 0)
        {
            Debug.LogError("Opened categvory has no worlds!");
            return;
        }

        Debug.Log($"Setting up category view for {worldCategory.CategoryName} with {worldCategory.Worlds.Count} worlds.");

        GameObject horizontalCategoriesGroup = null;

        for (int i = 0; i < worldCategory.Worlds.Count; i++)
        {
            if (i % MAX_CATEGORIES_PER_ROW == 0)
                horizontalCategoriesGroup = Instantiate(_horizontalWorldsGroupPrefab, _verticalWorldsGroup.transform);

            GameObject worldButton = Instantiate(_worldButtonPrefab, horizontalCategoriesGroup.transform);
            CreateWorldView(worldCategory.Worlds[i], worldButton.GetComponent<HubWorldButtonView>());
        }
    }

    private void CreateWorldView(HubWorldDetails worldDetails, HubWorldButtonView worldView)
    {
        worldView.SetupView(worldDetails);
        worldView.OnWorldClicked += OnWorldClicked;
    }
}
