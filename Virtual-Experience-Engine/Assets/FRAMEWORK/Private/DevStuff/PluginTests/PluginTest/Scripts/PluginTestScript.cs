using TMPro;
using UnityEngine;
using VE2.Common.API;
using VE2.Common.Shared;
using VE2.NonCore.Instancing.API;

public class PluginTestScript : MonoBehaviour
{
    [SerializeField] private InterfaceReference<IV_NetworkObject> _networkObject;
    [SerializeField] private TMP_Text _timesButtonPushedText;

    private int _timesButtonPushed = 0;

    public void HandleButtonPushed()
    {
        if (VE2API.InstanceService.IsHost)
        {
            _timesButtonPushed++;
            _networkObject.Interface.UpdateData(_timesButtonPushed);
        }
    }

    public void HandleSyncDataUpdated(object obj)
    {
        if (!VE2API.InstanceService.IsHost)
        {
            _timesButtonPushed = (int)obj;
        }
        
        _timesButtonPushedText.text = _timesButtonPushed.ToString(); 
    }    

}
