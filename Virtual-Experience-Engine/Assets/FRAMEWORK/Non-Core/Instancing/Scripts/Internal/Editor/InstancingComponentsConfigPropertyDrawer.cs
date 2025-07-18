#if UNITY_EDITOR

using VE2.NonCore.Instancing.Internal;

namespace VE2.Core.VComponents.Internal
{
    #region Property Drawer Implementations
    //NOTE, each of these drawers must be added to the ToolboxEditorSettings' TargetTypeDrawers list

    public class NetworkObjectStateConfigDrawer : OrderedTargetTypeDrawer
    {
        protected override System.Type TargetType => typeof(NetworkObjectStateConfig);
    }
    #endregion
}
#endif
