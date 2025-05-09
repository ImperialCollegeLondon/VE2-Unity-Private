using System;
using UnityEngine;
using UnityEngine.InputSystem;
using VE2.NonCore.Instancing.API;

public class InstantMessageTest : MonoBehaviour
{
    private IV_InstantMessageHandler _instantMessageHandler => GetComponent<IV_InstantMessageHandler>();
    private int _counter = 0;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _instantMessageHandler?.OnMessageReceived.AddListener(ReceiveInstantMessage);
    }

    // Update is called once per frame
    void Update()
    {
        if (Keyboard.current.digit9Key.wasPressedThisFrame)
        {
            _counter++;
            Message message = new("Hello world", _counter);
            Debug.Log($"Try send instant message {message}");
            _instantMessageHandler.SendInstantMessage(message);
        }
    }

    public void ReceiveInstantMessage(object message)
    {
        Message receivedMessage = (Message)message;
        _counter = receivedMessage.Counter;
        Debug.Log($"Received instant message {receivedMessage}");
    }

    [Serializable]
    class Message
    {
        public string MessageText { get; private set; }
        public int Counter;

        public Message(string messageText, int counter)
        {
            this.MessageText = messageText;
            this.Counter = counter;
        }

        public override string ToString()
        {
            return $"{MessageText}, {Counter}";
        }

    }
}
