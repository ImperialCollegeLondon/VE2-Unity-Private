using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VIRSE.Core.VComponents.InteractableInterfaces
{
    public interface IRangedClickInteractionModule : IRangedInteractionModule
    {
        public void Click(ushort clientID);
    }
}