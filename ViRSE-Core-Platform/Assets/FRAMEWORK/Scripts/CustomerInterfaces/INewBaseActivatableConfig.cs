using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VComponents
{
    public interface INewBaseActivatableConfig
    {
        public void Activate();

        public void Deactivate();

        public bool IsActivated();
    }
}
