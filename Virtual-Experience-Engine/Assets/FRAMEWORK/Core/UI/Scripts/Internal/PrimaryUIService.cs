using System;
using System.Collections.Generic;
using UnityEngine;
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
        }
        public event Action OnUIShow;
        public event Action OnUIHide;
        public void HidePrimaryUI() 
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;   
            _primaryUIGameObject.SetActive(false);   
        }

        public void MoveUIToCanvas(Canvas canvas)
        {
            UIUtils.MovePanelToFillRect(_primaryUIGameObject.GetComponent<RectTransform>(), canvas.GetComponent<RectTransform>());
        }

        public void AddNewTab(GameObject tab, string tabName, IconType iconType) => _centerPanelHandler.AddNewTab(tab, tabName, iconType);
        #endregion

        private readonly IPressableInput _onToggleUIPressed;
        private readonly GameObject _primaryUIGameObject;
        private readonly CenterPanelHandler _centerPanelHandler;

        public PrimaryUIService(IPressableInput onToggleUIPressed)
        {
            //GameObject primaryUIGO = GameObject.Instantiate(Resources.Load<GameObject>("PrimaryUIHolder").transform.GetChild(0).gameObject);
            GameObject primaryUIGOCanvas = GameObject.Instantiate(Resources.Load<GameObject>("PrimaryUIHolder"));
            GameObject primaryUIGO = primaryUIGOCanvas.transform.GetChild(0).gameObject;
            //primaryUIGO.SetActive(false);

            PrimaryUIReferences primaryUIReferences = primaryUIGO.GetComponent<PrimaryUIReferences>();  
            _primaryUIGameObject = primaryUIReferences.PrimaryUI;
            _centerPanelHandler = new CenterPanelHandler(primaryUIReferences.CenterPanelUIReferences);

            _onToggleUIPressed = onToggleUIPressed;
            _onToggleUIPressed.OnPressed += HandleToggleUIPressed;

            Button test = primaryUIGO.GetComponent<Button>();  
            test.onClick.AddListener(() => HandleTabPressed(0));
        }

        private void HandleTabPressed(int tabID)
        {
            //TODO - handle tab pressed
        }

        internal void HandleUpdate() 
        {

        }

        private void HandleToggleUIPressed()
        {
            if (_primaryUIGameObject.activeSelf)
            {
                HidePrimaryUI();
                OnUIHide?.Invoke();
            }
            else
            {
                ShowUI();
                OnUIShow?.Invoke();
            }
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

        private readonly List<V_ColorAssignment> _tabColorHandlers = new();
        private readonly List<GameObject> _tabPanels = new();

        private int _currentTab = 0;

        public void AddNewTab(GameObject newTab, string tabName, IconType iconType) //TODO - perhaps wants to pass intended tab position?
        {
            _tabPanels.Add(newTab);

            GameObject newTabButton = GameObject.Instantiate(TabPrefab, TabLayoutGroup.transform);
            newTabButton.GetComponentInChildren<Text>().text = tabName;
            newTabButton.GetComponent<Button>().onClick.AddListener(() => HandleTabPressed(_tabPanels.Count - 1));

            _tabColorHandlers.Add(newTabButton.GetComponent<V_ColorAssignment>());

            //TODO - handle icon... maybe the consumer should just pass a sprite?

            UIUtils.MovePanelToFillRect(newTab.GetComponent<RectTransform>(), MainContentPanel);
        }

        internal CenterPanelHandler(CenterPanelUIReferences centerPanelUIReferences)
        {
            TabLayoutGroup = centerPanelUIReferences.TabLayoutGroup;
            TabPrefab = centerPanelUIReferences.TabPrefab;
            MainContentPanel = centerPanelUIReferences.MainContentPanel;
        }

        private void HandleTabPressed(int tabID)
        {
            _tabPanels[_currentTab].SetActive(false);
            _tabPanels[tabID].SetActive(true);
            _currentTab = tabID;

            //TODO - change colors of tabs
        }
            
    }
}
