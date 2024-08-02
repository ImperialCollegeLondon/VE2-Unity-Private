using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace ViRSE.PlayerRig
{
    public class InputHandler : MonoBehaviour
    {
        public static InputHandler instance;

        public UnityEvent OnMouseLeftClick { get; private set; } = new();

        private void Awake()
        {
            instance = this;
        }

        void Update()
        {
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                OnMouseLeftClick?.Invoke();
            }
        }
    }
}
