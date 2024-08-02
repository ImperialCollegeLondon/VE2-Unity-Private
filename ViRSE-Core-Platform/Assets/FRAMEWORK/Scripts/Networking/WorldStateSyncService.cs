using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace ViRSE
{
    public class SyncableStateReceiveEvent : UnityEvent<BaseSyncableState> { };

    public static class WorldStateSyncService /*: MonoBehaviour*/
    {
        //public static WorldStateSyncService instance;

        //[ReadOnly][SerializeField] private int numberOfSyncablesRegisteredDebug = 0;

        public delegate void SyncableStateReceiver(BaseSyncableState newState);
        private static Dictionary<string, SyncableStateReceiveEvent> syncableStateReceivedEvents = new();

        private static List<BaseSyncableState> outgoingSyncableStateBufferTCP = new();
        private static List<BaseSyncableState> outgoingSyncableStateBufferUDP = new();

        public static List<BaseSyncableState> incommingWorldStateBuffer = new();

        public static SyncableStateReceiveEvent RegisterForSyncDataReceivedEvents(string id)
        {
            SyncableStateReceiveEvent syncableStateReceivedEvent = new();
            syncableStateReceivedEvents.Add(id, syncableStateReceivedEvent);

            //numberOfSyncablesRegisteredDebug++;

            return syncableStateReceivedEvent;
        }

        public static void DeregisterListener(string id)
        {
            //numberOfSyncablesRegisteredDebug--;

            syncableStateReceivedEvents.Remove(id);
        }

        public static void AddStateToOutgoingBuffer(BaseSyncableState stateToTransmit, TransmissionProtocol protocol)
        {
            if (protocol == TransmissionProtocol.TCP)
                outgoingSyncableStateBufferTCP.Add(stateToTransmit);
            else
                outgoingSyncableStateBufferUDP.Add(stateToTransmit);
        }

        public static void ReceiveWorldState(List<BaseSyncableState> syncableStates)
        {
            incommingWorldStateBuffer.AddRange(syncableStates);
        }

        public static void UpdateWorldState()
        {
            ProcessReceivedWorldStates();

            //if (outgoingSyncableStateBufferTCP.Count > 0)
            //    V_NetworkCommsHandler.Send.WorldStateUDP(outgoingSyncableStateBufferTCP);

            //if (outgoingSyncableStateBufferUDP.Count() > 0)
            //    V_NetworkCommsHandler.Send.WorldStateTCP(outgoingSyncableStateBufferUDP);

            outgoingSyncableStateBufferTCP.Clear();
            outgoingSyncableStateBufferUDP.Clear();
        }

        private static void ProcessReceivedWorldStates()
        {
            foreach (BaseSyncableState receivedState in incommingWorldStateBuffer)
            {
                if (syncableStateReceivedEvents.TryGetValue(receivedState.id, out SyncableStateReceiveEvent syncableStateReceiveEvent))
                {
                    try
                    {
                        syncableStateReceiveEvent.Invoke(receivedState);
                    }
                    catch (System.Exception ex)
                    {
                        //V_Logger.Error("Error receiving syncable data - " + ex.StackTrace + " - " + ex.Message);
                    }
                }
            }

            incommingWorldStateBuffer.Clear();
        }
    }

    public abstract class BaseSyncableState
    {
        public readonly string id;

        protected BaseSyncableState(string id)
        {
            this.id = id;
        }
    }
}