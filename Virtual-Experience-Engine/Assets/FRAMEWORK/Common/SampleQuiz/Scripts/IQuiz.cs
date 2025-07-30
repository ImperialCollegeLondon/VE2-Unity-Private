using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class QuizCompleteEvent : UnityEvent<int> { }
public interface IQuiz 
{
    /// <summary>
    /// Show the quiz. If this is showing in the main UI, it will be opened. If the quiz has been hidden or closed after finishing, this will carry
    /// on from where it left off (which may be the completion page if the quiz was closed at the end).
    /// </summary>
    public void Show();

    /// <summary>
    /// Hide the quiz - the equivalent to using the hide 'X' button, or using close on the quiz completion page. You can perform this programmatically
    /// using this method even if the quiz is not set to allow hiding.
    /// </summary>
    public void Hide();

    /// <summary>
    /// Calculate the total score for the quiz, as an integer. Use in combination with GetMaxScore to work out a percentage.
    /// Questions not yet attempted will be scored as 0
    /// </summary>
    /// <returns>An integer giving the score</returns>
    public int GetTotalScore();

    /// <summary>
    /// Gives the question index that the quiz is currently on. Initially this will be 0 (meaning first question). If the quiz is complete,
    /// this returns the number of questions in the quiz (so for a 3 question quiz, 0 means first question, 2 means 3rd question, 3 means finished).
    /// To find details of the question, use this value to index into the list returned by GetQuestions, but remember to check that the quiz is not
    /// finished to avoid an index error.
    /// </summary>
    /// <returns></returns>
    public int GetCurrentQuestionNumber();

    /// <summary>
    /// Reset the quiz - remove all data entered so far, and set the question number to 0 (first question). If the quiz is using the MainUI, this will
    /// also re-open the UI if it is closed
    /// </summary>
    public void ResetQuiz();

    /// <summary>
    /// Get a list of all the questions in the quiz (assembled by ViRSE by looking for children of the quiz gameobject that contain a V_QuizQuestion script).
    /// </summary>
    /// <returns>A list of IQuizQuestions</returns>
    public List<IQuizQuestion> GetQuestions();

    /// <summary>
    /// Change question by adding an offset to the current question number - so MoveQuestion(1) moves on one, and MoveQuestion(-1) moves back one.
    /// You can use larger offsets if you want to, and ViRSE will not let you move off either end of the quiz - so MoveQuestion(-9999) will move to the first question,
    /// unless you have more than 10000 questions in your quiz! Note that you CAN move past the last question (by 1), onto the completion page (see GetCurrentQuestionNumber).
    /// </summary>
    /// <param name="offset"></param>
    public void MoveQuestion(int offset);

    /// <summary>
    /// Calculate the maximum possible score for the quiz, as in integer. Use in combination with GetTotalScore to work out a percentage.
    /// </summary>
    /// <returns></returns>
    public int GetMaxScore();

    /// <summary>
    /// Advanced! Forces ViRSE to rebuild the quiz from scratch. Use this if you have used code to rearrange question order, or disabled/enabled questions.
    /// </summary>
    public void RePopulateQuestions();

    
    public enum NumberMode { none, digits, romanNumerals, letters }

    public enum NetworkMode { local, group }

    /// <summary>
    /// The title of the quiz - this can be read or set in code. Changes will only take effect when a new question is displayed.
    /// </summary>
    public string QuizTitle { get; set; }

    

    /// <summary>
    /// Whether ViRSE provides a 'back' button for each question. Changes will only take effect when a new question is displayed.
    /// Note that you can move back or forwards through questions programmatically with MoveQuestion even if this is CanGoBack is false;
    /// </summary>
    public bool CanGoBack { get; set; }

    /// <summary>
    /// The type of question numbering used - this can be read but not set in code.
    /// </summary>
    public NumberMode QuestionNumbering { get; }

    /// <summary>
    /// The type of answer numbering used - this can be read but not set in code.
    /// </summary>
    public NumberMode AnswerNumbering { get; }

    /// <summary>
    /// The type of networking used (local or group) - this can be read but not set in code.
    /// </summary>
    public NetworkMode NetworkingMode { get; }

    /// <summary>
    /// A background colour for the top portion of the quiz UI (title and question). Changes will only take effect when a new question is displayed.
    /// Set this to transparent if you want to use the 'natural' colour of the canvas the quiz is displayed on.
    /// </summary>
    public Color QuestionBackgroundColour { get; set;  }

    /// <summary>
    /// A background colour for the main portion of the quiz UI (answers/images/textboxes/butons). Changes will only take effect when a new question is displayed.
    /// Set this to transparent if you want to use the 'natural' colour of the canvas the quiz is displayed on.
    /// </summary>
    public Color AnswerBackgroundColour { get; set; }


    /// <summary>
    /// The RectTransform of the GameObject that you want the quiz to appear in. If this is set to null, the quiz will display in the Main UI. 
    /// This can be set in code, but changing it while the quiz is open is not advised.
    /// </summary>
    public RectTransform RectTransformHolder { get; set; }

    /// <summary>
    /// The message displayed on the completion page, after the final question. Changes will not take effect if the completion page is currently displayed.
    /// </summary>
    public string CompletionMessage { get; set; }

    /// <summary>
    /// Whether ViRSE should show the correct answers after the submit button it pressed.
    /// </summary>
    public bool RevealAnswersAfterEachQuestion { get; set; }

    /// <summary>
    /// Whether ViRSE should show the question score after the submit button it pressed.
    /// </summary>
    public bool RevealScoresAfterEachQuestion { get; set; }

    /// <summary>
    /// Whether ViRSE should provide a summary of scores by questions on the completion page at the end of the quiz. Changes will not take effect if the completion page is currently displayed.
    /// </summary>
    public bool RevealScoresAtEndOfQuiz { get; set; }

    /// <summary>
    /// Whether ViRSE should give the total quiz score (and percentage) at the end of the quiz. Changes will not take effect if the completion page is currently displayed.
    /// </summary>
    public bool RevealTotalScoreAtEndOfQuiz { get; set; }

    /// <summary>
    /// Whether ViRSE should show quiz progress (e.g. Question 5/7) during the quiz. Changes will only take effect when a new question is displayed.
    /// </summary>
    public bool ShowQuestionPosition { get; set; }

    /// <summary>
    /// Is the quiz currently showing? Note that if the quiz is placed on the main UI and this is hidden manually by the user, it will still count as showing.
    /// This property is read-only. To change showing, use the Show() or Hide() methods.
    /// </summary>
    public bool Showing { get; }

    /// <summary>
    /// Is the user allowed to hide the quiz (with the X button top right)? Changes will only take effect when a new question is displayed.
    /// </summary>
    public bool CanHide { get; set;  }


    /// <summary>
    /// Are sound effects provided during the quiz?
    /// </summary>
    public bool SoundEffects { get; set; }

    /// <summary>
    /// Is quiz sound 3D/positional? If false, it is 2D (everyone hears it). If true, sound comes from position of the V_Quiz gameobject (not the canvas).
    /// </summary>
    public bool SoundPositional { get; set; }


    public Q_Pointer PointerForQuiz { get;}
}
