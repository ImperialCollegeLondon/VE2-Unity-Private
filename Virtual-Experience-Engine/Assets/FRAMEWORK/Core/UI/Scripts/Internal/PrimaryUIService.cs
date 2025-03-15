using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VE2.Core.Common;
using VE2.Core.Player.API;
using VE2.Core.UI.API;

namespace VE2.Core.UI.Internal
{
    internal class PrimaryUIService : IPrimaryUIService
    {
        #region Interfaces
        public bool IsShowing => _primaryUIGameObject.activeSelf;
        public void ShowUI() 
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            _primaryUIGameObject.SetActive(true);
            OnUIShow?.Invoke();
        }
        public event Action OnUIShow;
        public event Action OnUIHide;
        public void HideUI() 
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;   
            _primaryUIGameObject.SetActive(false);   
            OnUIHide?.Invoke();
        }

        public void MoveUIToCanvas(Canvas canvas)
        {
            UIUtils.MovePanelToFillRect(_primaryUIGameObject.GetComponent<RectTransform>(), canvas.GetComponent<RectTransform>());

            if (_primaryUIHolderGameObject != null)
                GameObject.Destroy(_primaryUIHolderGameObject);
        }

        public void AddNewTab(string tabName, GameObject tab, Sprite icon, int targetIndex) => _centerPanelHandler.AddNewTab(tabName, tab, icon, targetIndex);

        public void ShowTab(string tabName) => _centerPanelHandler.OpenTab(tabName);
        #endregion

        private readonly IPressableInput _onToggleUIPressed;
        private readonly GameObject _primaryUIHolderGameObject;
        private readonly GameObject _primaryUIGameObject;
        private readonly CenterPanelHandler _centerPanelHandler;

        public PrimaryUIService(IPressableInput onToggleUIPressed)
        {
            //GameObject primaryUIGO = GameObject.Instantiate(Resources.Load<GameObject>("PrimaryUIHolder").transform.GetChild(0).gameObject);
            _primaryUIHolderGameObject = GameObject.Instantiate(Resources.Load<GameObject>("PrimaryUIHolder"));
            GameObject primaryUIGO = _primaryUIHolderGameObject.transform.GetChild(0).gameObject;
            primaryUIGO.SetActive(false);

            PrimaryUIReferences primaryUIReferences = primaryUIGO.GetComponent<PrimaryUIReferences>();  
            _primaryUIGameObject = primaryUIReferences.PrimaryUI;
            _centerPanelHandler = new CenterPanelHandler(primaryUIReferences.CenterPanelUIReferences);

            primaryUIReferences.CloseButton.onClick.AddListener(HandleCloseButtonPressed);

            _onToggleUIPressed = onToggleUIPressed;
            _onToggleUIPressed.OnPressed += HandleToggleUIPressed;
        }

        internal void HandleUpdate() 
        {

        }

        private void HandleToggleUIPressed()
        {
            if (_primaryUIGameObject.activeSelf)
                HideUI();
            else
                ShowUI();
        }

        private void HandleCloseButtonPressed()
        {
            HideUI();
        }

        internal void TearDown()
        {
            _onToggleUIPressed.OnPressed -= HandleToggleUIPressed;
        }
    }

    internal class CenterPanelHandler 
    {
        private readonly HorizontalLayoutGroup TabLayoutGroup;
        private readonly GameObject TabPrefab;
        private readonly RectTransform MainContentPanel;

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
            UIUtils.MovePanelToFillRect(newTab.GetComponent<RectTransform>(), MainContentPanel);
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
            GameObject newTabButton = GameObject.Instantiate(TabPrefab, TabLayoutGroup.transform); 
            newTabButton.GetComponentInChildren<TMP_Text>().text = tabName;
            newTabButton.GetComponent<Button>().onClick.AddListener(() => 
            {
                OpenTab(tabName);
                EventSystem.current.SetSelectedGameObject(null);
            });
            
            Image buttonSubImage = newTabButton.GetComponentsInChildren<Image>(true)
                .FirstOrDefault(img => img.gameObject != newTabButton);
            buttonSubImage.sprite = icon;

            V_ColorAssignment tabColorHandler = newTabButton.GetComponent<V_ColorAssignment>();
            tabColorHandler.Setup(); //Starts inactive, so can't rely on Awake

            //Create a TabInfo to store ===
            TabInfo newTabInfo = new TabInfo(closestAvailableIndex, newTab, newTabButton, tabColorHandler);    
            _tabs.Add(tabName, newTabInfo);
            
            //Reshuffle tab buttons - Loop through tabs in ascending order, setting their position in the layout group
            TabInfo[] tabsByIndex = _tabs.Values.OrderBy(tab => tab.Index).ToArray(); 
            foreach (TabInfo tab in tabsByIndex)
                tab.TabButton.transform.SetSiblingIndex(tab.Index);

            Debug.Log("Adding tab: " + tabName + " index: " + closestAvailableIndex); // Should log correct indices 0, 1, 2, etc.

            if (_tabs.Values.Count == 1)
               OpenTab(tabName);
        }


        internal CenterPanelHandler(CenterPanelUIReferences centerPanelUIReferences)
        {
            TabLayoutGroup = centerPanelUIReferences.TabLayoutGroup;
            TabPrefab = centerPanelUIReferences.TabPrefab;
            MainContentPanel = centerPanelUIReferences.MainContentPanel;
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
            public readonly V_ColorAssignment TabColorHandler;

            public TabInfo(int index, GameObject tab, GameObject tabButton, V_ColorAssignment tabColorHandler)
            {
                Index = index;
                Tab = tab;
                TabButton = tabButton;
                TabColorHandler = tabColorHandler;
            }
        }  
    }
}
