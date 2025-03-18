using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VE2.Core.VComponents.API
{
    internal interface IRangedClickInteractionModule : IRangedInteractionModule
    {
        public void ClickDown(InteractorID interactorID);
        public void ClickUp(InteractorID interactorID);
        public string ID { get; }
    }
}