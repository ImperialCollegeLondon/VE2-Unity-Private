using System.Collections.Generic;
using UnityEngine;
using VE2.Core.Common;

namespace VE2.Core.Player.Internal
{
    //TODO - we can reuse this for the remote player as well... apart from hands??
    //We could just have a "HandsAvatarHandler"?
    //Maybe AvatarHeadAndBodyHandler
    //AvatarHandsColorHandler - maybe we don't need this one tbf
    public class LocalAvatarHandler : MonoBehaviour
    {
        [SerializeField] private Transform _playerCamera;
        [SerializeField] private Transform _avatarHead;
        [SerializeField] private Transform _avatarBody;

        private List<Material> _colorMaterials = new();

        private float _torsoOffsetFromHead = 0.387f;

        public void HandleLocalAvatarColorChanged(Color newColor)
        {
            foreach (Material material in _colorMaterials)
                material.color = newColor;
        }

        private void Awake()
        {
            //Assuming at this point the head and torso are a child of this GO
            _colorMaterials = CommonUtils.GetAvatarColorMaterialsForGameObject(gameObject);

            _avatarHead.transform.parent = _playerCamera;
            _avatarHead.transform.localPosition = Vector3.zero;
            _avatarHead.transform.localRotation = Quaternion.identity;

            _avatarBody.transform.parent = _playerCamera.parent;
            _avatarBody.transform.localRotation = Quaternion.identity;
        }

        private void Update()
        {
            _avatarBody.transform.position = _avatarHead.position - new Vector3(0, _torsoOffsetFromHead, 0);
        }  
    }
}
