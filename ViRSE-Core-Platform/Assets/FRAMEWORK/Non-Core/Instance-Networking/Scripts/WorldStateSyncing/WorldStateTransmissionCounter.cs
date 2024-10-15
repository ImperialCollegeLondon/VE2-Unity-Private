using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.Events;
using ViRSE.FrameworkRuntime;

namespace ViRSE.Core.VComponents
{
    //TODO - rename to something else, this isn't really a "module" anymore... maybe "TransmissionCounter"?
    public class WorldStateTransmissionCounter 
    {
        //#region Plugin Interfaces
        //float IWorldStateSyncableModule.SyncFrequency {
        //    get => _config.SyncFrequency;
        //    set {
        //        _config.SyncFrequency = value;
        //        if (_config.SyncFrequency > 1)
        //            _config.SyncFrequency = Mathf.RoundToInt(_config.SyncFrequency);
        //    }
        //}
        ////TransmissionProtocol IWorldStateSyncableModule.TransmissionProtocol { get => _protocolModule.TransmissionProtocol; set => _protocolModule.TransmissionProtocol = value; }
        //#endregion

        float _transmissionFrequency;

        //We don't want host to blast out state for everything every x frames, should instead stagger them to even network load
        private int _hostSyncOffset;
        private int _hostSyncInterval; //How many frames go by before syncing

        private bool _syncOffsetSetup = false;
        private static Dictionary<int, int> _numOfSyncablesPerSyncOffsets = new();

        public bool IsOnBroadcastFrame(int cycleNumber) => (cycleNumber + _hostSyncOffset) % _hostSyncInterval == 0;

        public WorldStateTransmissionCounter(float transmissionFrequency)
        {
           _transmissionFrequency = transmissionFrequency;
            SetupHostSyncOffset();
        }

        private void SetupHostSyncOffset()
        {
            //We'd need to subtract 1 if we've already added one
            if (_syncOffsetSetup)
                _numOfSyncablesPerSyncOffsets[_hostSyncOffset]--;

            _hostSyncInterval = (int)(50 / _transmissionFrequency); //a frequency of 1 should send messages every 50 fixedupdate frames 

            //To smooth out the transmission load, choose the least used offset
            int leastUsedSyncOffset = 0;
            int timesLeastUsedSyncOffsetUsed = 10000;

            for (int i = 0; i < 50; i++)
            {
                int timesSyncOffsetUsed = _numOfSyncablesPerSyncOffsets.ContainsKey(i) ?
                    _numOfSyncablesPerSyncOffsets[i] : 0;

                if (timesSyncOffsetUsed < timesLeastUsedSyncOffsetUsed)
                {
                    timesLeastUsedSyncOffsetUsed = timesSyncOffsetUsed;
                    leastUsedSyncOffset = i;
                }

                if (timesLeastUsedSyncOffsetUsed == 0)
                    break; //No need to keep searching, can't be less than zero!
            }

            _hostSyncOffset = leastUsedSyncOffset;

            if (_numOfSyncablesPerSyncOffsets.ContainsKey(_hostSyncOffset))
                _numOfSyncablesPerSyncOffsets[_hostSyncOffset]++;
            else
                _numOfSyncablesPerSyncOffsets.Add(_hostSyncOffset, 1);

            _syncOffsetSetup = true;
        }

        //public void SetSyncFrequency(float newFrequency)
        //{
        //    if (newFrequency < 0)
        //    {
        //        V_Logger.Error("Tried to set sync frequency to below zero on the " + GetType() + " on " + _goName + ", this is not allowed!");
        //        return;
        //    }

        //    if (_config.TransmissionFrequency > 1)
        //        newFrequency = Mathf.RoundToInt(newFrequency);

        //    _config.TransmissionFrequency = newFrequency;
        //    SetupHostSyncOffset();
        //}
    }
}


/*
 * Networking plan
 * The Networking config is part of the core framework, it shows up in the inspector if the state module finds a network thingy present 
 * we want to abstract away any mention of "syncable", "predictive" etc
 * 
 * What about the ID?
 * GO name for ID makes sense 
 * let's just keep it like that. If we're showing network settings, show the ID stuff too!!
 * 
 * 
 */