using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IRangedPlayerInteractable
{
    public bool IsPositionWithinInteractRange(Vector3 position);
}
