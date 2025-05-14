using System;
using UnityEngine;
using UnityEngine.InputSystem;
using VE2.NonCore.Instancing.API;

public class InstantMessagePluginScript : MonoBehaviour
{
    private IV_InstantMessageHandler _instantMessageHandler => GetComponent<IV_InstantMessageHandler>();
    private IV_NetworkObject _networkObject => GetComponent<IV_NetworkObject>();
    private int _counter = 0;

    void Start()
    {
        _instantMessageHandler?.OnMessageReceived.AddListener(ReceiveInstantMessage);
        _networkObject?.OnDataChange.AddListener(ReceiveNetworkObject);
    }

    void Update()
    {
        if (Keyboard.current.digit9Key.wasPressedThisFrame)
        {
            _counter++;
            Message message = new("Hello world", _counter);
            Debug.Log($"Try send instant message {message}");
            _instantMessageHandler.SendInstantMessage(message);
        }

        if (Keyboard.current.digit8Key.wasPressedThisFrame)
        {
            _counter++;
            Message message = new("network object... ", _counter);
            Debug.Log($"Try send network object {message}");
            _networkObject.UpdateData(message);
        }
    }

    public void ReceiveInstantMessage(object message)
    {
        Message receivedMessage = (Message)message;
        _counter = receivedMessage.Counter;
        Debug.Log($"Received instant message {receivedMessage}");
    }

    public void ReceiveNetworkObject(object message)
    {
        Message receivedMessage = (Message)message;
        _counter = receivedMessage.Counter;
        Debug.Log($"Received network object {receivedMessage}");
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
