
using UnityEngine;

/// <summary>
/// Encapsulates an image placed in multiple-choice answer sequence (not part of an answer)
/// </summary>
public class Q_QuizHorizontalImage : Q_QuizMCItem, IQuizHorizontalImage
{
    //[Tooltip("V_QuizHorizontalImage should be the child of a V_QuizQuestionMultipleChoice GameObject.\n\n" +
    //    "To access the image from plugin code, use GetComponent<IQuizHorizontalImage>() on this GameObject.\n\nExample: if your script is on this gameobject, set the image textue using:\ngameObject.GetComponent<IQuizHorizontalImage>().Image = myImage;")]

    [Tooltip("Image to place as horizontal item")]
    [SerializeField] private Texture2D image;
    [Tooltip("Set to more than 1 to occupy extra height in layout")]
    [SerializeField] private float layoutWeight = 1.0f;

    public float LayoutWeight { get => layoutWeight; set => layoutWeight = value; }
    public Texture2D Image { get => image; set => image = value; }
}
