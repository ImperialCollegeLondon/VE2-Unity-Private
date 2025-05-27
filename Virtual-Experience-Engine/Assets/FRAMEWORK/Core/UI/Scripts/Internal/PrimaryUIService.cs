using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem.UI;
using VE2.Common.Shared;
using VE2.Core.Player.API;
using VE2.Core.UI.API;
using UnityEngine.SceneManagement;
using UnityEngine.Events;

namespace VE2.Core.UI.Internal
{
    internal class PrimaryUIService : IPrimaryUIServiceInternal //Acts as the "Controller" for the PrimaryUI Views 
    {
        #region Public Interfaces
        public bool IsShowing => _primaryUIGameObject.activeSelf;

        public UnityEvent OnUIShow => _menuUIConfig.OnActivateMainMenu;
        public UnityEvent OnUIHide => _menuUIConfig.OnDeactivateMainMenu;

        public void ShowUI() //TODO - cursor locking should move into the player
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            _primaryUIGameObject.SetActive(true);
            OnUIShow?.Invoke();
        }
        public void HideUI() 
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;   
            _primaryUIGameObject.SetActive(false);   
            OnUIHide?.Invoke();
        }

        public void MovePrimaryUIToHolderRect(RectTransform rect)
        {
            CommonUtils.MovePanelToFillRect(_primaryUIGameObject.GetComponent<RectTransform>(), rect);

            if (_primaryUIHolderGameObject != null)
                GameObject.Destroy(_primaryUIHolderGameObject);
        }

        public void AddNewTab(string tabName, GameObject tab, Sprite icon, int targetIndex) => _centerPanelView.AddNewTab(tabName, tab, icon, targetIndex);

        public void ShowTab(string tabName) => _centerPanelView.OpenTab(tabName);
        #endregion

        #region Internal Interfaces     
        public void EnableModeSwitchButtons() => _utilsPanelView.EnableModeSwitchButtons();
        public void ShowSwitchToVRButton() => _utilsPanelView.ShowSwitchToVRButton();
        public void ShowSwitchTo2DButton() => _utilsPanelView.ShowSwitchTo2DButton();

        public event Action OnSwitchTo2DButtonClicked
        {
            add => _utilsPanelView.OnSwitchTo2DButtonClicked += value;
            remove => _utilsPanelView.OnSwitchTo2DButtonClicked -= value;
        }

        public event Action OnSwitchToVRButtonClicked
        {
            add => _utilsPanelView.OnSwitchToVRButtonClicked += value;
            remove => _utilsPanelView.OnSwitchToVRButtonClicked -= value;
        }


        public event Action OnUIShowInternal
        {
            add => OnUIShow.AddListener(() => value?.Invoke());
            remove => OnUIShow.RemoveListener(() => value?.Invoke());
        }
        public event Action OnUIHideInternal
        {
            add => OnUIHide.AddListener(() => value?.Invoke());
            remove => OnUIHide.RemoveListener(() => value?.Invoke());
        }

        public void SetPlatformQuickpanel(GameObject platformQuickPanel) => _quickPanelView.SetPlatformQuickpanel(platformQuickPanel);

        public void SetInstanceCodeText(string text) => _topBarView.SubtitleText = text;
        #endregion

        private readonly IQuickPressInput _onToggleUIPressed;
        private readonly InputSystemUIInputModule _UIInputModule;

        private readonly GameObject _primaryUIHolderGameObject;
        private readonly GameObject _primaryUIGameObject;

        private readonly PrimaryUITopBarView _topBarView;
        private readonly PrimaryUICenterPanelView _centerPanelView;
        private readonly PrimaryUIQuickPanelView _quickPanelView;
        private readonly PrimaryUIUtilsPanelView _utilsPanelView;
        private readonly MenuUIConfig _menuUIConfig;


        public PrimaryUIService(IQuickPressInput onToggleUIPressed, InputSystemUIInputModule uiInputModule, MenuUIConfig menuUIConfig)
        {
            _onToggleUIPressed = onToggleUIPressed;
            _onToggleUIPressed.OnQuickPress += HandleToggleUIPressed;

            _UIInputModule = uiInputModule;
            _UIInputModule.cursorLockBehavior = InputSystemUIInputModule.CursorLockBehavior.OutsideScreen;

            _menuUIConfig = menuUIConfig;

            _primaryUIHolderGameObject = GameObject.Instantiate(Resources.Load<GameObject>("PrimaryUIHolder"));
            _primaryUIGameObject = _primaryUIHolderGameObject.transform.GetChild(0).gameObject;
            _primaryUIGameObject.SetActive(false);

            _topBarView = _primaryUIGameObject.GetComponentInChildren<PrimaryUITopBarView>();
            _topBarView.OnCloseButtonClicked += HideUI;
            _topBarView.TitleText = SceneManager.GetActiveScene().name;
            _topBarView.SubtitleText = "Solo Play";

            _centerPanelView = _primaryUIGameObject.GetComponentInChildren<PrimaryUICenterPanelView>();
            _quickPanelView = _primaryUIGameObject.GetComponentInChildren<PrimaryUIQuickPanelView>();

            _utilsPanelView = _primaryUIGameObject.GetComponentInChildren<PrimaryUIUtilsPanelView>();
            _utilsPanelView.OnQuitButtonClicked += HandleQuitButtonPressed;
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

        private void HandleQuitButtonPressed()
        {
            #if UNITY_EDITOR
            if (Application.isEditor)
            {
                UnityEditor.EditorApplication.isPlaying = false;
                return;
            }
            #endif

            Application.Quit();
        }

        internal void TearDown()
        {
            _onToggleUIPressed.OnQuickPress -= HandleToggleUIPressed;
        }
    }
}
