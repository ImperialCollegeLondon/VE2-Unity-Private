using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Sits on the horizontal image element of MC question
/// </summary>
public class Q_QuizHorizontalImageUI : MonoBehaviour
{
    [SerializeField] private RawImage image;

    //Set it up!
    public void SetUp(Q_QuizHorizontalImage horizontalImage)
    {
        if (horizontalImage.Image!=null)
        {
            image.texture = horizontalImage.Image;
            image.GetComponent<AspectRatioFitter>().aspectRatio = (float)horizontalImage.Image.width/(float)horizontalImage.Image.height;
        }
    }
}
