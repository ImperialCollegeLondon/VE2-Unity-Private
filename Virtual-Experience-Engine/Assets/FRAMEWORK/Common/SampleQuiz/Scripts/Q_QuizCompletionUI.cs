using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VE2.Common.API;

/// <summary>
/// UI controller for end-of-quiz screen
/// </summary>
public class Q_QuizCompletionUI : MonoBehaviour
{
    [SerializeField] GameObject textLinePrototypye, backButton, hideButton;
    [SerializeField] TMP_Text completionMessageText, quizTitle, scoreText;
    [SerializeField] Image mainPanelImage, titlePanelImage;
    [SerializeField] RectTransform mainPanel;
    [SerializeField] int maxLinesBeforeTwoColumns = 8;
    
    private Q_Quiz quiz;

    //Construct/configure
    public void SetUp(Q_Quiz quiz)
    {
        this.quiz = quiz;

        //Set quiz in detectors (they need reference to the pointer)
        var detectors = GetComponentsInChildren<Q_MouseEventDetector>();
        foreach (var detector in detectors)
            detector.SetQuiz(quiz);

        //colours
        titlePanelImage.color = quiz.QuestionBackgroundColour;
        mainPanelImage.color = quiz.AnswerBackgroundColour;

        backButton.SetActive(quiz.CanGoBack);
        hideButton.SetActive(quiz.CanHide);

        quizTitle.text = quiz.QuizTitle;
        completionMessageText.text = quiz.CompletionMessage;

        SetupPositionsForItems(quiz);

        if (quiz.RevealTotalScoreAtEndOfQuiz)
        {
            int totalScore = quiz.GetTotalScore();
            int maxScore = quiz.GetMaxScore();
            if (maxScore == 0)
                scoreText.text = $"All questions skipped - no score available";
            else
                scoreText.text=$"Total score: {totalScore}/{maxScore} (={100*totalScore / maxScore}%)";
        }

        FixFonts(quiz.Font);
        textLinePrototypye.SetActive(false);

        if (quiz.RevealScoresAtEndOfQuiz)
            DoScoresList(quiz);

        quiz.CloseExcept(this);
    }

    private void FixFonts(TMP_FontAsset font)
    {
        var allTextObjects = GetComponentsInChildren<TMP_Text>();
        foreach (var textObject in allTextObjects)
        {
            textObject.font = font;
        }

        var allInputFields = GetComponentsInChildren<TMP_InputField>();
        foreach (var textObject in allInputFields)
        {
            textObject.fontAsset = font;
        }

    }

    private void SetupPositionsForItems(Q_Quiz quiz)
    {
        if (quiz.RevealTotalScoreAtEndOfQuiz && !quiz.RevealScoresAtEndOfQuiz)
        {
            completionMessageText.GetComponent<RectTransform>().anchorMin = new Vector2(0f, .6f);
            completionMessageText.GetComponent<RectTransform>().anchorMax = new Vector2(1f, 1f);
            scoreText.GetComponent<RectTransform>().anchorMin = new Vector2(0f, .2f);
            scoreText.GetComponent<RectTransform>().anchorMax = new Vector2(1f, .6f);
        }

        if (quiz.RevealScoresAtEndOfQuiz && !quiz.RevealTotalScoreAtEndOfQuiz)
        {
            scoreText.gameObject.SetActive(false);
            mainPanel.anchorMax = new Vector2(.99f, .86f);
        }

        if (!quiz.RevealScoresAtEndOfQuiz && !quiz.RevealTotalScoreAtEndOfQuiz)
        {
            scoreText.gameObject.SetActive(false);
            completionMessageText.GetComponent<RectTransform>().anchorMin = new Vector2(0f, .2f);
            completionMessageText.GetComponent<RectTransform>().anchorMax = new Vector2(1f, 1f);
        }


    }

    private void DoScoresList(Q_Quiz quiz)
    {
        //work out what we need to  say - then we can work out  how to fit it in
        List<string> messages = new List<string>();

        foreach (Q_QuizQuestion question in quiz.GetQuizQuestions())
        {
            int maxScore = question.GetMaxScore();
            if (maxScore > 0)
            {
                if (question.Submitted)
                {
                    messages.Add($"{question.QuestionText}  <color=yellow>{question.Score}/{question.GetMaxScore()}</color>");
                }
                else
                {
                    messages.Add($"{question.QuestionText}  <color=yellow>[skipped]</color>");
                }
            }
            //else
            //{
            //    messages.Add($"{question.QuestionText}  <color=yellow>[non-scoring]</color>");
            //}
        }

        if (messages.Count > maxLinesBeforeTwoColumns) //two columns
        {

            int linesInFirstColumn = (messages.Count + 1) / 2;
            float sizeEachMessage = 1f / (float)linesInFirstColumn;

            for (int i = 0; i < messages.Count; i++)
            {
                GameObject message = Instantiate(textLinePrototypye, mainPanel);
                message.SetActive(true);
                RectTransform rt = message.GetComponent<RectTransform>();

                int iInColumn = i;
                float minX = 0f;
                float maxX = .49f;

                if (i >= linesInFirstColumn)
                {
                    iInColumn = i - linesInFirstColumn;
                    minX = .51f;
                    maxX = 1f;
                }

                rt.anchorMin = new Vector2(minX, 1f - (sizeEachMessage * (float)(iInColumn + 1)));
                rt.anchorMax = new Vector2(maxX, 1f - (sizeEachMessage * (float)iInColumn));
                message.GetComponent<TMP_Text>().text = messages[i];
            }
        }
        else //single column
        {
            float sizeEachMessage = 1f / (float)messages.Count;

            for (int i = 0; i < messages.Count; i++)
            {
                GameObject message = Instantiate(textLinePrototypye, mainPanel);
                message.SetActive(true);
                RectTransform rt = message.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0f, 1f - (sizeEachMessage * (float)(i + 1)));
                rt.anchorMax = new Vector2(1f, 1f - (sizeEachMessage * (float)(i)));
                message.GetComponent<TMP_Text>().text = messages[i];
            }
        }
    }

    //button handlers
    public void Close()
    {
        if (VE2API.InstanceService.IsHost || quiz.NetworkingMode == IQuiz.NetworkMode.local)
        {
            quiz.Hide();
        }
    }

    public void Back()
    {
        if (VE2API.InstanceService.IsHost || quiz.NetworkingMode == IQuiz.NetworkMode.local)
        {
            quiz.MoveQuestion(-1);
        }
    }


}
