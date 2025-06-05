using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VE2.Common.Shared;
using static VE2.Core.Player.API.PlayerSerializables;
using static VE2.NonCore.Platform.API.PlatformPublicSerializables;

internal class HubInstanceView : MonoBehaviour
{
    [SerializeField] private TMP_Text _instanceCodeText;
    [SerializeField] private Button _selectInstanceButton;

    [SerializeField] private HorizontalLayoutGroup _playerPreviewsHorizontalGroup;
    private Dictionary<ushort, GameObject> _playerPreviews = new();
    [SerializeField] private TMP_Text _extraPlayersText;
    [SerializeField] private GameObject _playerPreviewPrefab;
    [SerializeField] List<V_UIColorHandler> _colorHandlers;

    public event Action<PlatformInstanceInfo> OnSelectInstance;

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value)
                return;

            _isSelected = value;
            foreach (V_UIColorHandler _colorHandler in _colorHandlers)
            {
                if (_isSelected)
                    _colorHandler.LockSelectedColor();
                else
                    _colorHandler.UnlockSelectedColor();
            }
        }
    }

    private const int MAX_NUM_PLAYER_ICONS = 10;

    //TODO: Need to rework how we're sharing internal serializables...
    public void SetupView(PlatformInstanceInfo instanceInfo)
    {
        _instanceCodeText.text = instanceInfo.InstanceCode.InstanceSuffix;
        _selectInstanceButton.onClick.AddListener(() => OnSelectInstance?.Invoke(instanceInfo));

        UpdateInstanceInfo(instanceInfo);
    }

    public void UpdateInstanceInfo(PlatformInstanceInfo instanceInfo)
    {
        //Remove previews of players that are no longer in the instance
        List<ushort> playerPreviewsToRemove = new();
        foreach (KeyValuePair<ushort, GameObject> playerPreview in _playerPreviews)
        {
            if (!instanceInfo.ClientInfos.ContainsKey(playerPreview.Key))
            {
                Destroy(playerPreview.Value);
                playerPreviewsToRemove.Add(playerPreview.Key);
            }
        }
        foreach (ushort playerID in playerPreviewsToRemove)
            _playerPreviews.Remove(playerID);

        //Add new player previews for players that are in the instance but not currently shown
        foreach (PlatformClientInfo clientInfo in instanceInfo.ClientInfos.Values)
        {
            if (_playerPreviews.Count >= MAX_NUM_PLAYER_ICONS)
                break; // Don't add more than the maximum number of player icons

            if (!_playerPreviews.ContainsKey(clientInfo.ClientID))
            {
                GameObject playerPreview = Instantiate(_playerPreviewPrefab, _playerPreviewsHorizontalGroup.transform);
                Image playerIcon = playerPreview.GetComponent<Image>();

                PlayerPresentationConfig playerPresentationConfig = clientInfo.PlayerPresentationConfig;
                playerIcon.color = new Color(
                    playerPresentationConfig.AvatarRed / 255f,
                    playerPresentationConfig.AvatarGreen / 255f,
                    playerPresentationConfig.AvatarBlue / 255f
                );

                _playerPreviews.Add(clientInfo.ClientID, playerPreview);
            }
        }

        if (instanceInfo.ClientInfos.Count > MAX_NUM_PLAYER_ICONS)
        {
            _extraPlayersText.text = $"+{instanceInfo.ClientInfos.Count - MAX_NUM_PLAYER_ICONS}";
            _extraPlayersText.gameObject.SetActive(true);
        }
        else
        {
            _extraPlayersText.gameObject.SetActive(false);
        }
    }
}
