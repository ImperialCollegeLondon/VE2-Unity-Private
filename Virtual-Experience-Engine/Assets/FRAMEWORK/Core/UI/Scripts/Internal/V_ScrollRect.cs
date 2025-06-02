using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace VE2.Core.UI.Internal
{
    public class V_ScrollRect : ScrollRect, IScrollableUI
    {
        public Scrollbar VerticalScrollbar => this.verticalScrollbar;
        public Scrollbar HorizontalScrollbar => this.horizontalScrollbar;

        public override void OnDrag(PointerEventData eventData)
        {

        }

        public override void OnBeginDrag(PointerEventData eventData)
        {

        }

        public override void OnEndDrag(PointerEventData eventData)
        {

        }

        public void OnScrollUp()
        {
            if (VerticalScrollbar != null)
            {
                Debug.Log($"Scrolling up: {VerticalScrollbar.value}");
                VerticalScrollbar.value += 0.1f; // Adjust the value as needed
            }
        }
        
        public void OnScrollDown()
        {
            if (VerticalScrollbar != null)
            {
                Debug.Log($"Scrolling down: {VerticalScrollbar.value}");
                VerticalScrollbar.value -= 0.1f; // Adjust the value as needed
            }
        }
    }

    public interface IScrollableUI
    {
        public Scrollbar VerticalScrollbar { get; }
        public Scrollbar HorizontalScrollbar { get; }
        public void OnScrollUp();
        public void OnScrollDown();
    }
}
