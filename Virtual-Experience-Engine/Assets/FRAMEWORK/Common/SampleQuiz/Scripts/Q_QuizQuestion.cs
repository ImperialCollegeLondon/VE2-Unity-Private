
using System;
using System.Collections.Generic;
using UnityEngine;
using VE2.Common.API;

[DisallowMultipleComponent]
/// <summary>
/// Base class for quiz questions. 
/// </summary>
public abstract class Q_QuizQuestion : MonoBehaviour, IQuizQuestion
{
    [Tooltip("Invokes when the question is submitted, passes the score attained for this question")]
    public QuizQuestionCompleteEvent OnQuestionComplete;

    [Header("Question in quiz")]
    //[Tooltip("Quiz Question should be the child of a V_Quiz GameObject.")]

    [Tooltip("Text for the question. Add a question mark, but not a question number!")]
    [SerializeField] string questionText;
    [SerializeField] bool canSkip;

    [Header("Settings for optional image to left or right of answers")]
    [SerializeField] IQuizQuestion.ImagePosition verticalImagePosition = IQuizQuestion.ImagePosition.none;
    [Tooltip("What percentage of available width should the image occupy?")]
    [SerializeField][Range(10f, 75f)] float verticalImageWidthPercentage = 50f;
    [Tooltip("Texture2D asset containing the image")]
    [SerializeField] Texture2D verticalImage;

    //local data
    protected bool submitted = false;
    protected int score = 0;
    protected GameObject QuestionUI;

    protected Q_Quiz quiz;
    private int questionNumber;
    private bool revealMode = false;

    //properties
    public string QuestionText { get => questionText; set => questionText = value; }
    public bool CanSkip { get => canSkip; set => canSkip = value; }
    public bool Submitted { get => submitted; }
    public int Score { get => score; }
    public Q_Quiz Quiz { get => quiz; }
    public int QuestionNumber { get => questionNumber; }
    public IQuizQuestion.ImagePosition VerticalImagePosition { get => verticalImagePosition; set => verticalImagePosition = value; }
    public Texture2D VerticalImage { get => verticalImage; set => verticalImage = value; }
    public float VerticalImageWidthPercentage { get => verticalImageWidthPercentage; set => verticalImageWidthPercentage=value;}
    public abstract bool MultipleAnswersAllowed { get; }
    public abstract bool NoAnswers { get; }
    public abstract string Text { get; set; }
    public abstract int MaximumCharacters { get; set; }
    public abstract bool ProvideResponse { get; set; }
    public abstract bool MultiLine { get; }
    public abstract bool Valid { get; }
    public abstract string Response { get; }
    public abstract string CorrectTextForAnswerReveal { get; set; }
    public abstract bool AllOrNothingMultipleAnswerScoring { get; set; }
    public abstract int AllOrNothingCorrectAnswerScore { get; set; }
    public abstract IQuizQuestion.TextScorer textScorer { get; set; }
    public abstract IQuizQuestion.TextValidator textValidator { get; set; }
    public abstract IQuizQuestion.TextResponder textResponder { get; set; }
    public bool RevealMode { get => revealMode;  }

    //Abstract & virtual methods for implementing by derived classes
    public virtual void Show()
    {
        quiz.CloseExcept(this);
        revealMode= false;
    }

    protected abstract int ComputeScore();
    public abstract bool Validate();
    public abstract int GetMaxScore();
    protected abstract void ResetQuestionDerived();
    //Internal API

    //on construction
    public void SetQuizAndQuestion(Q_Quiz v_Quiz, int questionNo)
    {
        quiz = v_Quiz;
        questionNumber = questionNo;
        ResetQuestion();
        Populate();
    }

    protected abstract void Populate();

    //Called by UI when submit is clicked - do reveal or not, advance or not
    public void Submit()
    {
        submitted = true;
        ComputeScore();

        quiz.PlaySound(Q_Quiz.QuizSounds.submit);

        bool revealAnswers = quiz.RevealAnswersAfterEachQuestion;

        //if it's text and no answer is given - cannot reveal answers
        if (GetType() == typeof(Q_QuizQuestionText))
            if ((this as Q_QuizQuestionText).CorrectTextForAnswerReveal == "")
                revealAnswers = false;

        //if max score is 0 there is no scoring - cannot reveal score
        bool revealScores = quiz.RevealScoresAfterEachQuestion;
        if (GetMaxScore()==0)
                revealScores = false;

        try
        {
            OnQuestionComplete?.Invoke(score);
            Debug.LogWarning("Question complete " + score);
        }
        catch (Exception ex)
        {
            Debug.LogWarning(ex.Message);
        }

        revealMode = true; //needs to be in place BEFORE do post submission stuff
        
        QuestionUI.GetComponent<Q_QuizQuestionUI>().DoPostSubmissionStuff();
        if (!revealAnswers && !revealScores)
        {
            revealMode = false;
            Skip(); //does a sendstate
            return;
        }
        else
        {
            Debug.Log("Doing reveal/scores");
            quiz.SendState();
        }
    }

    private string uploadedAnswers="####";
    private void UploadAnswers()
    {

        if (quiz.NetworkingMode == IQuiz.NetworkMode.local || VE2API.InstanceService.IsHost)
        {
            string newAnswers = GetAnswerText();
            //if (newAnswers!=uploadedAnswers)
            //    V_MasterNetworkController.StoreQuiz(Quiz.QuizTitle, Quiz.NetworkingMode == IQuiz.NetworkMode.group,
            //        questionNumber, questionText, score, GetTypeString(), newAnswers , Quiz.AnonymousUploads);
            Debug.LogWarning("TODO  - handle answer upload");
            uploadedAnswers= newAnswers;
        }
    }

    //skip button clicked
    public void Skip()
    { 
        quiz.MoveQuestion(1);
    }

    //back button clicked
    public void Back()
    {
        quiz.MoveQuestion(-1);
    }

    //Teardown UI
    public void DestroyUI()
    {
        if (QuestionUI!= null) Destroy(QuestionUI);
        QuestionUI = null;
    }
    
    //Passthrough to validate method on UI 
    public void UIValidate()
    {
        QuestionUI?.GetComponent<Q_QuizQuestionUI>()?.Validate();
    }

    public void ResetQuestion()
    {
        submitted = false;
        score = 0;
        revealMode = false;
        ResetQuestionDerived(); //call derived
        DestroyUI();
    }

    public abstract bool IsText();
    public abstract bool IsMC();
    public abstract List<IQuizAnswer> GetAnswers();
    public abstract List<IQuizHorizontalImage> GetHorizontalImages();

    protected abstract string GetTypeString();

    protected abstract string GetAnswerText();


    public void ChangeSubmit(bool submit, bool revealMode)
    {
        if (submit && !submitted || (revealMode && !this.revealMode))
        {
            //this is a change to submit
            Submit();
        }

        if (!submit && submitted)
        {
            //this is a reset probably. it's a rebuild
            submitted = false;
            if (QuestionUI!=null) 
                Destroy(QuestionUI);
            Show(); 
        }
    }

    public void SetScoreAndSubmit(int score, bool submit)
    {
        this.score = score;
        this.submitted = submit;
    }

    public void ResetRevealMode()
    {
        revealMode= false;
    }
}
