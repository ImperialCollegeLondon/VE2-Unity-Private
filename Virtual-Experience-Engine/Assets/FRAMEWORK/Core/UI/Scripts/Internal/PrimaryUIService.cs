using System;
using UnityEngine;
using VE2.Core.Common;
using VE2.Core.UI.API;

namespace VE2.Core.UI.Internal
{
    public class PrimaryUIService : IPrimaryUIService
    {
        #region Interfaces
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

        public PrimaryUIService()
        {
            GameObject primaryUIGO = GameObject.Instantiate(Resources.Load<GameObject>("PrimaryUI"));
            PrimaryUIReferences primaryUIReferences = primaryUIGO.GetComponent<PrimaryUIReferences>();  
            _primaryUIGameObject = primaryUIReferences.PrimaryUI;
        }

        internal void TearDown()
        {

        }
    }
}
