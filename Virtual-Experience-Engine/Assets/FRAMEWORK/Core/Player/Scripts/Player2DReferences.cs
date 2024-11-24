using UnityEngine;

namespace VE2.Core.Player
{
    public class Player2DReferences : MonoBehaviour
    {
        public Interactor2DReferences Interactor2DReferences => _interactor2DReferences;
        [SerializeField, IgnoreParent] private Interactor2DReferences _interactor2DReferences;
    }
}
