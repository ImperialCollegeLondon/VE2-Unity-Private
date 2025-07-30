using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using VE2.Common.API;

/// <summary>
/// Main controller for question screen in quiz UI
/// </summary>
public class Q_QuizQuestionUI : MonoBehaviour
{
    [SerializeField] GameObject textSingleLine, textMultiLine, responseLine, MCAnswerPrototype, 
        ImagePrototype, skipButton, backButton, hideButton, multiOption;

    [SerializeField] TMP_Text positionText;
    [SerializeField] RectTransform verticalImageRT, answerPanel;

    [SerializeField] Image questionPanelImage, answersPanelImage;
    [SerializeField] RawImage verticalImage;
    [SerializeField] TMP_Text responseLineText, questionText, quizTitle;
    [SerializeField] Button submitButton;
    [SerializeField] TMP_Text scoreText;



    // local data

    private TMP_InputField textInputField;
    private Q_QuizQuestion question;
    private bool textMode = false;
    private string oldText = "";
    private List<Q_QuizMCItem> itemList;


    //Setup methods - text or MC mode

    public void SetUpText(Q_QuizQuestion linkedQuestion)
    {
        question = linkedQuestion;
        oldText = (question as Q_QuizQuestionText).Text; //gets overriden by some event cascade
        if ((question as Q_QuizQuestionText).MultiLine)
        {
            textMultiLine.SetActive(true);
            textInputField = textMultiLine.GetComponent<TMP_InputField>();
        }
        else
        {
            textSingleLine.SetActive(true);
            textInputField = textSingleLine.GetComponent<TMP_InputField>();
        }
        textInputField.characterLimit = (question as Q_QuizQuestionText).MaximumCharacters;
        responseLineText.SetText("");
        responseLine.SetActive(true);
        textMode = true;

        textInputField.text = oldText;

        SetUpGeneric();
    }

    public void SetUpMultipleChoice(Q_QuizQuestion linkedQuestion)
    {
        question = linkedQuestion;
        itemList = (question as Q_QuizQuestionMultipleChoice).GetQuizItems();

        float totalWeight = 0f;
        foreach (Q_QuizMCItem item in itemList)
        {
            if (item.GetType() == typeof(Q_QuizHorizontalImage))
                totalWeight += (item as Q_QuizHorizontalImage).LayoutWeight;
            else
                totalWeight += 1f;
        }

        if (totalWeight == 0)
        {
            Debug.LogError("Zero weight in item layout");
            return;
        }

        float multiLineMod = 0f;
        if ((question as Q_QuizQuestionMultipleChoice).MultipleAnswersAllowed) multiLineMod = .07f;
        //we have 83% to play with normally -7% if multiline
        float unitsPerWeight = (.83f - multiLineMod) / totalWeight;

        float top = .99f;
        bool firstQn = true;
        foreach (Q_QuizMCItem item in itemList)
        {
            GameObject go = null;
            if (item.GetType() == typeof(Q_QuizHorizontalImage))
            {
                go = Instantiate(ImagePrototype, answerPanel);
                go.GetComponent<Q_QuizHorizontalImageUI>().SetUp(item as Q_QuizHorizontalImage);
            }
            else if (item.GetType() == typeof(Q_QuizAnswer))
            {
                //do warning about multiline
                if (firstQn && (question as Q_QuizQuestionMultipleChoice).MultipleAnswersAllowed)
                {
                    multiOption.SetActive(true);
                    multiOption.GetComponent<RectTransform>().anchorMin = new Vector2(0f, top - .07f);
                    multiOption.GetComponent<RectTransform>().anchorMax = new Vector2(1f, top);
                    top -= .07f;
                    firstQn = false;
                }
                go = Instantiate(MCAnswerPrototype, answerPanel);
                go.GetComponent<Q_QuizAnswerUI>().SetUp(item as Q_QuizAnswer, question as Q_QuizQuestionMultipleChoice);
            }

            go.SetActive(true);
            RectTransform rt = go.GetComponent<RectTransform>();

            float layoutWeight = 1f;
            if (item.GetType() == typeof(Q_QuizHorizontalImage))
                layoutWeight = (item as Q_QuizHorizontalImage).LayoutWeight;
            float bottom = top - layoutWeight * unitsPerWeight;
            rt.anchorMin = new Vector2(0f, bottom);
            rt.anchorMax = new Vector2(1f, top);
            top = bottom;
        }


        SetUpGeneric();

        //This is 'slideshow mode' - there are no actual answers (maybe just an image?) - so skip button only
        if ((question as Q_QuizQuestionMultipleChoice).NoAnswers)
        {
            skipButton.GetComponent<Button>().interactable=true;
            submitButton.gameObject.SetActive(false);
            Debug.Log($"SetUpMultipleChoice set submit false");
        }
    }


    /// <summary>
    /// Do the setup stuff that's the same for text and multiple choice
    /// </summary>
    private void SetUpGeneric()
    {
        //Set quiz in detectors (they need reference to the pointer)
        var detectors = GetComponentsInChildren<Q_MouseEventDetector>();
        foreach (var detector in detectors)
            detector.SetQuiz(question.Quiz);


        //colours
        questionPanelImage.color = question.Quiz.QuestionBackgroundColour;
        answersPanelImage.color = question.Quiz.AnswerBackgroundColour;
        questionText.text = FormattedQuestionNumber(question.Quiz.QuestionNumbering, question.QuestionNumber) + question.QuestionText;

        SetupVerticalImage();

        quizTitle.text = question.Quiz.QuizTitle;

        if (question.Quiz.ShowQuestionPosition)
        {
            positionText.text = $"{question.QuestionNumber + 1}/{question.Quiz.GetQuizQuestions().Count}";
        }
        else
        {
            positionText.text = "";
        }
        Validate();

        DoButtonVisibility();


        FixFonts(question.Quiz.Font);


        //Finally - this (and all children) needs to be on same layer as parent - probably. Do it anyway
        SetLayerRecursively(gameObject, gameObject.transform.parent.gameObject.layer);


    }

    private void FixFonts(TMP_FontAsset font)
    {
        bool scVis = scoreText.gameObject.activeSelf;
        scoreText.gameObject.SetActive(true);

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
        scoreText.gameObject.SetActive(scVis);
    }

    public void DoButtonVisibility()
    {
        hideButton.SetActive(question.Quiz.CanHide);
        skipButton.GetComponent<Button>().interactable = question.CanSkip || question.RevealMode;
        backButton.GetComponent<Button>().interactable = question.Quiz.CanGoBack && question.QuestionNumber > 0;
        submitButton.gameObject.SetActive(!question.RevealMode);
        Debug.Log($"DoButtonVisibility set submit {!question.RevealMode}");
        submitButton.interactable = question.Validate();
    }


    //Getter
    public string GetTextInTextField()
    {
        return textInputField.text;
    }


    /// <summary>
    /// As it says. Not sure it's vital really.
    /// </summary>
    /// <param name="gameObject"></param>
    /// <param name="layer"></param>
    private void SetLayerRecursively(GameObject gameObject, int layer)
    {
        gameObject.layer = layer;
        for (int i=0; i< gameObject.transform.childCount; i++)
            SetLayerRecursively(gameObject.transform.GetChild(i).gameObject, layer);
    }

    private void SetupVerticalImage()
    {
        switch (question.VerticalImagePosition)
        {
            case IQuizQuestion.ImagePosition.none:
                verticalImageRT.gameObject.SetActive(false);
                answerPanel.anchorMin = new Vector2(0f, 0f);
                answerPanel.anchorMax = new Vector2(1f, 1f);
                break;
            case IQuizQuestion.ImagePosition.left:
                verticalImageRT.anchorMin = new Vector2(0.00f, verticalImageRT.anchorMin.y);
                verticalImageRT.anchorMax = new Vector2(question.VerticalImageWidthPercentage / 100f, verticalImageRT.anchorMax.y);
                answerPanel.anchorMin = new Vector2(question.VerticalImageWidthPercentage / 100f, verticalImageRT.anchorMin.y);
                answerPanel.anchorMax = new Vector2(1f, verticalImageRT.anchorMax.y);
                verticalImageRT.gameObject.SetActive(true);
                verticalImage.texture = question.VerticalImage;
                verticalImage.GetComponent<AspectRatioFitter>().aspectRatio = 
                    (float)question.VerticalImage.width/ (float)question.VerticalImage.height;
                break;
            case IQuizQuestion.ImagePosition.right:
                verticalImageRT.anchorMin = new Vector2(1f - question.VerticalImageWidthPercentage / 100f, verticalImageRT.anchorMin.y);
                verticalImageRT.anchorMax = new Vector2(1f, verticalImageRT.anchorMax.y);
                answerPanel.anchorMin = new Vector2(0, verticalImageRT.anchorMin.y);
                answerPanel.anchorMax = new Vector2(1f - question.VerticalImageWidthPercentage / 100f, verticalImageRT.anchorMax.y);
                verticalImageRT.gameObject.SetActive(true);
                verticalImage.texture = question.VerticalImage;
                verticalImage.GetComponent<AspectRatioFitter>().aspectRatio =
                    (float)question.VerticalImage.width / (float)question.VerticalImage.height;
                break;
            default:
                Debug.LogError($"Unhandled case {question.VerticalImagePosition} in QuizQuestionUI");
                break;
        }
    }


    public void Validate()
    {
        submitButton.interactable = question.Validate();
        if (textMode)
        {
            (question as Q_QuizQuestionText).Text = textInputField.text;
            if ((question as Q_QuizQuestionText).ProvideResponse)
            {
                responseLineText.text = (question as Q_QuizQuestionText).Respond();
            }
            question.Quiz.SendState();
        }
    }

    // Handle animations etc during score/answer reveal
    private void DoScoreReveal()
    {
        scoreText.text = $"Score: {question.Score}/{question.GetMaxScore()}";
        scoreText.gameObject.SetActive(true);
        scoreText.transform.localScale = new Vector3(3, 3, 3);
        scoreText.GetComponent<RectTransform>().DOScale(new Vector3(1f, 1f, 1f), 1f).SetEase(Ease.OutBack);
    }

    private void SetupPostSubmitStuff()
    {
        submitButton.gameObject.SetActive(false);
        Debug.Log($"SetupPostSubmitStuff set submit false");
        skipButton.GetComponent<Button>().interactable = true;
    }

    private void DoAnswerReveal()
    {
        Debug.Log("In Do Answer Reveal");
        int maxScore = question.GetMaxScore();

        if (textMode)
        {
            if (question.Score <= maxScore / 2)
                question.Quiz.PlaySound(Q_Quiz.QuizSounds.wrong);
            else
                question.Quiz.PlaySound(Q_Quiz.QuizSounds.correct);

            responseLineText.text = (question as Q_QuizQuestionText).CorrectTextForAnswerReveal;
        }
        else
        {
            (question as Q_QuizQuestionMultipleChoice).ResetEmitDelay();
            itemList = (question as Q_QuizQuestionMultipleChoice).GetQuizItems();
            foreach (var item in itemList)
            {
                if (item.GetType() == typeof(Q_QuizAnswer))
                {
                    (item as Q_QuizAnswer).answerUI.ShowCorrect();
                }
            }

            if (question.Score <= maxScore / 2)
                question.Quiz.PlaySound(Q_Quiz.QuizSounds.wrong);
            else
                question.Quiz.PlaySound(Q_Quiz.QuizSounds.correct);
        }
    }


    //button handlers - called from unity events
    public void Submit()
    {
        if (ApplyDirect())
        {
            question.Submit();
        }
    }

    public void DoPostSubmissionStuff()
    {
        if (question.Quiz.RevealScoresAfterEachQuestion || question.Quiz.RevealAnswersAfterEachQuestion)
            SetupPostSubmitStuff();

        if (question.Quiz.RevealAnswersAfterEachQuestion)
            DoAnswerReveal();
        if (question.Quiz.RevealScoresAfterEachQuestion)
            DoScoreReveal();
    }

    public void Skip()
    {
        if (ApplyDirect())
            question.Skip();
    }



    public void Back()
    {
        if (ApplyDirect())
            question.Back();
    }

    public void Hide()
    {
        if (ApplyDirect())
            question.Quiz.Hide();
    }

    //Can I apply changes directly? E.g. is it a local quiz - or am I a host controller?
    private bool ApplyDirect()
    {
        if (VE2API.InstanceService.IsHost || question.Quiz.NetworkingMode == IQuiz.NetworkMode.local)
            return true;
        else
            return false;
    }



    //Static number formatters

    public static string FormattedQuestionNumber(IQuiz.NumberMode questionNumbering, int questionNumber)
    {
        switch (questionNumbering)
        {
            case IQuiz.NumberMode.none:
                return "";
            case IQuiz.NumberMode.digits:
                return $"{questionNumber + 1}. ";
            case IQuiz.NumberMode.romanNumerals:
                return NumberToRoman(questionNumber + 1) + ". ";
            case IQuiz.NumberMode.letters:
                return NumberToLetter(questionNumber) + ". ";
            default:
                return $"{questionNumber + 1}. ";
        }
    }

    public static string NumberToLetter(int questionNumber)
    {
        if (questionNumber <= 26)
            return Char.ToString((char)(questionNumber + 65));
        else
            return $"{questionNumber + 1}. ";
    }


    //Static roman numeral code - from https://stackoverflow.com/questions/22392810/integer-to-roman-format
    private static List<string> romanNumerals = new List<string>() { "m", "cm", "d", "cd", "c", "xc", "l", "xl", "x", "ix", "v", "iv", "i" };
    private static List<int> numerals = new List<int>() { 1000, 900, 500, 400, 100, 90, 50, 40, 10, 9, 5, 4, 1 };

    public static string NumberToRoman(int number)
    {
        var romanNumeral = string.Empty;
        while (number > 0)
        {
            // find biggest numeral that is less than equal to number
            var index = numerals.FindIndex(x => x <= number);
            // subtract it's value from your number
            number -= numerals[index];
            // tack it onto the end of your roman numeral
            romanNumeral += romanNumerals[index];
        }
        return romanNumeral;
    }

    public bool CanSubmit()
    {
        return submitButton.gameObject.activeSelf;
    }

    public void SetTextRemotely(string text)
    {
        textInputField.text = text;
    }

    public void EnableAnswers(bool enable)
    {
        if (question.IsMC())
        {
            foreach (var item in itemList)
            {
                if (item.GetType() == typeof(Q_QuizAnswer))
                {
                    (item as Q_QuizAnswer).answerUI?.EnableUI(enable);
                }
            }
        }
        
        if (question.IsText()) 
        {
            textSingleLine.GetComponent<TMP_InputField>().interactable = enable;
            textMultiLine.GetComponent<TMP_InputField>().interactable = enable;
        }
    }

    public void TextChanged(string text)
    {
        question.Quiz.SendQuizActionText(question, text);
        Validate();
    }
}
