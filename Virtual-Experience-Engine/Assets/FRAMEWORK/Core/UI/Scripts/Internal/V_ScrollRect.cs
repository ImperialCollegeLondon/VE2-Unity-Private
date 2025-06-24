using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace VE2.Core.UI.Internal
{
    public class V_ScrollRect : ScrollRect, IScrollableUI
    {
        private Vector3 _initialDragPosition;
        public bool isHoveringOverScrollbar { get; set; } = false;

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

        public void OnScrollbarBeginDrag(Vector3 position)
        {
            _initialDragPosition = position;
        }

        public void OnScrollbarEndDrag()
        {
            _initialDragPosition = Vector3.zero; // Reset initial position
        }

        public void OnScrollbarDrag(Vector3 position)
        {
            if (_initialDragPosition != Vector3.zero)
            {
                Vector3 dragDelta = position - _initialDragPosition;
                _initialDragPosition = position; // Update initial position for next drag

                if (vertical && verticalScrollbar != null)
                {
                    verticalScrollbar.value += dragDelta.y * scrollSensitivity * 2f;
                }
                if (horizontal && horizontalScrollbar != null)
                {
                    horizontalScrollbar.value += dragDelta.x * scrollSensitivity * 2f;
                }
            }
        }
    }

    public interface IScrollableUI
    {
        public GameObject GameObject => ((Component)this).gameObject;
        public bool isHoveringOverScrollbar { get; set; }
        public void OnScrollUp();
        public void OnScrollDown();
        public void OnScrollLeft();
        public void OnScrollRight();
        public void OnScrollbarBeginDrag(Vector3 position) { }
        public void OnScrollbarEndDrag() { }
        public void OnScrollbarDrag(Vector3 position) { }
    }
}
