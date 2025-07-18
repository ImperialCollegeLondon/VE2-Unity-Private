using UnityEngine;

public interface IV_GeneralInteractable
{
    #region General Interaction Module Interface
    //We have two General Interaction Modules here, it doesn't matter which one we point to, both share the same General Interaction Config object!
    public bool AdminOnly { get; set; }
    public bool EnableControllerVibrations { get; set; }
    public bool ShowTooltipsAndHighlight { get; set; }
    public bool IsInteractable { get; set; }
    #endregion
}
