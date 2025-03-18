using UnityEngine;
using VE2.Core.UI.API;

namespace VE2.Core.UI.Internal
{
    internal class SecondaryUIService : ISecondaryUIServiceInternal
    {
        #region Public Interfaces
        public void SetContent(RectTransform contentRect) => _secondaryUIView.SetContent(contentRect);
        #endregion

        #region Internal Interfaces
        public void MoveSecondaryUIToHolderRect(RectTransform rect)
        {
            UIUtils.MovePanelToFillRect(_secondaryUIGameObject.GetComponent<RectTransform>(), rect);

            if (_secondaryUIHolderGameObject != null)
                GameObject.Destroy(_secondaryUIHolderGameObject);
        }
        #endregion

        private readonly GameObject _secondaryUIHolderGameObject;
        private readonly GameObject _secondaryUIGameObject;
        private readonly SecondaryUIView _secondaryUIView;

        public SecondaryUIService()
        {
            _secondaryUIHolderGameObject = GameObject.Instantiate(Resources.Load<GameObject>("SecondaryUIHolder"));
            _secondaryUIGameObject = _secondaryUIHolderGameObject.transform.GetChild(0).gameObject;
            _secondaryUIGameObject.SetActive(true);

            _secondaryUIView = _secondaryUIGameObject.GetComponent<SecondaryUIView>();
        }

        internal void TearDown()
        {

        }
    }
}
