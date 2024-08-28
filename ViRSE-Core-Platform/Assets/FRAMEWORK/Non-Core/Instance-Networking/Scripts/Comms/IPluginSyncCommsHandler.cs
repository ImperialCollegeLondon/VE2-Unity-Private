using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using UnityEngine.Events;
using ViRSE;
using ViRSE.Core.Shared;

namespace ViRSE.FrameworkRuntime
{
    public interface IPluginSyncCommsHandler
    {
        public bool IsReadyToTransmit { get; }

        public void ConnectToServer(IPAddress ipAddress, int portNumber);
        public void SendServerRegistrationRequest(byte[] serverRegistrationBytes);
        public event Action<byte[]> OnReceiveNetcodeConfirmation;
        public event Action<byte[]> OnReceiveServerRegistrationConfirmation;
        public event Action<byte[]> OnReceiveInstanceInfoUpdate;
        public event Action OnDisconnectedFromServer;

        public void SendWorldStateBundle(byte[] bytes, TransmissionProtocol transmissionProtocol);
        public void SendWorldStateSnapshot(byte[] bytes);
        public event Action<byte[]> OnReceiveWorldStateSyncableBundle;

        public void SendLocalPlayerState(byte[] bytes);
        public event Action<byte[]> OnReceiveRemotePlayerState;

        public void SendInstantMessage(byte[] bytes);
        public event Action<byte[]> OnReceiveInstantMessage;

        public void SendPingToHost();

        public void SendPingReplyToNonHost();
    }

    //public interface IPluginWorldStateCommsHandler
    //{
    //    public event Action<byte[]> OnReceiveWorldStateSyncableBundle;

    //    public void SendWorldStateBundle(byte[] bytes, TransmissionProtocol transmissionProtocol);
    //    public void SendWorldStateSnapshot(byte[] bytes);
    //}

    //public interface IPluginLocalPlayerCommsHandler
    //{
    //    public void SendLocalPlayerState(byte[] bytes);
    //}

    //public interface IPluginRemotePlayerCommsHandler
    //{
    //    public event Action<byte[]> OnReceiveRemotePlayerState;
    //}

    //public interface IPluginInstantMessageCommsHandler
    //{
    //    public event Action<byte[]> OnReceiveInstantMessage;
    //    public void SendInstantMessage(byte[] bytes);
    //}

    //public interface IPluginPingCommsHandler
    //{
    //    public void SendPingToHost();

    //    public void SendPingReplyToNonHost();
    //}
}

/*
 * I don't really want WorldStateSyncer seeing the OnPlayerStateReceive message 
 * Could split up the interfaces 
 * 
 * What is PluginSyncService even doing???
 * Well, it's likely managing update of the WorldStateSyncer, InstantMessageRouter, LocalPlayerSyncer, and RemotePlayerSyncer
 * It receives InstanceInfo updates, and uses this to work out if RemotePlayerSyncer should spawn new players (sounds like that's the PlayerSyncer's job?)
 * It also handles PingToHost, uses this to change the length of the buffers
 * 
 * 
 * Why don't we have the WorldStateSyncer pushing messages out to the syncables rather than emitting events? This means WSS has to receive an event from the network, and then emit another event
 * Because only the syncable knows when it starts, we have to register with the syncer to get events 
 * Well, let's just register with the syncer to get direct invocations?
 * Makes more sense for the snapshot! 
 * 
 * This is good! It means the indivual PluginSyncSubServices don't have to talk to the comms handler 
 * The comms handler emits to the PluginSyncService, which then forwards down to the syncers/routers
 * TBF, the PluginSyncService could just wire the CommsHandler events directly up to the syncer's methods? MMmm, not sure about that
 * It might also be nice if PlayerSyncer can have its own variable update frequency??
 * 
 */