using System.Collections.Generic;
using UnityEngine;
using VE2.Core.Common;
using VE2.Core.Player.API;
using static VE2.Core.Player.API.PlayerSerializables;

namespace VE2.Core.Player.Internal
{
    internal class AvatarVisHandler : MonoBehaviour
    {
        [SerializeField] private Transform _headHolder;
        [SerializeField] private Transform _torsoHolder;

        private List<Material> _colorMaterials = new();

        private float _torsoYOffsetFromHead = -0.387f;

        private OverridableAvatarAppearance _currentRemoteAvatarAppearance;
        private GameObject _activeHead;
        private GameObject _activeTorso;

        private List<GameObject> _virseAvatarHeadGameObjects;
        private List<GameObject> _virseAvatarTorsoGameObjects;
        private List<GameObject> _avatarHeadOverrideGameObjects;
        private List<GameObject> _avatarTorsoOverrideGameObjects;

        private void Awake()
        {
            //Assuming at this point the head and torso are a child of this GO
            _colorMaterials = CommonUtils.GetAvatarColorMaterialsForGameObject(gameObject);

            _activeHead = _headHolder.transform.GetChild(0).gameObject;
            _activeTorso = _torsoHolder.transform.GetChild(0).gameObject;

            RecaptureMaterials();
        }

        private void RecaptureMaterials()
        {
            _colorMaterials = CommonUtils.GetAvatarColorMaterialsForGameObject(gameObject);
        }

        private void Update()
        {
            _torsoHolder.position = _headHolder.position + (_torsoYOffsetFromHead * Vector3.up);
        }  

        /// <summary>
        /// Note, this WON'T set the initial appearance, HandleReceiveAvatarAppearance should be called after initialization
        /// </summary>
        public void Initialize(IPlayerServiceInternal playerServiceInternal)
        {
            //TODO - maybe these should come from PlayerService too?
            _virseAvatarHeadGameObjects = new List<GameObject>()
            {
                Resources.Load<GameObject>("Avatars/Heads/V_Avatar_Head_Default_1"),
                Resources.Load<GameObject>("Avatars/Heads/V_Avatar_Head_Default_2"),
            };

            _virseAvatarTorsoGameObjects = new List<GameObject>()
            {
                Resources.Load<GameObject>("Avatars/Torsos/V_Avatar_Torso_Default_1"),
            };

            _avatarHeadOverrideGameObjects = playerServiceInternal.HeadOverrideGOs;
            _avatarTorsoOverrideGameObjects = playerServiceInternal.HeadOverrideGOs;
        }

        public void HandleReceiveAvatarAppearance(OverridableAvatarAppearance newAvatarAppearance)
        {
            if (_currentRemoteAvatarAppearance != null && _currentRemoteAvatarAppearance.Equals(newAvatarAppearance))
                return;

            bool headChanged = SetHead(newAvatarAppearance.PresentationConfig.AvatarHeadType, newAvatarAppearance.HeadOverrideType);
            bool torsoChanged = SetTorso(newAvatarAppearance.PresentationConfig.AvatarTorsoType, newAvatarAppearance.TorsoOverrideType);

            if (headChanged || torsoChanged)
                RecaptureMaterials();

            foreach (Material material in _colorMaterials)
                material.color = new Color(newAvatarAppearance.PresentationConfig.AvatarRed, newAvatarAppearance.PresentationConfig.AvatarGreen, newAvatarAppearance.PresentationConfig.AvatarBlue) / 255f;

            _currentRemoteAvatarAppearance = newAvatarAppearance;
        }


        private bool SetHead(VE2AvatarHeadAppearanceType avatarHeadType, AvatarAppearanceOverrideType headOverrideType)
        {
            if (_currentRemoteAvatarAppearance != null && _currentRemoteAvatarAppearance.PresentationConfig.AvatarHeadType == avatarHeadType && _currentRemoteAvatarAppearance.HeadOverrideType == headOverrideType)
                return false;

            GameObject newHead;
            if (headOverrideType != AvatarAppearanceOverrideType.None && TryGetOverrideGO(headOverrideType, _avatarHeadOverrideGameObjects, out GameObject headOverrideGO))
            {
                newHead = headOverrideGO;
            }
            else
            {
                newHead = _virseAvatarHeadGameObjects[(int)avatarHeadType];
            }

            if (_activeHead != null)
                GameObject.Destroy(_activeHead);

            _activeHead = GameObject.Instantiate(newHead, _headHolder.transform.position, _headHolder.transform.rotation, _headHolder);

            return true;
        }

        private bool SetTorso(VE2AvatarTorsoAppearanceType avatarTorsoType, AvatarAppearanceOverrideType torsoOverrideType)
        {
            if (_currentRemoteAvatarAppearance != null && _currentRemoteAvatarAppearance.PresentationConfig.AvatarTorsoType == avatarTorsoType && _currentRemoteAvatarAppearance.TorsoOverrideType == torsoOverrideType)
                return false;

            GameObject newTorso;
            if (torsoOverrideType != AvatarAppearanceOverrideType.None && TryGetOverrideGO(torsoOverrideType, _avatarTorsoOverrideGameObjects, out GameObject torsoOverrideGO))
                newTorso = torsoOverrideGO;
            else
                newTorso = _virseAvatarTorsoGameObjects[(int)avatarTorsoType];

            if (_activeTorso != null)
                GameObject.Destroy(_activeTorso);

            _activeTorso = GameObject.Instantiate(newTorso, _torsoHolder.transform.position, _torsoHolder.transform.rotation, _torsoHolder);

            return true;
        }

        private bool TryGetOverrideGO(AvatarAppearanceOverrideType overrideType, List<GameObject> overrideGameObjects, out GameObject overrideGO)
        {
            int index = (int)overrideType - 1; //-1 as 0 is "no override"
            if (overrideGameObjects.Count > index && overrideGameObjects[index] != null)
            {
                overrideGO = overrideGameObjects[index];
                return true;
            }
            else
            {
                overrideGO = null;
                return false;
            }
        }
    }
}
