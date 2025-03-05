using UnityEngine;
using VE2.Core.Common;

public class PrimaryUIReferences : MonoBehaviour
{
    //Don't think we actually need to stub out GOs here, they're an implementation detail of the service
    // private IGameObjectWrapper _primaryUI;
    // public IGameObjectWrapper PrimaryUI 
    // {
    //     get 
    //     {
    //         _primaryUI ??= new GameObjectWrapper(gameObject);
    //         return _primaryUI;
    //     }
    // }

    public GameObject PrimaryUI => gameObject;
}
