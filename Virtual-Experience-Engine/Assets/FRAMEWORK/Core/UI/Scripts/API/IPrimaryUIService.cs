using System;
using UnityEngine;
using UnityEngine.Events;

namespace VE2.Core.UI.API
{
    public interface IPrimaryUIService
    {
        public bool IsShowing { get; }

        public void ShowUI();
        public UnityEvent OnUIShow { get; }

        public void HideUI();
        public UnityEvent OnUIHide { get; }

        /// <summary>
        /// Takes the target index, returns the actual index - the two may differ if another panel got that index first 
        /// </summary>
        public void AddNewTab(string tabName, GameObject tab, Sprite icon, int targetIndex);

        public void ShowTab(string tabName);
    }
}
