using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class V_InputHandler : MonoBehaviour
{
    protected static V_InputHandler _instance;
    public static V_InputHandler Instance {
        get {
            if (_instance == null)
                _instance = FindFirstObjectByType<V_InputHandler>();
            if (_instance == null)
                _instance = new GameObject("V_InputHandler").AddComponent<V_InputHandler>();
            return _instance;
        }
    }

    public virtual InputHandler2D InputHandler2D {get; protected set;} = new();

    private void Update()
    {
        InputHandler2D.HandleUpdate();
    }
}

public class InputHandler2D 
{
    public event Action OnMouseLeftClick;
    protected void InvokeOnMouseLeftClick() => OnMouseLeftClick?.Invoke();

    public void HandleUpdate()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            OnMouseLeftClick?.Invoke();
        }
            
    }
}
