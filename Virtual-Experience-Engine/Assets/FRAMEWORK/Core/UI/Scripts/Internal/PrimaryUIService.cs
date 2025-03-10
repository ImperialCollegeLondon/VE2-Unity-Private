using System;
using UnityEngine;
using VE2.Core.Common;
using VE2.Core.Player.API;
using VE2.Core.UI.API;

namespace VE2.Core.UI.Internal
{
    internal class PrimaryUIService : IPrimaryUIService
    {
        #region Interfaces
        public bool IsShowing => _primaryUIGameObject.activeSelf;
        public void ShowUI() => _primaryUIGameObject.SetActive(true);
        public event Action OnUIShow;
        public event Action OnUIHide;
        public void HidePrimaryUI() => _primaryUIGameObject.SetActive(false);   

        public void MoveUIToCanvas(Canvas canvas)
        {
            UIUtils.MovePanelToFillCanvas(_primaryUIGameObject.GetComponent<RectTransform>(), canvas);
        }

        public void AddNewTab(GameObject tab, string tabName, IconType iconType)
        {
            throw new System.NotImplementedException();
        }
        #endregion

        private readonly GameObject _primaryUIGameObject;
        private readonly IPressableInput _onToggleUIPressed;

        public PrimaryUIService(IPressableInput onToggleUIPressed)
        {
            GameObject primaryUIGO = GameObject.Instantiate(Resources.Load<GameObject>("PrimaryUI"));
            primaryUIGO.SetActive(false);

            PrimaryUIReferences primaryUIReferences = primaryUIGO.GetComponent<PrimaryUIReferences>();  
            _primaryUIGameObject = primaryUIReferences.PrimaryUI;

            _onToggleUIPressed = onToggleUIPressed;
            _onToggleUIPressed.OnPressed += HandleToggleUIPressed;
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
}
