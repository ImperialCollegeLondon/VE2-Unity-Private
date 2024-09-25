using TMPro;
using UnityEngine;
using static InstanceSyncSerializables;

public class RemotePlayerController : MonoBehaviour
{
    [SerializeField] private Transform _headHolder;
    [SerializeField] private Transform _torsoHolder;
    private float _torsoOffsetFromHead;
    //private float _torsoOffsetFromRoot;

    [SerializeField] private TMP_Text _playerNameText;
    [SerializeField] private Transform _namePlateTransform;

    private InstancedAvatarAppearance _avatarAppearance;

    private void Awake() 
    {
        _torsoOffsetFromHead = _torsoHolder.position.y - _headHolder.position.y;
        //_torsoOffsetFromRoot = _torsoHolder.position.y - transform.position.y;
    }

    public void HandleReceiveRemotePlayerState(PlayerState playerState)
    {
        transform.position = playerState.RootPosition;
        transform.rotation = playerState.RootRotation;

        _headHolder.position = playerState.HeadPosition;
        _headHolder.rotation = playerState.HeadRotation;

        _torsoHolder.position = playerState.HeadPosition + (_torsoOffsetFromHead * Vector3.up);
    }

    public void HandleReceiveAvatarAppearance(InstancedAvatarAppearance newAvatarAppearance)
    {
        if (_avatarAppearance != null && _avatarAppearance.Equals(newAvatarAppearance))
            return;

        _avatarAppearance = newAvatarAppearance;

        _playerNameText.text = newAvatarAppearance.PlayerName;

        //TODO, handle avatar types, handle colour, handle nameplate 
    }

    private void Update() 
    {
        Vector3 dirToCamera = Camera.main.transform.position - _namePlateTransform.position;
        Vector3 lookPosition = _namePlateTransform.position - dirToCamera;
        _namePlateTransform.LookAt(lookPosition);
    }

    private void OnDisable() 
    {
        //Destroy GO for domain reload
        if (gameObject != null)
            Destroy(gameObject);
    }
}
