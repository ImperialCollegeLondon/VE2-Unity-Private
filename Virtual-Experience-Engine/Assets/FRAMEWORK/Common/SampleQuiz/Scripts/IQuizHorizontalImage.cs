using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IQuizHorizontalImage
{
    /// <summary>
    /// The relative vertical weight for the horizontal image in the layout, e.g. setting this to '2' will 
    /// make it occupy twice the vertical space of any answer rows. Changes will not affect currently displayed questions.
    /// </summary>
    public float LayoutWeight { get; set; }

    /// <summary>
    /// Texture2D image to display. Changes will not affect currently displayed questions.
    /// </summary>
    public Texture2D Image { get; set; }
}
