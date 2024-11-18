using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public interface IInputHandler 
{
    public event Action OnMouseLeftClick;
    public event Action OnChangeModePressed;
    public event Action OnKeyboardActionKeyPressed;
    public event Action OnMouseScrollUp;
    public event Action OnMouseScrollDown;
}

public class InputHandler : MonoBehaviour, IInputHandler
{
    public event Action OnMouseLeftClick;
    public event Action OnChangeModePressed;
    public event Action OnKeyboardActionKeyPressed;
    public event Action OnMouseScrollUp;
    public event Action OnMouseScrollDown;

    private void Update()
    {
        HandleInput2D();
    }

    private void HandleInput2D()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            OnMouseLeftClick?.Invoke();
        }

        if (Keyboard.current.leftCtrlKey.isPressed && Keyboard.current.hKey.wasPressedThisFrame)
        {
            OnChangeModePressed?.Invoke();
        }

        if (Keyboard.current.fKey.wasPressedThisFrame)
        {
            OnKeyboardActionKeyPressed?.Invoke();
        }

        if (Mouse.current.scroll.ReadValue().y > 0)
        {
            Debug.Log($"Mouse scroll wheel on scroll up is {Mouse.current.scroll.ReadValue().y}");
            OnMouseScrollUp?.Invoke();
        }

        if (Mouse.current.scroll.ReadValue().y < 0)
        {
            Debug.Log($"Mouse scroll wheel on scroll up is {Mouse.current.scroll.ReadValue().y}");
            OnMouseScrollDown?.Invoke();
        }
    }
}
