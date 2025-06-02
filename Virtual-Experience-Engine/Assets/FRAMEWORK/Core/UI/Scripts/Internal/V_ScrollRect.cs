using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace VE2.Core.UI.Internal
{
    public class V_ScrollRect : ScrollRect, IScrollableUI
    {
        public void OnScrollUp()
        {
            if (vertical && verticalScrollbar != null)
            {
                verticalScrollbar.value += 0.1f * scrollSensitivity;
            }
        }

        public void OnScrollDown()
        {
            if (vertical && verticalScrollbar != null)
            {
                verticalScrollbar.value -= 0.1f * scrollSensitivity;
            }
        }

        public void OnScrollLeft()
        {
            if (horizontal && horizontalScrollbar != null)
            {
                horizontalScrollbar.value -= 0.1f * scrollSensitivity;
            }
        }

        public void OnScrollRight()
        {
            if (horizontal && horizontalScrollbar != null)
            {
                horizontalScrollbar.value += 0.1f * scrollSensitivity;
            }
        }
    }

    public interface IScrollableUI
    {
        public GameObject GameObject => ((Component)this).gameObject;
        public Scrollbar VerticalScrollbar => ((V_ScrollRect)this).verticalScrollbar;
        public Scrollbar HorizontalScrollbar => ((V_ScrollRect)this).horizontalScrollbar;
        public void OnScrollUp();
        public void OnScrollDown();
        public void OnScrollLeft();
        public void OnScrollRight();
    }
}
