
using System;
using UnityEngine;

/// <summary>
/// Encapsulates a multiple-choice answer. Should be on a gameobject underneath a V_QuizQuestionMultipleChoice.
/// </summary>
public class Q_QuizAnswer : Q_QuizMCItem, IQuizAnswer
{
    [Header("Multiple choice answer settings")]
    //[Tooltip("V_QuizAnswer should be the child of a V_QuizQuestionMultipleChoice GameObject.\n\n" +
    //    "To access the answer from plugin code, use GetComponent<IQuizAnswer>() on this GameObject.\n\nExample: if your script is on this gameobject, check if the answer is ticked using:\nbool isTicked = gameObject.GetComponent<IQuizAnswer>().Ticked;")]
    [Tooltip("The text for the answer")]
    [SerializeField] string answerText;

    [Tooltip("Points scored if this answer is selected - can be negative for multi-answer questions")]
    [SerializeField] int pointsForTicked;

    [Tooltip("Points scored if this answer is NOT selected. Should normally be 0.")]
    [SerializeField] int pointsForUnticked;

    [Header("Optional per-answer image settings")]
    [Tooltip("Should there be an image, and if so, where positioned? Leave as 'None' for no image.")]
    [SerializeField] IQuizAnswer.ImagePositions imagePosition = IQuizAnswer.ImagePositions.none;
    [Tooltip("Texture2D asset for the image")]
    [SerializeField] Texture2D image;
    [Tooltip("What percentage of the answer-width should the image take up? Maximum is 50%")]
    [SerializeField][Range(0f, 50f)] float imageWidthPercentage = 20f;

    //Properties
    public int Index { get => index; set => index = value; }
    public string AnswerText { get => answerText; set => answerText = value;  }
    public int PointsForTicked { get => pointsForTicked; set => pointsForTicked = value;  }
    public int PointsForUnticked { get => pointsForUnticked; set => pointsForUnticked = value;  }
    public IQuizAnswer.ImagePositions ImagePosition { get => imagePosition; set => imagePosition = value; }
    public Texture2D Image { get => image; set => image = value; }
    public float ImageWidthPercentage { get => imageWidthPercentage; set => imageWidthPercentage = value; }
    public bool Ticked { get => ticked; set => ticked = value; }

    //local data
    private int index;
    private bool ticked = false;

    //UI reference
    [HideInInspector] public Q_QuizAnswerUI answerUI;
    
    public void UnSet()
    {
        answerUI?.UnSet();
        ticked= false;
    }

    public int GetScore()
    {
        if (ticked) return pointsForTicked;
        else return pointsForUnticked;
    }

    public void Reset()
    {
        ticked = false;
    }
}
