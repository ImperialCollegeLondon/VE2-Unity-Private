#if UNITY_EDITOR

namespace VE2.Core.VComponents.Internal
{
    #region Property Drawer Implementations
    //NOTE, each of these drawers must be added to the ToolboxEditorSettings' TargetTypeDrawers list

    public class RangedAdjustableInteractionConfigDrawer : OrderedTargetTypeDrawer
    {
        protected override System.Type TargetType => typeof(RangedAdjustableInteractionConfig);
    }

    public class RangedFreeGrabInteractionConfigDrawer : OrderedTargetTypeDrawer
    {
        protected override System.Type TargetType => typeof(RangedFreeGrabInteractionConfig);
    }

    public class RangedClickInteractionConfigDrawer : OrderedTargetTypeDrawer
    {
        protected override System.Type TargetType => typeof(RangedClickInteractionConfig);
    }
    #endregion
}
#endif
