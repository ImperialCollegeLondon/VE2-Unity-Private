

using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;


/// <summary>
/// Derived multiple choice quesiton class - should be placed on a gameobject child of a V_Quiz
/// </summary>
public class Q_QuizQuestionMultipleChoice : Q_QuizQuestion
{
    //[Tooltip("To access the question from plugin code, use GetComponent<IQuizQuestion>() on this GameObject.\n\nExample: if your script is on this gameobject, get score for question using:\nint score = gameObject.GetComponent<IQuizQuestion>().GetScore();")]
    [Tooltip("Should the user be able to select more than one answer?")]
    [SerializeField] bool multipleAnswersAllowed;
    [Tooltip("For multiple answer mode, score only if best possible combination of answers is given?")]
    [SerializeField] bool allOrNothingMultipleAnswerScoring;
    [Tooltip("For all or nothing mode, use this score if best possible combination is given (otherwise score 0)")]
    [SerializeField] int allOrNothingCorrectAnswerScore = 1;

    //Property
    public override bool MultipleAnswersAllowed { get => multipleAnswersAllowed; }
    public override  bool NoAnswers { get => noAnswers;  }
    public override string Text { get => ""; set => throw new NotImplementedException(); }
    public override int MaximumCharacters { get => 0; set => throw new NotImplementedException(); }
    public override bool ProvideResponse { get => false; set => throw new NotImplementedException(); }

    public override bool MultiLine { get => false; }

    public override bool Valid { get => QuestionUI.GetComponent<Q_QuizQuestionUI>().CanSubmit(); }

    public override string Response { get => ""; }


    public override string CorrectTextForAnswerReveal { get => ""; set => throw new NotImplementedException(); }
    public override bool AllOrNothingMultipleAnswerScoring { get => allOrNothingMultipleAnswerScoring; set => allOrNothingMultipleAnswerScoring = value; }
    public override int AllOrNothingCorrectAnswerScore { get => allOrNothingCorrectAnswerScore; set => allOrNothingCorrectAnswerScore = value; }
    public override IQuizQuestion.TextScorer textScorer { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public override IQuizQuestion.TextValidator textValidator { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public override IQuizQuestion.TextResponder textResponder { get => throw new NotImplementedException(); set => throw new NotImplementedException();  }

    private List<Q_QuizMCItem> items;
    private bool noAnswers=false;

    /// <summary>
    /// Create the UI
    /// </summary>
    public override void Show()
    {
        if (QuestionUI == null)
        {
            QuestionUI = Instantiate(quiz.QuizQuestionPrefab, Quiz.RectTransformHolder);
            QuestionUI.GetComponent<Q_QuizQuestionUI>().SetUpMultipleChoice(this);
        }
        base.Show();
    }

    /// <summary>
    /// Implement ComputeScore - sum of scores of all answers
    /// </summary>
    /// <returns></returns>
    protected override int ComputeScore()
    {
        score = 0;
        foreach (Q_QuizMCItem item in items)
        {
            if (item.GetType() == typeof(Q_QuizAnswer))
            {
                score += (item as Q_QuizAnswer).GetScore();
            }
        }

        if (multipleAnswersAllowed && allOrNothingMultipleAnswerScoring) 
        {
            if (score == NormalMaxScoreForMulti())
                score = allOrNothingCorrectAnswerScore;
            else
                score = 0;
        }
         
        return score;
        
    }

    /// <summary>
    /// Implement validate - always OK for multiple, require something ticked for normal
    /// </summary>
    /// <returns></returns>
    public override bool Validate()
    {
        if (multipleAnswersAllowed)
            return true;
        else
        {
            bool chosen = false;
            foreach (Q_QuizMCItem item in items)
            {
                if (item.GetType() == typeof(Q_QuizAnswer))
                {
                    if ((item as Q_QuizAnswer).Ticked) chosen = true;
                }
            }
            return chosen;
        }
    }

    /// <summary>
    /// Calculate maximum possible score for question
    /// </summary>
    /// <returns></returns>
    public override int GetMaxScore()
    {
        int maxScore = 0;
        if (multipleAnswersAllowed)
        {
            if (allOrNothingMultipleAnswerScoring)
            {
                return AllOrNothingCorrectAnswerScore;
            }
            else
            {
                maxScore = NormalMaxScoreForMulti();
            }
        }
        else
        {
            foreach (Q_QuizMCItem item in items)
            {
                if (item.GetType() == typeof(Q_QuizAnswer))
                {
                    int yes = (item as Q_QuizAnswer).PointsForTicked;
                    if (yes > maxScore)
                        maxScore = yes;
                }
            }
        }

        return maxScore;
    }

    /// <summary>
    /// Max score for multi-question - without the 'all or nothing' rule
    /// </summary>
    /// <returns></returns>
    private int NormalMaxScoreForMulti()
    {
        int maxScore = 0;
        foreach (Q_QuizMCItem item in items)
        {
            if (item.GetType() == typeof(Q_QuizAnswer))
            {
                int no = (item as Q_QuizAnswer).PointsForUnticked;
                int yes = (item as Q_QuizAnswer).PointsForTicked;
                if (no > yes)
                    maxScore += no;
                else
                    maxScore += yes;
            }
        }

        return maxScore;
    }

    public List<Q_QuizMCItem> GetQuizItems()
    {
        return items;
    }

    //Look at children, find all images and answers
    protected override void Populate()
    { 
        items = new List<Q_QuizMCItem>();
        int children = transform.childCount;
        int nextIndex = 0;
        for (int i = 0; i < children; i++)
        {
            Transform child = transform.GetChild(i);
            if (child.TryGetComponent<Q_QuizMCItem>(out Q_QuizMCItem item) &&  child.gameObject.activeInHierarchy)
            {
                items.Add(item);

                if (item.GetType()==typeof(Q_QuizAnswer))
                {
                    (item as Q_QuizAnswer).Index = nextIndex++;
                }
            }
        }
        noAnswers = (nextIndex == 0); //if no answers - no submit button, just skip
    }

    /// <summary>
    /// Used when toggles are clicked on in non-multi mode
    /// </summary>
    /// <param name="answer"></param>
    public void UnSetAllExcept(Q_QuizAnswer answer)
    {
        foreach (Q_QuizMCItem item in items)
        {
            if (item.GetType()==typeof(Q_QuizAnswer))
            {
                if (item!=answer)
                {
                    (item as Q_QuizAnswer).UnSet();
                }
            }
        }
    }

    /// <summary>
    /// Find highest score possible for any answer - for mulitple this is NOT
    /// the same thing as maximum score for question
    /// </summary>
    /// <returns></returns>
    public int MaxAnswerScore()
    {
        int maxScore = 0;
        if (multipleAnswersAllowed)
        {
            if (allOrNothingMultipleAnswerScoring)
                return allOrNothingCorrectAnswerScore;
            else
            {
                foreach (Q_QuizMCItem item in items)
                {
                    if (item.GetType() == typeof(Q_QuizAnswer))
                    {
                        int no = (item as Q_QuizAnswer).PointsForUnticked;
                        int yes = (item as Q_QuizAnswer).PointsForTicked;
                        if (no > yes)
                            maxScore = Mathf.Max(maxScore, no);
                        else
                            maxScore = Mathf.Max(maxScore, yes);
                    }
                }
            }
        }
        else
        {
            foreach (Q_QuizMCItem item in items)
            {
                if (item.GetType() == typeof(Q_QuizAnswer))
                {
                    int yes = (item as Q_QuizAnswer).PointsForTicked;
                    if (yes > maxScore)
                        maxScore = yes;
                }
            }
        }

        return maxScore;
    }
    

    // Emit delay counter system - for pulsed particle systems in UI
    private int emitDelay = 0;
    public void ResetEmitDelay()
    {
        emitDelay = 0;
    }

    public int GetEmitDelay()
    {
        return emitDelay++;
    }

    protected override void ResetQuestionDerived()
    {
        if (items == null) return;
        foreach (var item in items)
        {
            if (item.GetType() == typeof(Q_QuizAnswer))
            {
                (item as Q_QuizAnswer).Reset();
            }
        }
    }

    public override bool IsText()
    {
        return false;
    }

    public override bool IsMC()
    {
        return true;
    }

    public override List<IQuizAnswer> GetAnswers()
    {
        List<IQuizAnswer> answers = new List<IQuizAnswer>();
        foreach (var item in items)
        {
            if (item.GetType() == typeof(Q_QuizAnswer))
                answers.Add(item as IQuizAnswer);
        }
        return answers;
    }

    public override List<IQuizHorizontalImage> GetHorizontalImages()
    {
        List<IQuizHorizontalImage> images = new List<IQuizHorizontalImage>();
        foreach (var item in items)
        {
            if (item.GetType() == typeof(Q_QuizHorizontalImage))
                images.Add(item as IQuizHorizontalImage);
        }
        return images;
    }

    protected override string GetTypeString()
    {
        if (multipleAnswersAllowed)
            return "Multiple Choice (multiple answers)";
        else
            return "Multiple Choice";

    }

    protected override string GetAnswerText()
    {
        if (noAnswers)
            return "";
        else
        {
            var answers = GetAnswers();
            List<string> answerTexts = new List<string>();
            foreach (var answer in answers)
            {
                if (answer.Ticked)
                    answerTexts.Add(answer.AnswerText);
            }
            return string.Join("|", answerTexts);
        }
    }

    public void ChangeToggles(bool[] toggles)
    {
        List<Q_QuizAnswerUI> answers = new List<Q_QuizAnswerUI>();
        foreach (var item in items)
        {
            if (item.GetType() == typeof(Q_QuizAnswer))
            {
                var answerUI = (item as Q_QuizAnswer).answerUI;
                answers.Add(answerUI);
            }
        }

        if (toggles.Length != answers.Count)
            Debug.LogError("Wrong toggle data in quiz sync");
        else
        {
            for (int i=0; i<toggles.Length; i++)
                answers[i].SetToggleValue(toggles[i]);
        }
    }

    public bool[] GetToggles()
    {
        var answers = GetAnswers();
        bool[] toggles = new bool[answers.Count];
        for (int i = 0; i < answers.Count; i++)
            toggles[i] = answers[i].Ticked;

        return toggles;

    }

    internal void SetToggle(int toggleID, bool toggleState)
    {
        List<Q_QuizAnswerUI> answers = new List<Q_QuizAnswerUI>();
        foreach (var item in items)
        {
            if (item.GetType() == typeof(Q_QuizAnswer))
            {
                var answerUI = (item as Q_QuizAnswer).answerUI;
                answers.Add(answerUI);
            }
        }

        answers[toggleID].ToggleSetRemotely(toggleState);
    }
}
