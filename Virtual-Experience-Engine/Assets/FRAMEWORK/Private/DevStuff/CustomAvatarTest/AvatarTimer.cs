using TMPro;
using UnityEngine;
using VE2.Common.API;
using VE2.NonCore.Instancing.API;

public class AvatarTimer : MonoBehaviour
{
    [SerializeField] private TMP_Text _timerText;
    private IV_NetworkObject _networkObject;

    private float _elapsedTime;

    private void Start()
    {
        _networkObject = GetComponent<IV_NetworkObject>();
        _networkObject.OnDataChange.AddListener(HandleDataUpdated);
    }

    void Update()
    {
        if (VE2API.InstanceService.IsHost)
        {
            _elapsedTime += Time.deltaTime;
            _networkObject.UpdateData(_elapsedTime);
        }
    }

    private void HandleDataUpdated(object data)
    {
        if (data is float elapsedTime)
        {
            _elapsedTime = elapsedTime;
            _timerText.text = ((int)elapsedTime).ToString();
        }
    }
}
