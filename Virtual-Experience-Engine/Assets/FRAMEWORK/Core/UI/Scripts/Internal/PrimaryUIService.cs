using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem.UI;
using VE2.Core.Common;
using VE2.Core.Player.API;
using VE2.Core.UI.API;

namespace VE2.Core.UI.Internal
{
    internal class PrimaryUIService : IPrimaryUIServiceInternal
    {
        #region Public Interfaces
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

        #region Internal Interfaces     
        public void SetPlatformQuickpanel(GameObject platformQuickPanel) 
        {
            UIUtils.MovePanelToFillRect(platformQuickPanel.GetComponent<RectTransform>(), _platformQuickPanelHolder.GetComponent<RectTransform>());
            platformQuickPanel.SetActive(true);
            _platformPromoPanel.SetActive(false);
        }
        #endregion

        private readonly IPressableInput _onToggleUIPressed;
        private readonly InputSystemUIInputModule _UIInputModule;

        private readonly GameObject _primaryUIHolderGameObject;
        private readonly GameObject _primaryUIGameObject;
        private readonly PrimaryUICenterPanelHandler _centerPanelHandler;
        private readonly GameObject _platformQuickPanelHolder;
        private readonly GameObject _platformPromoPanel;

        public PrimaryUIService(IPressableInput onToggleUIPressed, InputSystemUIInputModule uiInputModule)
        {
            _onToggleUIPressed = onToggleUIPressed;
            _onToggleUIPressed.OnPressed += HandleToggleUIPressed;

            _UIInputModule = uiInputModule;
            _UIInputModule.cursorLockBehavior = InputSystemUIInputModule.CursorLockBehavior.OutsideScreen;

            _primaryUIHolderGameObject = GameObject.Instantiate(Resources.Load<GameObject>("PrimaryUIHolder"));
            GameObject primaryUIGO = _primaryUIHolderGameObject.transform.GetChild(0).gameObject;
            primaryUIGO.SetActive(false);

            PrimaryUIReferences primaryUIReferences = primaryUIGO.GetComponent<PrimaryUIReferences>();  
            _primaryUIGameObject = primaryUIReferences.PrimaryUI;
            _centerPanelHandler = new PrimaryUICenterPanelHandler(primaryUIReferences.CenterPanelUIReferences);
            _platformQuickPanelHolder = primaryUIReferences.PlatformQuickPanelHolder;
            _platformPromoPanel = primaryUIReferences.PlatformPromoPanel;

            primaryUIReferences.CloseButton.onClick.AddListener(HandleCloseButtonPressed);
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
}
