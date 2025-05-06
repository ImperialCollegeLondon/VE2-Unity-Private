using System;
using UnityEngine;
using System.Collections.Generic;
using static VE2.NonCore.Instancing.Internal.InstanceSyncSerializables;
using VE2.Common.Shared;

namespace VE2.NonCore.Instancing.Internal
{
    internal class PingSyncer
    {
        public event Action<int> OnPingUpdate;

        // Client Id and Host status
        private InstanceInfoContainer _instanceInfoContainer;

        // Ping history information
        private int _cycleNumber = 0; // Use as ping identifier

        // key: _cycleNumber, value: time ping was sent in ms
        private Dictionary<int, float> _sentPingMessages = new();

        // key: _cycleNumber, value: ping time in ms
        private List<float> _pings = new();

        private readonly IPluginSyncCommsHandler _commsHandler;

        public PingSyncer(IPluginSyncCommsHandler commsHandler, InstanceInfoContainer instanceInfoContainer)
        {
            _commsHandler = commsHandler;
            _instanceInfoContainer = instanceInfoContainer;

            _commsHandler.OnReceivePingMessage += HandleReceivePingMessage;
        }

        public float Ping => _pings.Count > 0 ? _pings[^1] : -1;
        public int SmoothPing => GetAveragePing();

        private readonly int MAX_PING_RECORDS = 20;

        public void NetworkUpdate() //TODO - maybe doesn't want to send ping as often as every fixedupdate?
        {
            // Every network update send ping if not host
            if (!_instanceInfoContainer.IsHost)
            {
                _cycleNumber++;
                PingMessage pingMessage = new(_cycleNumber, _instanceInfoContainer.LocalClientID);
                _sentPingMessages.Add(_cycleNumber, Time.time*1000);
                _commsHandler.SendMessage(pingMessage.Bytes, InstanceNetworkingMessageCodes.PingMessage, TransmissionProtocol.TCP);
            }
        }

        public void HandleReceivePingMessage(byte[] bytes)
        {
            PingMessage receivedPingMessage = new(bytes);

            if (_instanceInfoContainer.IsHost)
            {
                // If host, send back
                PingMessage pingMessage = new(receivedPingMessage.PingId, receivedPingMessage.ClientId);
                _commsHandler.SendMessage(pingMessage.Bytes, InstanceNetworkingMessageCodes.PingMessage, TransmissionProtocol.TCP);
            }
            else
            {
                // If non host, we can store a new ping value!
                StorePing(_sentPingMessages[receivedPingMessage.PingId]);

                // We no longer have any use for this sent ping message, let's remove it
                _sentPingMessages.Remove(receivedPingMessage.PingId);
                OnPingUpdate?.Invoke(SmoothPing);
            }

        }


        private int GetAveragePing()
        {
            if (_pings.Count > 0)
            {
                float sum = 0;

                for (int i = 0; i < _pings.Count; i++)
                {
                    sum += _pings[i];
                }

                return (int)(sum / _pings.Count);
            }
            else
            {
                return -1;
            }
        }

        public void TearDown()
        {
            _commsHandler.OnReceivePingMessage -= HandleReceivePingMessage;
        }

        private void StorePing(float pingReturnTime)
        {
            _pings.Add(Time.time * 1000 - pingReturnTime);

            // Keep list of ping some specific length?
            while (_pings.Count > MAX_PING_RECORDS)
            {
                _pings.RemoveAt(0);
            }
        }

    }

}