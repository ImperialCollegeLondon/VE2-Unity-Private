using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VE2.Core.VComponents.InteractableInterfaces
{
    public interface IRangedClickInteractionModule : IRangedInteractionModule
    {
        public void Click(ushort clientID);
    }
}