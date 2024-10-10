using System.Collections.Generic;
using TMPro;
using UnityEngine;
using static InstanceSyncSerializables;

public class RemotePlayerController : MonoBehaviour
{
    [SerializeField] private Transform _headHolder;
    [SerializeField] private Transform _torsoHolder;
    private float _torsoOffsetFromHead;

    [SerializeField] private TMP_Text _playerNameText;
    [SerializeField] private Transform _namePlateTransform;

    private List<Material> _colorMaterials = new();

    private InstancedPlayerPresentation _avatarAppearance;

    private void Awake() 
    {
        _torsoOffsetFromHead = _torsoHolder.position.y - _headHolder.position.y;

        foreach (Renderer renderer in GetComponentsInChildren<Renderer>())
        {
            for (int i = 0; i < renderer.materials.Length; i++)
            {
                if (renderer.materials[i].name.Contains("V_AvatarPrimary"))
                {
                    _colorMaterials.Add(renderer.materials[i]);
                }
            }
        }
    }

    public void HandleReceiveRemotePlayerState(PlayerState playerState)
    {
        transform.position = playerState.RootPosition;
        transform.rotation = playerState.RootRotation;

        _headHolder.position = playerState.HeadPosition;
        _headHolder.rotation = playerState.HeadRotation;

        _torsoHolder.position = playerState.HeadPosition + (_torsoOffsetFromHead * Vector3.up);
    }

    public void HandleReceiveAvatarAppearance(InstancedPlayerPresentation newAvatarAppearance)
    {
        if (_avatarAppearance != null && _avatarAppearance.Equals(newAvatarAppearance))
            return;

        _avatarAppearance = newAvatarAppearance;

        _playerNameText.text = newAvatarAppearance.PlayerPresentationConfig.PlayerName;

        foreach (Material material in _colorMaterials)
            material.color = new Color(newAvatarAppearance.PlayerPresentationConfig.AvatarRed, newAvatarAppearance.PlayerPresentationConfig.AvatarGreen, newAvatarAppearance.PlayerPresentationConfig.AvatarBlue) / 255f; 

        //TODO, handle avatar types
    }

    private void Update() 
    {
        if (Camera.main == null)
            return;

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
