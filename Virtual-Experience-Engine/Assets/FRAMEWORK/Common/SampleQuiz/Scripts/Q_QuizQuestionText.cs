
using System.Collections.Generic;
using TMPro;
using UnityEngine;


/// <summary>
/// Derived text question class - should be placed on a gameobject child of a V_Quiz
/// If validation/scoring/response functionality is required, callback methods need
/// to be connected - see interface internal documentation
/// </summary>
public class Q_QuizQuestionText : Q_QuizQuestion
{
    //[Tooltip("To access the question from plugin code, use GetComponent<IQuizQuestion>() on this GameObject.\n\nExample: if your script is on this gameobject, get text for question using:\nstring text = gameObject.GetComponent<IQuizQuestion>().GetText();")]

    [Header("Settings for text question")]
    [Tooltip("Maximum characters allowed")]
    [SerializeField] int maximumCharacters = 100;
    [Tooltip("Provide larger multi-line box")]
    [SerializeField] bool multiLine = false;
    [Tooltip("Turn on text-response (validation hints). Requires connection of a callback validation method in code.")]
    [SerializeField] bool provideResponse = false;
    [Tooltip("Highest possible score. Calculation of score requires connection of a callback validation method in code.\nLeave Max Score as 0 to switch off scoring for this question.")]
    [SerializeField] int maxScore = 0;
    [Tooltip("Text to show when answers are revealed (if applicable) - e.g. correct answer. Leave blank to skip reveal for this question.")]
    [SerializeField] string correctTextForAnswerReveal = "";

    //local data
    private bool valid =true;
    private string response = "";
    private Q_QuizQuestionUI quizQuestionUI = null;
    private IQuizQuestion.TextResponder localTextResponder= null;
    private IQuizQuestion.TextValidator localTextValidator = null;
    private IQuizQuestion.TextScorer localTextScorer = null;

    [HideInInspector] private string text;

    //Properties
    public override string Text { get => text; set => text = value; }
    public override int MaximumCharacters { get => maximumCharacters; set => maximumCharacters = value; }
    public override bool MultiLine { get => multiLine;  }
    public override bool ProvideResponse { get => provideResponse; set => provideResponse = value; }
    public override string CorrectTextForAnswerReveal { get => correctTextForAnswerReveal; set => correctTextForAnswerReveal = value; }
    public override bool Valid { get => valid;}
    public override string Response { get => response; }

    public override bool MultipleAnswersAllowed { get => false; }

    public override bool NoAnswers { get => false; }
    public override bool AllOrNothingMultipleAnswerScoring { get => false; set => throw new System.NotImplementedException(); }
    public override int AllOrNothingCorrectAnswerScore { get => 0; set => throw new System.NotImplementedException(); }

    public override IQuizQuestion.TextScorer textScorer { get => localTextScorer; set => localTextScorer = value; }
    public override IQuizQuestion.TextValidator textValidator { get => localTextValidator; set => localTextValidator = value; }
    public override IQuizQuestion.TextResponder textResponder { get => localTextResponder; set => localTextResponder = value; }



    //Abstract implementations (all simple)
    protected override int ComputeScore()
    {
        return score = ScoreText(quizQuestionUI.GetTextInTextField());
    }

    public override bool Validate()
    {
        if (quizQuestionUI ==null)
        {
            valid = false;
            return false;
        }
        return valid = ValidateText(quizQuestionUI.GetTextInTextField());
    }

    public override void Show()
    {
        if (QuestionUI == null)
        {
            QuestionUI = Instantiate(quiz.QuizQuestionPrefab, Quiz.RectTransformHolder);
            quizQuestionUI = QuestionUI.GetComponent<Q_QuizQuestionUI>();
            quizQuestionUI.SetUpText(this);
        }
        base.Show();
    }

    public override int GetMaxScore()
    {
        return maxScore;
    }

    protected override void ResetQuestionDerived()
    {
        valid = true;
        response = "";
        text = "";
    }

    //Respond/Validate/Score interface
    //Inherit from this class and override ScoreText, ValidateText, RespondToText to implement this functionality

    public string Respond()
    {
        return response = RespondToText(quizQuestionUI.GetTextInTextField());
    }

    protected int ScoreText(string text)
    {
        if (localTextScorer!=null)
            return localTextScorer.Invoke(text);
        else
            return 0;
    }

    protected bool ValidateText(string text)
    {  
        if (localTextValidator != null)
            return localTextValidator.Invoke(text);
        else
            return true;
    }

    protected string RespondToText(string text)
    {
        if (localTextResponder != null)
            return localTextResponder.Invoke(text);
        else
            return "";
    }

    protected override void Populate()
    {
        return;
    }

    public override bool IsText()
    {
        return true;
    }

    public override bool IsMC()
    {
        return false;
    }

    public override List<IQuizAnswer> GetAnswers()
    {
        return null;
    }

    public override List<IQuizHorizontalImage> GetHorizontalImages()
    {
        return null;
    }

    protected override string GetTypeString()
    {
        return "Text";
    }

    protected override string GetAnswerText()
    {
        return text;
    }

    public void ChangeText(string answerText)
    {
        text = answerText;
        if (QuestionUI != null) QuestionUI.GetComponent<Q_QuizQuestionUI>().SetTextRemotely(text);
        Quiz.SendState();
    }
}
