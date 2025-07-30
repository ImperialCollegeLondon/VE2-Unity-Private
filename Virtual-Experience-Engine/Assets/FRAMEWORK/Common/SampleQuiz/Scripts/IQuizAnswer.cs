using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static IQuizQuestion;
public interface IQuizAnswer 
{
    public enum ImagePositions { none, leftOfAnswer, betweenAnswerAndCheckBox, rightOfCheckBox };

    /// <summary>
    /// The index number of the answer (0-indexed).
    /// </summary>
    public int Index { get; }

    /// <summary>
    /// The text for the answer. Changes will not update questions currently displayed.
    /// </summary>
    public string AnswerText { get; set; }

    /// <summary>
    /// How many points are scored if this answer is ticked? Negative values are allowed.
    /// </summary>
    public int PointsForTicked { get; set; }

    /// <summary>
    /// How many points are scored if this answer is NOT ticked? Negative values are allowed.
    /// This will be zero in most scenarios, but some multi-answer multiple choice quizzes
    /// give points for NOT ticking incorrect answers.
    /// </summary>
    public int PointsForUnticked { get; set; }

    /// <summary>
    /// Do you want an Image in the answer line, and if so, where should it be displayed? Changes will not update questions currently displayed.
    /// </summary>
    public ImagePositions ImagePosition { get; set; }

    /// <summary>
    /// Texture2D for the image to display. Changes will not update questions currently displayed.
    /// </summary>
    public Texture2D Image { get; set; }

    /// <summary>
    /// What percentage of the answer-line width should the image occupy? Changes will not update questions currently displayed.
    /// Note that answer lines are normally much wider than high, so images with a typical aspect ration, a low percentage will work best.
    /// </summary>
    public float ImageWidthPercentage { get; set; }


    /// <summary>
    /// Has the user ticked this answer?
    /// </summary>
    public bool Ticked { get; }

    /// <summary>
    /// Calculate and return the score for the question
    /// </summary>
    public int GetScore();

}
