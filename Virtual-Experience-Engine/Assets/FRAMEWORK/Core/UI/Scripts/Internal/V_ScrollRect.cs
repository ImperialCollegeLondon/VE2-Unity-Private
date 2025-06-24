using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VE2.Core.UI.API;

namespace VE2.Core.UI.Internal
{
    public class V_ScrollRect : ScrollRect, IScrollableUI
    {
        private Vector3 _initialDragPosition;

        [SerializeField] private float _dragSensitivity = 2.5f; //Sensitivity for drag scrolling

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
                    verticalScrollbar.value += dragDelta.y * _dragSensitivity;
                }
                if (horizontal && horizontalScrollbar != null)
                {
                    horizontalScrollbar.value += dragDelta.x * _dragSensitivity;
                }
            }
        }
    }
}
