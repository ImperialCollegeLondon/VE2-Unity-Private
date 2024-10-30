using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public interface IInputHandler 
{
    public event Action OnMouseLeftClick;
}

public class InputHandler : MonoBehaviour, IInputHandler
{
    public event Action OnMouseLeftClick;

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
    }
}
