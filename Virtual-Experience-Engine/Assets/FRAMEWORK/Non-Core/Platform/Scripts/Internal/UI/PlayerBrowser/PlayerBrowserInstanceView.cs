using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.UI;
using static VE2.NonCore.Platform.API.PlatformPublicSerializables;

namespace VE2.NonCore.Platform.Internal
{
    internal class PlayerBrowserInstanceView : MonoBehaviour
    {
        // has the button for the instance 
        // has a list of player views 

        [SerializeField] private TMP_Text _instanceNumberText;
        [SerializeField] private Button _instanceButton;
        [SerializeField] private VerticalLayoutGroup _playersLayoutGroup;

        public event Action<InstanceCode> OnInstanceButtonClicked;

        private InstanceCode _instanceCode;

        public void Setup(InstanceCode instanceCode, Dictionary<ushort, ClientInfoBase> players)
        {
            _instanceCode = instanceCode;
            _instanceNumberText.text = instanceCode.ToString();
            _instanceButton.onClick.AddListener(() => OnInstanceButtonClicked?.Invoke(instanceCode));

            UpdatePlayers(players);
        }

        public void UpdatePlayers(Dictionary<ushort, ClientInfoBase> players)
        {
            // Clear existing player views
            foreach (Transform child in _playersLayoutGroup.transform)
            {
                Destroy(child.gameObject);
            }

            // Create new player views
            foreach (var player in players)
            {
                GameObject playerViewObj = Instantiate(Resources.Load<GameObject>("PlayerBrowserPlayerViewPrefab"), _playersLayoutGroup.transform);
                PlayerBrowserPlayerView playerView = playerViewObj.GetComponent<PlayerBrowserPlayerView>();
                playerView.Setup(player.Value, player.Value.IsHost);
            }
        }
    }
}
