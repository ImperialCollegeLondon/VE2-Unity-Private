#if UNITY_EDITOR

using VE2.Core.VComponents.Internal;

namespace VE2.Core.Player.Internal
{
    #region Property Drawer Implementations
    //NOTE, each of these drawers must be added to the ToolboxEditorSettings' TargetTypeDrawers list

    public class PlayerConfigConfigDrawer : OrderedTargetTypeDrawer
    {
        protected override System.Type TargetType => typeof(PlayerConfig);
    }
    #endregion
}
#endif
