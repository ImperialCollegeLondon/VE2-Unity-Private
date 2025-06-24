using UnityEngine;

namespace VE2.Core.UI.API
{
    internal interface IScrollableUI
    {
        public GameObject GameObject => ((Component)this).gameObject;
        public void OnScrollUp();
        public void OnScrollDown();
        public void OnScrollLeft();
        public void OnScrollRight();
        public void OnScrollbarBeginDrag(Vector3 position) { }
        public void OnScrollbarEndDrag() { }
        public void OnScrollbarDrag(Vector3 position) { }
    }
}
