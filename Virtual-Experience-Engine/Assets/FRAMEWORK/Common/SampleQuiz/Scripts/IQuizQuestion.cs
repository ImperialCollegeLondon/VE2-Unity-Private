using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class QuizQuestionCompleteEvent : UnityEvent<int> { }
public interface IQuizQuestion
{
    public enum ImagePosition { none, left, right };

    /// <summary>
    /// The text of the question (to be displayed under the quiz title at the top). Changes will not affect currently displayed questions.
    /// </summary>
    public string QuestionText { get; set; }

    /// <summary>
    /// Whether ViRSE should display a 'skip' buttton for this question. Changes will not affect currently displayed questions.
    /// Note that you can programmatically skip using IQuiz.MoveQuestion even if CanSkip is set to false.
    /// </summary>
    public bool CanSkip { get; set; }

    /// <summary>
    /// Has this question been submitted or not?
    /// </summary>
    public bool Submitted { get; }

    /// <summary>
    /// The user's score for this question - this can be read before the question is submitted.
    /// </summary>
    public int Score { get; }

    /// <summary>
    /// The index number of this question in the quiz (zero-indexed, so 1 is the 2nd question)
    /// </summary>
    public int QuestionNumber { get; }

    /// <summary>
    /// Whether ViRSE should display a vertical image to the left of right of the answer space.  Changes will not affect currently displayed questions.
    /// </summary>
    public ImagePosition VerticalImagePosition { get; set; }

    /// <summary>
    /// Texture2D Image for the vertical image (ignored if position is set to 'None').  Changes will not affect currently displayed questions.
    /// </summary>
    public Texture2D VerticalImage { get; set; }

    /// <summary>
    /// What percentage of available width the image should occupy (ignored if position is set to 'None').  Changes will not affect currently displayed questions.
    /// </summary>
    public float VerticalImageWidthPercentage { get; set; }

    /// <summary>
    /// If this is a text question, gives the text typed in by the user (even if not yet submitted).
    /// If this is a multiple choice question, gives an empty string
    /// Setting this property will update the text box for a text question, or throw an exception for a multiple choice question
    /// </summary>
    public string Text { get; set; }

    /// <summary>
    /// If this is a text question, the maximum length of text that can be typed in. Changes will not affect currently displayed questions.
    /// Setting this property will throw an exception for a multiple choice question
    /// </summary>
    public int MaximumCharacters { get; set; }

    /// <summary>
    /// Whether ViRSE displays a Response line as the user types in a text question.
    /// Setting this property will throw an exception for a multiple choice question.
    /// Note that response line code requires registration of a callback to perform
    /// the response generation - see textResponder.
    /// </summary>
    public bool ProvideResponse { get; set; }

    /// <summary>
    /// Whether ViRSE should use a large multi-line input box for text.
    /// Setting this property will throw an exception for a multiple choice question.
    /// </summary>
    public bool MultiLine { get; }

    /// <summary>
    /// Is the input valid for submission (i.e. is ViRSE able to display a submit button)?
    /// </summary>
    public bool Valid { get; }

    /// <summary>
    /// The currently displayed response to text input. This will be an empty string if this
    /// is not a text question, or no response method has been registered
    /// </summary>
    public string Response { get; }

    /// <summary>
    /// The text that will be shown if this is a text question and ViRSE has been instructed to
    /// reveal correct answers after each question. If this is blank, no text will be shown, and
    /// if no score is being revealed either, the 'reveal' is skipped and ViRSE advances to the
    /// next question.
    /// </summary>
    public string CorrectTextForAnswerReveal { get; set; }

    /// <summary>
    /// Are multiple answers allowed? This is only applicable to Multiple Choice questions - it will
    /// always be false for text questions.
    /// </summary>
    public bool MultipleAnswersAllowed { get; }

    /// <summary>
    /// True if this is a multiple choice question without answers (e.g. just a Horizontal Image)
    /// </summary>
    public bool NoAnswers { get; }

    /// <summary>
    /// Should ViRSE use 'All or Nothing' scoring for multi-answer multiple choice questions? All or 
    /// nothing scoring provides the full score if the best possible answer is given, otherwise the
    /// user scores zero. This has no effect if multiple answers are not allowed, and setting this for  a
    /// text question will result in an exception.
    /// </summary>
    public abstract bool AllOrNothingMultipleAnswerScoring { get; set; }
    /// <summary>
    /// If 'All or Nothing' scoring is in use (see AllOrNothingMultipleAnswerScoring), what score should be
    /// assigned to a perfect answer? Setting this for a text quesiton will result in an exception.
    /// </summary>
    public abstract int AllOrNothingCorrectAnswerScore { get; set; }

    /// <summary>
    /// Is this a text question?
    /// </summary>
    /// <returns></returns>
    public bool IsText();

    /// <summary>
    /// Is this a multiple choice (MC) question?
    /// </summary>
    /// <returns></returns>
    public bool IsMC();

    /// <summary>
    /// Reset all data for the question.
    /// </summary>
    public void ResetQuestion();

    /// <summary>
    /// Calculate the highest possible score for the question - this works on both text and multiple
    /// choice questions. Note that score assignment for text questions requires the registration of
    /// a callback scoring method (see textScorer)
    /// </summary>
    /// <returns></returns>
    public int GetMaxScore();

    /// <summary>
    /// Returns a list of all multiple choice answers associated with this question. If this is a text
    /// question, it returns null.
    /// </summary>
    /// <returns></returns>
    public List<IQuizAnswer> GetAnswers();

    /// <summary>
    /// Returns a list of all horizontal images associated with this question. If this is a text
    /// question, it returns null.
    /// </summary>
    /// <returns></returns>
    public List<IQuizHorizontalImage> GetHorizontalImages();

    public delegate bool TextValidator(string text);
    public delegate string TextResponder(string text);
    public delegate int TextScorer(string text);

    /// <summary>
    /// Callback delegate for text scoring. If this is a text question that requires scoring, register 
    /// your method to compute score from the text string using this. The custom method should take a 
    /// string as an argument, and return an int. See provided samples for an example.
    /// </summary>
    public TextScorer textScorer { get; set; }

    /// <summary>
    /// Callback delegate for text validation (invalid text cannot be submitted).
    /// If this is a text question that requires validation, register 
    /// your method to compute validation from the text string using this. The custom method should take a 
    /// string as an argument, and return a bool. See provided samples for an example.
    /// </summary>
    public TextValidator textValidator { get; set;  }

    /// <summary>
    /// Callback delegate for text response (feedback text responding live to input).
    /// If this is a text question that requires response, register 
    /// your method to compute response from the text string using this. The custom method should take a 
    /// string as an argument, and return a string. See provided samples for an example. Note that 
    /// response will not appear if Response is set to false.
    /// </summary>
    public TextResponder textResponder { get; set; }
}
