using System.Collections.Generic;
using UnityEngine;
using ViRSE.Core.Player;
using ViRSE.Core.Shared;
using static InstanceSyncSerializables;

public class PlayerSyncer 
{
    private ILocalPlayerRig _localPlayerRig;
    public bool IsPlayerRegistered => _localPlayerRig != null;

    private Dictionary<ushort, RemotePlayerController> _remotePlayers = new();
    private int _cycleNumber = 0;

    public void RegisterLocalPlayer(ILocalPlayerRig localPlayerRig)
    {
        _localPlayerRig = localPlayerRig;
    }

    public void DeregisterLocalPlayer()
    {
        _localPlayerRig = null;
    }

    public byte[] GetPlayerState() 
    {
        if (_localPlayerRig == null) 
        {
            Debug.LogError("Local player rig not registered");
            return null;
        }   

        _cycleNumber++;
        if (_cycleNumber % (int)(50 / _localPlayerRig.TransmissionFrequency) == 0) 
            return new PlayerState(_localPlayerRig.RootPosition, _localPlayerRig.RootRotation, _localPlayerRig.HeadPosition, _localPlayerRig.HeadRotation).Bytes;
        else 
            return null;
    }

    public void HandleReceiveRemoteClientInfos(Dictionary<ushort, InstancedClientInfo> receivedRemoteClientInfos) 
    {
        foreach (InstancedClientInfo receivedRemoteClientInfo in receivedRemoteClientInfos.Values) 
        {
            if (!_remotePlayers.ContainsKey(receivedRemoteClientInfo.ClientID)) 
            {
                GameObject remotePlayerPrefab = Resources.Load<GameObject>("RemoteAvatar");
                GameObject remotePlayerGO = GameObject.Instantiate(remotePlayerPrefab);
                _remotePlayers.Add(receivedRemoteClientInfo.ClientID, remotePlayerGO.GetComponent<RemotePlayerController>());
            }

            _remotePlayers[receivedRemoteClientInfo.ClientID].HandleReceiveAvatarAppearance(receivedRemoteClientInfo.InstancedAvatarAppearance);
        }

        List<ushort> remoteClientIDsToDespawn = new(_remotePlayers.Keys);
        remoteClientIDsToDespawn.RemoveAll(id => receivedRemoteClientInfos.ContainsKey(id));

        foreach (ushort idToDespawn in remoteClientIDsToDespawn)
        {
            GameObject.Destroy(_remotePlayers[idToDespawn].gameObject);
            _remotePlayers.Remove(idToDespawn);
        }
    }

    public void HandleReceiveRemotePlayerState(byte[] stateAsBytes)
    {
        PlayerStateWrapper stateWrapper = new(stateAsBytes);
        PlayerState playerState = new(stateWrapper.StateBytes);

        if (_remotePlayers.TryGetValue(stateWrapper.ID, out RemotePlayerController remotePlayerController))
            remotePlayerController.HandleReceiveRemotePlayerState(playerState);
    }

    // private void SpawnNewRemotePlayer(ushort id)
    // {
    //     GameObject remotePlayerPrefab = Resources.Load<GameObject>("RemoteAvatar");
    //     GameObject remotePlayerGO = GameObject.Instantiate(remotePlayerPrefab);
    //     _remotePlayers.Add(id, remotePlayerGO.GetComponent<RemotePlayerController>());
    // }

    // private void DespawnRemotePlayer(ushort id)
    // {
    //     if (!_remotePlayers.TryGetValue(id, out RemotePlayerController remotePlayer))
    //     {
    //         Debug.LogError($"No remote player with id {id} found to despawn");
    //         return;
    //     }

    //     GameObject.Destroy(remotePlayer.gameObject);
    //     _remotePlayers.Remove(id);
    // }

    public void TearDown() 
    {
        foreach (RemotePlayerController remotePlayerController in _remotePlayers.Values)
            if (remotePlayerController != null && remotePlayerController.gameObject != null)
                GameObject.Destroy(remotePlayerController.gameObject);

        _remotePlayers.Clear();
    }
}
