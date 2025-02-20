using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VE2.Core.VComponents.API
{
    internal interface IRangedClickInteractionModule : IRangedInteractionModule
    {
        public void Click(ushort clientID);
    }
}