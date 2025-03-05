using UnityEngine;
using VE2.Core.UI.API;

namespace VE2.Core.UI.Internal
{
    internal class SecondaryUIService : ISecondaryUIService
    {
        #region Interfaces
        public void MoveUIToCanvas(Canvas canvas)
        {
            throw new System.NotImplementedException();
        }
        #endregion

        public SecondaryUIService(SecondaryUIReferences secondaryUIReferences)
        {

        }

        internal void TearDown()
        {

        }
    }
}
