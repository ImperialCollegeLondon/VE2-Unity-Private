using UnityEngine;
using VE2.Common.API;
using VE2.Common.Shared;

public class AvatarLogger : MonoBehaviour
{
    void Start()
    {
        IClientIDWrapper idOfClientWhoOwnsThisAvatar = VE2API.InstanceService.GetClientIDForAvatarGameObject(gameObject);
        Debug.Log($"{gameObject.name} - Created avatar for client ID: {idOfClientWhoOwnsThisAvatar.Value}, IsLocal: {idOfClientWhoOwnsThisAvatar.IsLocal}, IsRemote: {idOfClientWhoOwnsThisAvatar.IsRemote}");
    }
}
