using UnityEngine;
using VE2.Common.Shared;
using VE2.Core.Player.API;
using VE2.Core.UI.API;

namespace VE2.Core.UI.Internal
{
    internal class SecondaryUIService : ISecondaryUIServiceInternal
    {
        #region Public Interfaces
        #endregion

        #region Internal Interfaces
        public void MoveSecondaryUIToHolderRect(RectTransform rect)
        {
            CommonUtils.MovePanelToFillRect(_secondaryUIGameObject.GetComponent<RectTransform>(), rect);
            _secondaryUIGameObject.SetActive(true);

            if (_secondaryUIHolderGameObject != null)
                GameObject.Destroy(_secondaryUIHolderGameObject);
        }

        public void SetContent(RectTransform contentRect) => _secondaryUIView.SetContent(contentRect);

        public void EnableShowHideKeyboardControl()
        {
            _showHideKeyboardControlEnabled = true;
            _secondaryUIView.EnableKeyPrompt();
        }
        public void DisableShowHideKeyboardControl()
        {
            _showHideKeyboardControlEnabled = false;
            _secondaryUIView.DisableKeyPrompt();
        }
        #endregion

        private bool _showHideKeyboardControlEnabled = false;

        private readonly IPressableInput _onToggleSecondaryUIPressed;
        private readonly GameObject _secondaryUIHolderGameObject;
        private readonly GameObject _secondaryUIGameObject;
        private readonly SecondaryUIView _secondaryUIView;

        public SecondaryUIService(IPressableInput onToggleSecondaryUIPressed)
        {
            _secondaryUIHolderGameObject = GameObject.Instantiate(Resources.Load<GameObject>("SecondaryUIHolder"));
            _secondaryUIGameObject = _secondaryUIHolderGameObject.transform.GetChild(0).gameObject;
            _secondaryUIGameObject.SetActive(false);

            _secondaryUIView = _secondaryUIGameObject.GetComponent<SecondaryUIView>();

            _onToggleSecondaryUIPressed = onToggleSecondaryUIPressed;
            _onToggleSecondaryUIPressed.OnPressed += HandleToggleUIPressed;
        }

        private void HandleToggleUIPressed()
        {
            if (_showHideKeyboardControlEnabled)
                _secondaryUIView.ToggleBottomPanel();
        }

        internal void TearDown()
        {
            _onToggleSecondaryUIPressed.OnPressed -= HandleToggleUIPressed;
        }
    }
}
