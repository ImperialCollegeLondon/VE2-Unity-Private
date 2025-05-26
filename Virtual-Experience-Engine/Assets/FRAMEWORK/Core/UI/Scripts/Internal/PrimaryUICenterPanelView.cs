using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VE2.Common.Shared;

namespace VE2.Core.UI.Internal
{
    [AddComponentMenu("")] // Prevents this MonoBehaviour from showing in the Add Component menu
    internal class PrimaryUICenterPanelView : MonoBehaviour
    {
        [SerializeField] private HorizontalLayoutGroup _tabLayoutGroup;
        [SerializeField] private GameObject _TabPrefab;
        [SerializeField] private RectTransform _MainContentPanel;

        private readonly Dictionary<string, TabInfo> _tabs = new();

        private string _currentTab = "none";

        public void AddNewTab(string tabName, GameObject newTab, Sprite icon, int targetIndex)
        {
            if (_tabs.ContainsKey(tabName))
            {
                Debug.LogError("Tab with name " + tabName + " already exists");
                return;
            }

            //Move the panel into its new holder
            CommonUtils.MovePanelToFillRect(newTab.GetComponent<RectTransform>(), _MainContentPanel);
            newTab.SetActive(false);

            //Calculate the closest available index for the new tab, will be targetIndex if available
            int[] usedTabIndices = _tabs.Values.Select(tab => tab.Index).ToArray();
            int closestAvailableIndex = -1; 

            if (!usedTabIndices.Contains(targetIndex))
            {
                closestAvailableIndex = targetIndex;
            }
            else 
            {
                foreach (int usedIndex in usedTabIndices)
                {
                    if (usedTabIndices.Contains(usedIndex + 1))
                    {
                        closestAvailableIndex = usedIndex + 1;
                        break;
                    }
                }
            }

            //Create the button for the tab ===
            GameObject newTabButton = GameObject.Instantiate(_TabPrefab, _tabLayoutGroup.transform); 
            newTabButton.name = $"{tabName} Tab Button";
            newTabButton.GetComponentInChildren<TMP_Text>().text = tabName;
            newTabButton.GetComponent<Button>().onClick.AddListener(() => 
            {
                OpenTab(tabName);
                EventSystem.current.SetSelectedGameObject(null);
            });
            
            Image buttonSubImage = newTabButton.GetComponentsInChildren<Image>(true)
                .FirstOrDefault(img => img.gameObject != newTabButton);
            buttonSubImage.sprite = icon;

            V_UIColorHandler tabColorHandler = newTabButton.GetComponent<V_UIColorHandler>();
            tabColorHandler.Setup(); //Starts inactive, so can't rely on Awake

            //Create a TabInfo to store ===
            TabInfo newTabInfo = new TabInfo(closestAvailableIndex, newTab, newTabButton, tabColorHandler);    
            _tabs.Add(tabName, newTabInfo);
            
            //Reshuffle tab buttons - Loop through tabs in ascending order, setting their position in the layout group ===
            TabInfo[] tabsByIndex = _tabs.Values.OrderBy(tab => tab.Index).ToArray(); 
            foreach (TabInfo tab in tabsByIndex)
                tab.TabButton.transform.SetSiblingIndex(tab.Index);

            //Open the tab if it is the only one ===
            if (_tabs.Values.Count == 1)
               OpenTab(tabName);
        }

        internal void OpenTab(string tabName)
        {
            if (_currentTab != "none")
            {
                _tabs[_currentTab].Tab.SetActive(false);
                _tabs[_currentTab].TabColorHandler.UnlockSelectedColor(); 
            }

            _tabs[tabName].Tab.SetActive(true);
            _tabs[tabName].TabColorHandler.LockSelectedColor();
            _currentTab = tabName;
        }

        private class TabInfo 
        {
            public readonly int Index;
            public readonly GameObject Tab;
            public readonly GameObject TabButton;
            public readonly V_UIColorHandler TabColorHandler;

            public TabInfo(int index, GameObject tab, GameObject tabButton, V_UIColorHandler tabColorHandler)
            {
                Index = index;
                Tab = tab;
                TabButton = tabButton;
                TabColorHandler = tabColorHandler;
            }
        }  
    }
}
