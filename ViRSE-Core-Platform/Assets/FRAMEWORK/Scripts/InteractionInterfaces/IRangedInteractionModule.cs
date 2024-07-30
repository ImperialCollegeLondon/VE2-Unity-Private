using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IRangedInteractionModule
{
    public bool IsPositionWithinInteractRange(Vector3 position);
}
