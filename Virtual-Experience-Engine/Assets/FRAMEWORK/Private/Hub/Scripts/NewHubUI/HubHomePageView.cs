using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static VE2.NonCore.Platform.API.PlatformPublicSerializables;

internal class HubHomePageView : MonoBehaviour
{
    [SerializeField] private HubWorldButtonView[] _suggestedWorldViews = new HubWorldButtonView[5];
    [SerializeField] private Button _navigateCategoriesLeftButton;
    [SerializeField] private Button _navigateCategoriesRightButton;
    [SerializeField] private VerticalLayoutGroup _verticalCategoriesGroup;

    [SerializeField] private GameObject _horizontalCategoriesGroupPrefab;
    [SerializeField] private GameObject _categoriesButtonPrefab;

    private const int MAX_CATEGORIES_PER_ROW = 4;
    private const int MAX_CATEGORY_ROWS = 2;

    public event Action<WorldDetails> OnWorldClicked;
    public event Action<WorldCategory> OnCategoryClicked;

    void SetupView(WorldDetails[] suggestedWorldDetails, List<WorldCategory> worldCategories)
    {
        if (suggestedWorldDetails.Length == 0)
        {
            Debug.LogWarning("No suggested worlds provided!");
            return;
        }

        for (int i = 0; i < _suggestedWorldViews.Length && i < suggestedWorldDetails.Length; i++)
            CreateWorldView(suggestedWorldDetails[i], _suggestedWorldViews[i]);

        //TODO
        _navigateCategoriesLeftButton.interactable = false;
        _navigateCategoriesRightButton.interactable = false;

        if (worldCategories.Count == 0)
        {
            Debug.LogWarning("No world categories provided!");
            return;
        }

        GameObject horizontalCategoriesGroup1 = Instantiate(_horizontalCategoriesGroupPrefab, _verticalCategoriesGroup.transform);
        GameObject horizontalCategoriesGroup2 = worldCategories.Count >= MAX_CATEGORIES_PER_ROW ?
            Instantiate(_horizontalCategoriesGroupPrefab, _verticalCategoriesGroup.transform) : null;

        for (int i = 0; i < worldCategories.Count; i++)
        {
            if (i >= MAX_CATEGORIES_PER_ROW * 2) //TODO!
                break;

            GameObject horizontalGroup = i < MAX_CATEGORIES_PER_ROW ? horizontalCategoriesGroup1 : horizontalCategoriesGroup2;

            GameObject categoryButton = Instantiate(_categoriesButtonPrefab, horizontalGroup.transform);
            CreateCategoryView(worldCategories[i], categoryButton.GetComponent<HubCatagoryButtonView>());
        }
    }

    private void CreateWorldView(WorldDetails worldDetails, HubWorldButtonView worldView)
    {
        worldView.SetupView(worldDetails);
        worldView.OnWorldClicked += OnWorldClicked;
    }
    
    private void CreateCategoryView(WorldCategory categoryDetails, HubCatagoryButtonView categoryView)
    {
        categoryView.SetupView(categoryDetails);
        categoryView.OnCategoryClicked += OnCategoryClicked;
    }
}

internal class WorldCategory
{
    public readonly string CategoryName;
    public readonly WorldDetails[] Worlds;

    public WorldCategory(string categoryName, WorldDetails[] worlds)
    {
        CategoryName = categoryName;
        Worlds = worlds;
    }
}
