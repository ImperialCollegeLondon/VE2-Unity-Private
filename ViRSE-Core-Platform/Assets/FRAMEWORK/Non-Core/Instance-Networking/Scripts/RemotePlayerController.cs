using UnityEngine;

public class RemotePlayerController : MonoBehaviour
{
    [SerializeField] private Transform _headHolder;
    [SerializeField] private Transform _torsoHolder;
    private float _torsoOffsetFromHead;

    private void Awake() 
    {
        _torsoOffsetFromHead = _torsoHolder.position.y - _headHolder.position.y;
    }

    public void HandleReceiveRemotePlayerState(PlayerState playerState)
    {
        transform.position = playerState.RootPosition;
        transform.rotation = playerState.RootRotation;

        _headHolder.position = playerState.HeadPosition;
        _headHolder.rotation = playerState.HeadRotation;

        _torsoHolder.position = playerState.HeadPosition + (_torsoOffsetFromHead * Vector3.up);
    }
}
