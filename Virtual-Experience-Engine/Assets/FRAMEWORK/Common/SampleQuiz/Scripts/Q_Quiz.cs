
using System;
using System.Collections.Generic;

using UnityEngine;
using VE2.Common.API;
using VE2.NonCore.Instancing.API;
using UnityEngine.UI;

[DisallowMultipleComponent]
/// <summary>
/// Master quiz class - place at the root of the quiz hierarchy. Assumes that there are child gameobjects containing questions.
/// </summary>
public class Q_Quiz : MonoBehaviour
{
    [Tooltip("Invoked when the quiz ends, passes the total score, divide by GetMaxScore() to find the percentage")]
    public QuizCompleteEvent OnQuizComplete;

    [Header("Main Quiz Script")]
    //[Tooltip("V_Quiz expects question scripts on child objects.\n\n" +
    //    "To access the quiz from plugin code, use GetComponent<IQuiz>() on this GameObject.\n\nExample: if your script is on this gameobject, show the quiz using:\ngameObject.GetComponent<IQuiz>().Show();")]
    [Tooltip("Main title - appears at top of quiz")]
    [SerializeField] string quizTitle;

    [Header("Settings for Quiz appearance")]
    [Tooltip("Specify a RectTransform in which the quiz appears. Leave this as 'none' to use the main UI.")]
    [SerializeField] RectTransform rectTransformToShowIn;

    [Tooltip("Specify a RectTransform in which the quiz appears. Leave this as 'none' to use the main UI.")]
    [SerializeField] GraphicRaycaster graphicRaycaster;

    [Tooltip("Display question number as progress (e.g. 'Question 3/5')")]
    [SerializeField] bool showQuestionPosition = true;

    [Tooltip("Start the quiz in the supplied rect on play")]
    [SerializeField] bool showOnStart = false;


    [Tooltip("Numbering format for questions")]
    [SerializeField] IQuiz.NumberMode questionNumbering = IQuiz.NumberMode.digits;
    [Tooltip("Numbering format for multiple choice answers")]
    [SerializeField] IQuiz.NumberMode answerNumbering = IQuiz.NumberMode.letters;
    [Tooltip("Colour for title bar with quiz title and question")]
    [SerializeField] Color questionBackgroundColour = Color.blue / 2f;
    [Tooltip("Colour for main body of quiz, containing answers/images/text boxes")]
    [SerializeField] Color answerBackgroundColour = Color.blue / 2f;
    [Tooltip("Message to show at the end of the quiz")]
    [SerializeField] string completionMessage = "Quiz complete";

    [Header("Settings for Quiz behaviour")]
    [Tooltip("Can the user go back and re-answer questions?")]
    [SerializeField] bool canGoBack;

    [Tooltip("Can the user hide the quiz before completion?")]
    [SerializeField] bool canHide;

    [Tooltip("Should Quiz show correct answers after questions are submitted?")]
    [SerializeField] bool revealAnswersAfterEachQuestion;

    [Tooltip("Should Quiz reveal the assigned score after questions are submitted?")]
    [SerializeField] bool revealScoresAfterEachQuestion;

    [Tooltip("Should Quiz reveal/recap scores for each question at the end of the quiz?")]
    [SerializeField] bool revealScoresAtEndOfQuiz;

    [Tooltip("Should Quiz provide a total score for the quiz at the end?")]
    [SerializeField] bool revealTotalScoreAtEndOfQuiz;

    [Tooltip("Should Quiz use sound effects during the quiz?")]
    [SerializeField] bool soundEffects = true;

    [Tooltip("Should sound (if used) be positional? If false it will be 2D - same volume for everyone.")]
    [SerializeField] bool soundPositional = false;

    [Tooltip("How should the quiz work in multi-user environments?" +
        "\nLocal: quiz is per-user." +
        "\nGroup: quiz is per-instance - all users see the same synchronised quiz, and any user can answer questions."
        )]
    [SerializeField] IQuiz.NetworkMode networkingMode;


    [SerializeField] private AudioClip quizSubmitSound;
    [SerializeField] private AudioClip quizCorrectSound;
    [SerializeField] private AudioClip quizAnswerWrongSound;
    [SerializeField] private AudioClip quizButtonSound;
    [SerializeField] private TMPro.TMP_FontAsset font;
    [SerializeField] private bool usePointer = true;
    [SerializeField] private Q_Pointer pointerForQuiz;

    //Properties
    public string QuizTitle { get => quizTitle; set => quizTitle = value; }
    public bool CanGoBack { get => canGoBack; set => canGoBack = value; }
    public IQuiz.NumberMode QuestionNumbering { get => questionNumbering; set => questionNumbering = value; }
    public IQuiz.NumberMode AnswerNumbering { get => answerNumbering; set => answerNumbering = value; }
    public IQuiz.NetworkMode NetworkingMode { get => networkingMode; }
    public Color QuestionBackgroundColour { get => questionBackgroundColour; set => questionBackgroundColour = value; }
    public Color AnswerBackgroundColour { get => answerBackgroundColour; set => answerBackgroundColour = value; }
    public RectTransform RectTransformHolder { get => rectTransformToShowIn; set => rectTransformToShowIn = value; }
    public string CompletionMessage { get => completionMessage; set => completionMessage = value; }
    public bool ShowQuestionPosition { get => showQuestionPosition; set => showQuestionPosition = value; }
    public bool Showing { get => showing; }
    public bool CanHide { get => canHide; set => canHide = value; }
    public bool RevealAnswersAfterEachQuestion { get => revealAnswersAfterEachQuestion; set => revealAnswersAfterEachQuestion = value; }
    public bool RevealScoresAfterEachQuestion { get => revealScoresAfterEachQuestion; set => revealScoresAfterEachQuestion = value; }
    public bool RevealScoresAtEndOfQuiz { get => revealScoresAtEndOfQuiz; set => revealScoresAtEndOfQuiz = value; }
    public bool RevealTotalScoreAtEndOfQuiz { get => revealTotalScoreAtEndOfQuiz; set => revealTotalScoreAtEndOfQuiz = value; }

    public string ControllerDisplayName { get => controllerDisplayName; }
    public bool SoundEffects { get => soundEffects; set => soundEffects = value; }

    public bool SoundPositional { get => soundPositional; set => SoundPositionalChanged(value); }
    public GameObject QuizCompletePrefab { get => quizCompletePrefab; }
    public GameObject QuizQuestionPrefab { get => quizQuestionPrefab; }

    public Q_Pointer PointerForQuiz { get => pointerForQuiz; }
    public TMPro.TMP_FontAsset Font { get => font; }

    private void SoundPositionalChanged(bool value)
    {
        soundPositional = value;
        if (soundPositional)
            audioSource.spatialBlend = 1f;
        else
            audioSource.spatialBlend = 0f;
    }

    // local data
    private bool defaultCanvas = false;
    private bool complete = false;
    private int currentQuestionNumber = 0;
    private Dictionary<int, bool> wasChildDisabled = new Dictionary<int, bool>();
    private bool showing = false;
    private Q_QuizCompletionUI finalUI = null;
    private string controllerID = "";
    private string controllerDisplayName = "";
    private IV_NetworkObject quizSyncer;
    private IV_InstantMessageHandler imHandler;
    private AudioSource audioSource;


    [SerializeField] GameObject quizCompletePrefab, quizQuestionPrefab;


    private void Awake()
    {
        MakeSoundSource();
    }

    public void Start()
    {
        if (!usePointer && networkingMode == IQuiz.NetworkMode.group)
            Debug.LogError("You MUST use a pointer for group quizzes");

        if (usePointer)
        {
            if (pointerForQuiz == null)
            {
                Debug.LogError("No pointer specified!");
            }
            else
            {
                DisableGraphicsRaycaster(); //pointer interactions only
            }
        }
        else
            pointerForQuiz = null;


        PopulateQuestions();

        if (networkingMode == IQuiz.NetworkMode.group) SetupSyncers();

        if (showOnStart)
            Show();
    }

    private void DisableGraphicsRaycaster()
    {
        graphicRaycaster.enabled = false;
    }

    private void MakeSoundSource()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        SoundPositionalChanged(soundPositional);
    }

    /// <summary>
    /// Create a syncer to handle group quiz
    /// </summary>
    private void SetupSyncers()
    {
        quizSyncer = gameObject.GetComponent<IV_NetworkObject>();
        if (quizSyncer == null)
            Debug.LogError("Group quiz needs a V_NetworkObject");
        quizSyncer.OnDataChange.AddListener(ReceiveRawUpdate);

        imHandler = gameObject.GetComponent<IV_InstantMessageHandler>();
        if (imHandler == null)
            Debug.LogError("Group quiz needs a V_InstantMessageHandler");
        imHandler.OnMessageReceived.AddListener(IMReceived);
    }

    private void IMReceived(object arg0)
    {
        if (VE2API.InstanceService.IsHost)
        {
            Tuple<int, string> data = (Tuple<int, string>)arg0;
            int questionID = data.Item1;
            string newString = data.Item2;
            (questions[questionID] as Q_QuizQuestionText).ChangeText(newString); //triggers a send state

        }
    }


    //--------API 

    /// <summary>
    /// Start quiz - from current question. Might be continuation.
    /// </summary>
    public void Show()
    {
        if (gameObject.activeSelf==false) gameObject.SetActive(true);

        if (!showing)
        {
            PopulateQuestions(); //can't get called twice

            RectTransform rt = GetShowRectTransform();

            wasChildDisabled.Clear();

            //disable any current children of the rectTransform
            //storing original state for later
            for (int i = 0; i < rt.childCount; i++)
            {
                GameObject child = rt.GetChild(i).gameObject;
                int id = child.GetInstanceID();
                wasChildDisabled[id] = child.activeSelf;
                child.SetActive(false);
            }
        }
        showing = true;

        if (currentQuestionNumber < questions.Count && !complete)
        {
            questions[currentQuestionNumber].Show();
        }
        else
            ShowCompletion();

        if (defaultCanvas && !VE2API.PrimaryUIService.IsShowing)
            VE2API.PrimaryUIService.HideUI();
        
        SendState();
    }


    public void Hide()
    {
        if (showing)
        {
            RectTransform rt = GetShowRectTransform();

            for (int i = 0; i < rt.childCount; i++)
            {
                GameObject child = rt.GetChild(i).gameObject;
                int id = child.GetInstanceID();
                if (wasChildDisabled.TryGetValue(id, out bool enabled))
                {
                    child.SetActive(enabled);
                }
            }
            if (currentQuestionNumber < questions.Count)
                questions[currentQuestionNumber].DestroyUI();
            else
            {
                DestroyFinalUI();
            }

            PlaySound(QuizSounds.button);
        }
        showing = false;

        SendState();

    }


    public int GetTotalScore()
    {
        int totalScore = 0;
        foreach (Q_QuizQuestion question in questions)
        {
            if (question.Submitted)
                totalScore += question.Score;
        }
        return totalScore;
    }

    public int GetCurrentQuestionNumber()
    {
        return currentQuestionNumber;
    }

    public void ResetQuiz()
    {

        PopulateQuestions();
        complete = false;
        currentQuestionNumber = 0;

        foreach (Q_QuizQuestion q in questions)
            q.ResetQuestion();

        if (showing) Show();

        SendState();
    }

    public List<Q_QuizQuestion> GetQuizQuestions()
    {
        PopulateQuestions();
        return questions;
    }

    public List<IQuizQuestion> GetQuestions()
    {
        List<IQuizQuestion> qns = new List<IQuizQuestion>();
        foreach (var question in questions)
            qns.Add(question as IQuizQuestion);
        return qns;
    }

    public int GetMaxScore()
    {
        int totalScore = 0;
        foreach (Q_QuizQuestion question in questions)
        {
            if (question.Submitted)
                totalScore += question.GetMaxScore();
        }
        return totalScore;
    }

    public void MoveQuestion(int offset)
    {
        currentQuestionNumber += offset;
        currentQuestionNumber = Mathf.Clamp(currentQuestionNumber, 0, questions.Count);
        PlaySound(QuizSounds.button);
        ShowNewQuestionOrFinish();
        SendState();
    }

    public void RePopulateQuestions()
    {
        bool wasShowing = showing;
        if (showing)
            Hide();

        questions = null;
        PopulateQuestions();

        if (wasShowing)
            Show();

        SendState();
    }

    //------ internal API - no framework interface

    [Serializable]
    private struct UpdateMessage
    {
        public string title;
        public bool showing;
        public string controllerID;
        public int activeQuestion;
        public string answerText;
        public bool revealMode;
        public bool[] toggles;
        public bool[] submitted;
        public int[] scores;
    }

    public void ReceiveRawUpdate(object o)
    {
        if (!VE2API.InstanceService.IsHost)
            ReceiveUpdate((UpdateMessage)o);
    }

    private void DestroyFinalUI()
    {
        if (finalUI != null)
        {
            Destroy(finalUI.gameObject);
            finalUI = null;
        }
    }


    private void ReceiveUpdate(UpdateMessage msg)
    {
        if (msg.title != quizTitle) return;   //this is a message for a different quiz!

        if (msg.submitted.Length != questions.Count || msg.scores.Length != questions.Count)
        {
            Debug.LogError("Malformed quiz synch message");
            return;
        }

        if (msg.showing && !Showing)
        {
            //recieved a show - I'm not showing - so Show
            Show();
        }
        else if (Showing && !msg.showing)
        {
            //received a not show - I'm not showing - so Hide
            Hide();
        }

        if (msg.activeQuestion != currentQuestionNumber)
        {
            if (Showing)
            {
                if (currentQuestionNumber == questions.Count)
                    DestroyFinalUI();
                else
                    questions[currentQuestionNumber].DestroyUI();
            }
            currentQuestionNumber = msg.activeQuestion;
            ShowNewQuestionOrFinish();
        }

        if (currentQuestionNumber < questions.Count)
        {
            if (questions[currentQuestionNumber].IsText())
                (questions[currentQuestionNumber] as Q_QuizQuestionText).ChangeText(msg.answerText);

            if (questions[currentQuestionNumber].IsMC())
                (questions[currentQuestionNumber] as Q_QuizQuestionMultipleChoice).ChangeToggles(msg.toggles);

            if (msg.submitted[currentQuestionNumber] != questions[currentQuestionNumber].Submitted || msg.revealMode != questions[currentQuestionNumber].RevealMode)
                questions[currentQuestionNumber].ChangeSubmit(msg.submitted[currentQuestionNumber], msg.revealMode);
        }

        //update all scores and submits - 
        for (int i = 0; i < questions.Count; i++)
        {
            questions[i].SetScoreAndSubmit(msg.scores[i], msg.submitted[i]);
        }
    }

    public void SendState()
    {
        if (VE2API.InstanceService.IsHost && networkingMode==IQuiz.NetworkMode.group)
        {
            string questionAnswerText="";
            bool[] questionToggles = new bool[0];

            bool revealMode = false;

            //if not on summary screen
            if (currentQuestionNumber<questions.Count)
            {
                questionAnswerText = (questions[currentQuestionNumber] as IQuizQuestion).Text; //works for non-text

                revealMode = questions[currentQuestionNumber].RevealMode;

                if (questions[currentQuestionNumber].IsMC())
                    questionToggles = (questions[currentQuestionNumber] as Q_QuizQuestionMultipleChoice).GetToggles();
            }


            bool[] questionSubmitted = new bool[questions.Count];
            int[] questionScores = new int[questions.Count];

            for (int i=0; i<questions.Count; i++)
            {
                questionSubmitted[i] = questions[i].Submitted;
                questionScores[i] = questions[i].Score;

            }

            UpdateMessage msg = new UpdateMessage()
            {
                title = quizTitle,
                showing = Showing,
                controllerID = this.controllerID,
                activeQuestion = currentQuestionNumber,
                answerText = questionAnswerText,
                toggles = questionToggles,
                submitted = questionSubmitted,
                scores = questionScores,
                revealMode = revealMode
            };

            quizSyncer.UpdateData(msg);
        }
    }

    private RectTransform GetShowRectTransform()
    {
        return rectTransformToShowIn;
    }

    private List<Q_QuizQuestion> questions;

    private void PopulateQuestions()
    {
        if (questions != null) return;
        questions = new List<Q_QuizQuestion>();
        int children = transform.childCount;
        int qnCount = 0;
        for (int i = 0; i < children; i++)
        {
            Transform child = transform.GetChild(i);
            if (child.TryGetComponent<Q_QuizQuestion>(out Q_QuizQuestion question) && child.gameObject.activeInHierarchy)
            {
                questions.Add(question);
                question.SetQuizAndQuestion(this, qnCount++);
            }
        }
    }

    private void ShowNewQuestionOrFinish()
    {
        if (currentQuestionNumber == questions.Count)
        {
            FinishQuiz();
        }
        else
        {
            questions[currentQuestionNumber].ResetRevealMode();
            questions[currentQuestionNumber].Show();
        }
    }

    private void FinishQuiz()
    {
        ShowCompletion();

        try
        {
            OnQuizComplete?.Invoke(GetTotalScore());
            Debug.LogWarning("Quiz complete " + GetTotalScore());
        }
        catch (Exception ex)
        {
            Debug.LogWarning(ex.Message);
        }

    }

    private void ShowCompletion()
    {
        GameObject go =Instantiate(quizCompletePrefab, rectTransformToShowIn);
        finalUI = go.GetComponent<Q_QuizCompletionUI>();
        finalUI.SetUp(this);
    }


    public void SendQuizActionText(Q_QuizQuestion question, string text)
    {
        int questionID = questions.IndexOf(question);
        if (!VE2API.InstanceService.IsHost)
            GetComponent<IV_InstantMessageHandler>().SendInstantMessage(new Tuple<int, string>(questionID, text));
    }

    public void CloseExcept(object notThisOne)
    {
        foreach (var question in questions)
        {
            if (notThisOne is Q_QuizQuestion)
            {
                if (question == (notThisOne as Q_QuizQuestion))
                    continue;
            }
                
            question.DestroyUI();
        }

        if (notThisOne is Q_QuizCompletionUI)
            return;  //don't kill the final UI if this IS final UI

        DestroyFinalUI();
        
    }

    public enum QuizSounds { submit, correct, wrong, button }

    public void PlaySound(QuizSounds sound)
    {
        if (soundEffects)
        {
            switch (sound)
            {
                case QuizSounds.submit:
                    audioSource.PlayOneShot(quizSubmitSound);
                    break;
                case QuizSounds.correct:
                    audioSource.PlayOneShot(quizCorrectSound);
                    break;
                case QuizSounds.wrong:
                    audioSource.PlayOneShot(quizAnswerWrongSound);
                    break;
                case QuizSounds.button:
                    audioSource.PlayOneShot(quizButtonSound);
                    break;
            }
        }
    }

}
