using static InstanceSyncSerializables;
using VE2.Common;
using static VE2.Common.CommonSerializables;
using System;
using UnityEngine;
using System.Collections.Generic;

namespace VE2.InstanceNetworking
{
    internal class PingSyncer
    {
        public event Action<BytesAndProtocol> OnPingSend;

        // Client Id and Host status
        private InstanceInfoContainer _instanceInfoContainer;

        // Ping history information
        private int _cycleNumber = 0; // Use as ping identifier

        // key: _cycleNumber, value: time ping was sent in ms
        private Dictionary<int, float> _sentPingMessages = new();

        // key: _cycleNumber, value: ping time in ms
        private List<float> _pings = new();

        public PingSyncer(InstanceInfoContainer instanceInfoContainer)
        {
            _instanceInfoContainer = instanceInfoContainer;
        }

        public float Ping => _pings.Count > 0 ? _pings[^1] : -1;
        public float SmoothPing => GetAveragePing();

        public void NetworkUpdate()
        {
            // Every network update send ping if not host
            if (!_instanceInfoContainer.IsHost)
            {
                _cycleNumber++;
                PingMessage pingMessage = new(_cycleNumber, _instanceInfoContainer.LocalClientID, false);
                _sentPingMessages.Add(_cycleNumber, Time.time*1000);
                OnPingSend?.Invoke(new BytesAndProtocol(pingMessage.Bytes, TransmissionProtocol.TCP));
            }
        }

        public void HandleReceivePingMessage(byte[] bytes)
        {
            PingMessage receivedPingMessage = new(bytes);

            Debug.Log($"Received Ping from ${receivedPingMessage.ClientId}, with ping Id ${receivedPingMessage.PingId}");

            if (_instanceInfoContainer.IsHost)
            {
                // If host, send back
                PingMessage pingMessage = new(receivedPingMessage.PingId, receivedPingMessage.ClientId, true);
                OnPingSend?.Invoke(new BytesAndProtocol(pingMessage.Bytes, TransmissionProtocol.TCP));
            }
            else
            {
                // If non host, we can store a new ping value!
                StorePing(_sentPingMessages[receivedPingMessage.PingId]);

                // We no longer have any use for this sent ping message, let's remove it
                _sentPingMessages.Remove(receivedPingMessage.PingId);

                Debug.Log($"Currently ping is {Ping}ms with smoothed Ping {SmoothPing}ms");
            }

        }


        private float GetAveragePing()
        {
            if (_pings.Count > 0)
            {
                float sum = 0;

                for (int i = 0; i < _pings.Count; i++)
                {
                    sum += _pings[i];
                }

                return sum / _pings.Count;
            }
            else
            {
                return -1;
            }
        }

        public void TearDown()
        {

        }

        private void StorePing(float pingReturnTime)
        {
            _pings.Add(Time.time * 1000 - pingReturnTime);

            // Keep list of pings max 1 second long?
            while (_pings.Count > 60)
            {
                _pings.RemoveAt(0);
            }
        }

    }

}