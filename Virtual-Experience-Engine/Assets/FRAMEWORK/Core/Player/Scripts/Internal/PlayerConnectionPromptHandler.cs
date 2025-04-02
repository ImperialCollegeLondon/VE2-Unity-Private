using System;
using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using VE2.Core.Common;

namespace VE2.Core.Player.Internal
{
    public class PlayerConnectionPromptHandler : MonoBehaviour
    {
        [SerializeField] private TMP_Text _connectionPromptText;
        private ColorConfiguration _colorConfig;

        private bool _waitingForConnection = false;
        private bool _showingMessage = false;

        private float _timeOfStartShowingMessage = -1;
        Color _baseTextColor;

        private void Awake()
        {
            _connectionPromptText.enabled = false;
            enabled = false;
            _colorConfig = Resources.Load<ColorConfiguration>("ColorConfiguration"); //TODO: Inject, can probably actually go into the base class
        }

        public void NotifyWaitingForConnection()
        {
            _waitingForConnection = true;

            //If it takes more than 2 seconds to connect - show a message
            DOVirtual.DelayedCall(2, () =>
            {
                if (!_waitingForConnection)
                    return;

                _timeOfStartShowingMessage = Time.time;
                _showingMessage = true;
                _connectionPromptText.enabled = true;
                enabled = true;

                _connectionPromptText.text = "Waiting for connection...";
                _connectionPromptText.color = _colorConfig.AccentSecondaryColor;
                _baseTextColor = _colorConfig.AccentSecondaryColor;
            });
        }

        public void NotifyConnected()
        {
            _waitingForConnection = false;

            if (!_showingMessage)
                return;

            StartCoroutine(CheckAlphaAndSwitch());
        }

        private IEnumerator CheckAlphaAndSwitch()
        {
            while (_connectionPromptText.color.a > 0.1f)
                yield return null;

            _connectionPromptText.text = "Connected!";
            _connectionPromptText.color = _colorConfig.AccentPrimaryColor;
            _baseTextColor = _colorConfig.AccentPrimaryColor;

            DOVirtual.DelayedCall((float)Math.PI, () => 
            {
                _showingMessage = false;
                _connectionPromptText.enabled = false;
                enabled = false;
            });
        }

        private void Update()
        {
            if (!_showingMessage)
                return;

            float timeMessageShown = Time.time - _timeOfStartShowingMessage;

            _connectionPromptText.color = new Color(_baseTextColor.r, _baseTextColor.g, _baseTextColor.b, Mathf.Sin((timeMessageShown * 2) - (Mathf.PI / 2)) + 1);
        }
    }
}