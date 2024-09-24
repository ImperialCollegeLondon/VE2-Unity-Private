using System.Collections.Generic;
using UnityEngine;
using ViRSE.Core.Player;
using ViRSE.Core.Shared;

public class PlayerSyncer 
{
    private ILocalPlayerRig _localPlayerRig;

    private Dictionary<ushort, RemotePlayerController> _remotePlayers = new();
    private int _cycleNumber = 0;

    public void RegisterLocalPlayer(ILocalPlayerRig localPlayerRig)
    {
        _localPlayerRig = localPlayerRig;
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

    public void HandleReceiveRemotePlayerState(byte[] stateAsBytes)
    {
        PlayerStateWrapper stateWrapper = new(stateAsBytes);
        PlayerState playerState = new(stateWrapper.StateBytes);

        if (_remotePlayers.TryGetValue(stateWrapper.ID, out RemotePlayerController remotePlayerController))
            remotePlayerController.HandleReceiveRemotePlayerState(playerState);
    }

    public void SpawnNewRemotePlayer(ushort id)
    {
        GameObject remotePlayerPrefab = Resources.Load<GameObject>("RemoteAvatar");
        GameObject remotePlayerGO = GameObject.Instantiate(remotePlayerPrefab);
        _remotePlayers.Add(id, remotePlayerGO.GetComponent<RemotePlayerController>());
    }

    public void DespawnRemotePlayer(ushort id)
    {
        if (!_remotePlayers.TryGetValue(id, out RemotePlayerController remotePlayer))
        {
            Debug.LogError($"No remote player with id {id} found to despawn");
            return;
        }

        GameObject.Destroy(remotePlayer.gameObject);
        _remotePlayers.Remove(id);
    }

    public void TearDown() 
    {
        foreach (RemotePlayerController remotePlayerController in _remotePlayers.Values)
            if (remotePlayerController != null && remotePlayerController.gameObject != null)
                GameObject.Destroy(remotePlayerController.gameObject);

        _remotePlayers.Clear();
    }
}
