using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// UI class for a multiple choice answer
/// </summary>
public class Q_QuizAnswerUI : MonoBehaviour
{
    [SerializeField] private RectTransform imageRT, textRT, checkboxRT, tickRT, crossRT;
    [SerializeField] private TMP_Text questionText;
    [SerializeField] private Toggle checkBox;
    private Q_ToggleButtonHighlighter toggleHighlighter;
    [SerializeField] private RawImage image;
    [SerializeField] private CanvasGroup scoreResults;
    [SerializeField] private Q_FakeParticleController particleController;

    //local data
    private Q_QuizQuestionMultipleChoice question;
    private Q_QuizAnswer answer;
    private bool uiEnabled = true;
    private bool showCorrectMode = false;

    public void EnableUI(bool enable)
    {
        uiEnabled = enable;
        if (uiEnabled)
        {
            //on - so enable unless we are in showCorrectMode
            checkBox.interactable = !showCorrectMode;
        }
        else
        {
            checkBox.interactable = false;
        }
    }

    //construct the interface
    public void SetUp(Q_QuizAnswer answer, Q_QuizQuestionMultipleChoice question)
    {
        toggleHighlighter = checkBox.gameObject.GetComponent<Q_ToggleButtonHighlighter>();
        this.answer = answer;
        answer.answerUI= this;
        this.question = question;
        questionText.text = Q_QuizQuestionUI.FormattedQuestionNumber(question.Quiz.AnswerNumbering, answer.Index) + answer.AnswerText;
        if (answer.Image != null)
        {
            image.texture = answer.Image;
            image.GetComponent<AspectRatioFitter>().aspectRatio = (float)answer.Image.width / (float)answer.Image.height;
        }
        checkBox.isOn = answer.Ticked;
        toggleHighlighter.SetSelected(answer.Ticked);
        switch (answer.ImagePosition)
        {
            case IQuizAnswer.ImagePositions.none:
                imageRT.gameObject.SetActive(false);
                textRT.anchorMin = new Vector2(0f, 0f);
                textRT.anchorMax = new Vector2(.78f, 1f);
                break;
            case IQuizAnswer.ImagePositions.leftOfAnswer:
                //already set this way - nothing much to do
                imageRT.anchorMax = new Vector2(answer.ImageWidthPercentage / 100f, 1f);
                textRT.anchorMin = new Vector2(answer.ImageWidthPercentage / 100f + .02f, 0f);
                break;
            case IQuizAnswer.ImagePositions.betweenAnswerAndCheckBox:
                textRT.anchorMin = new Vector2(0f, 0f);
                textRT.anchorMax = new Vector2(.78f - answer.ImageWidthPercentage / 100f, 1f);
                imageRT.anchorMin = new Vector2(.78f - answer.ImageWidthPercentage / 100f, 0f);
                imageRT.anchorMax = new Vector2(.78f, 1f);
                break;
            case IQuizAnswer.ImagePositions.rightOfCheckBox:
                textRT.anchorMin = new Vector2(0f, 0f);
                textRT.anchorMax = new Vector2(.78f - answer.ImageWidthPercentage / 100f, 1f);
                imageRT.anchorMin = new Vector2(1f - answer.ImageWidthPercentage / 100f, 0f);
                imageRT.anchorMax = new Vector2(1f, 1f);
                checkboxRT.anchorMin = new Vector2(.78f - answer.ImageWidthPercentage / 100f, 0f);
                checkboxRT.anchorMax = new Vector2(1f - answer.ImageWidthPercentage / 100f, 1f);
                break;
        }
    }

    /// <summary>
    /// Handle UI for showing correct/incorrect answers on reveal
    /// </summary>
    public void ShowCorrect()
    {
        showCorrectMode = true;
        checkBox.interactable = false;
        int score = answer.PointsForTicked;
        if (answer.PointsForTicked > 0)
            score = Mathf.Max(score, -answer.PointsForUnticked);
        if (answer.PointsForTicked < 0)
            score = Mathf.Min(score, -answer.PointsForUnticked);

        if (score == 0)
        {
            if (question.MultipleAnswersAllowed)
                SetCheckBoxColourGreenToRed(0f);  //0 is neutral in a multi-answer qn - whether ticked or unticked
            else
            {
                if (answer.Ticked) 
                    SetCheckBoxColourGreenToRed(-1f);  //0 is wrong in a  single answer qn
            }
            return;
        }
        int maxScore = question.MaxAnswerScore();
        
        //size is 0.6f for max. Ratio according to positive/negative score, but min is 0.2f
        float scale = Mathf.Clamp(0.6f * (Mathf.Abs((float)score) / (float)maxScore),0.2f, 0.6f);

        if (score>0)
        {
            
            tickRT.gameObject.SetActive(true);
            tickRT.localScale = new Vector3(scale, scale, scale);
            if (answer.Ticked)
            {
                Emit(scale, question.GetEmitDelay());
                SetCheckBoxColourGreenToRed(scale / .6f);
                if (!question.MultipleAnswersAllowed)
                    return; //skip the tick generation - single answer, particles are enough
            }
            if (question.MultipleAnswersAllowed)
            {
                if (!answer.Ticked)
                {
                    SetCheckBoxColourGreenToRed(-scale / .6f);
                }
            }
        }
        else
        {
            crossRT.gameObject.SetActive(true);
            crossRT.localScale = new Vector3(scale, scale, scale);
            if (question.MultipleAnswersAllowed)
            {
                if (!answer.Ticked)
                {
                    Emit(scale, question.GetEmitDelay());
                    SetCheckBoxColourGreenToRed(scale/.6f);
                }
                else
                {
                    SetCheckBoxColourGreenToRed(-scale / .6f);
                }
            }
            else
            {
                if (answer.Ticked) 
                {
                    SetCheckBoxColourGreenToRed(-scale/.6f); //it was wrong
                }
            }
        }

        scoreResults.alpha = 0f;
        DOVirtual.Float(0f,1f,.25f, (float v) => scoreResults.alpha=v);
        //scoreResults.DOFade(1f, .25f);
    }

    //Fake particles to the rescue!
    private void Emit(float scale, int delay)
    {
        //Debug.Log($"Delay is {delay}");
        if (delay == 0)
            particleController.Burst();
        else
            DOVirtual.DelayedCall(1f * delay, () => particleController.Burst());
    }

    /// <summary>
    /// -1 = red, 1 = green
    /// </summary>
    /// <param name="v"></param>
    private void SetCheckBoxColourGreenToRed(float v)
    {
        v = Mathf.Clamp(v, -1f, 1f);
        ColorBlock cb = checkBox.colors;
        cb.disabledColor = Color.Lerp(Color.red, Color.green, (v + 1f) / 2f);
        checkBox.colors = cb;
    }

    //Called by Unity Event when toggle is toggled
    public void ToggleSet()
    {
        bool on = !answer.Ticked;
        //Debug.Log("Setting on " + on + " - " + gameObject.name);

        answer.Ticked= on;
        toggleHighlighter.SetSelected(on);
        if (!question.MultipleAnswersAllowed && on) 
        {
            question.UnSetAllExcept(answer);            
        }
        question.UIValidate();
        question.Quiz.SendState();
        //if (question.Quiz.NetworkingMode == IQuiz.NetworkMode.group) question.Quiz.SendQuizActionToggle(GetMyIndex(), on);
    }

    private int GetMyIndex()
    {
        var answers = question.GetAnswers();

        int i = 0;
        foreach (var answer in answers)
        {
            if (answer == this.answer as IQuizAnswer)
                return i;
            i++;
        }
        Debug.LogError("Can't find answer index");
        return -1; //error!
    }

    public void ToggleSetRemotely(bool on)
    {
        answer.Ticked = on;
        SetToggleValue(on);
        if (!question.MultipleAnswersAllowed && on)
        {
            question.UnSetAllExcept(answer);
        }
        question.UIValidate();
        question.Quiz.SendState();
    }




    //Part of the 'only one at once' system
    public void UnSet()
    {
        checkBox.isOn = false;
       
        toggleHighlighter.SetSelected(false);
    }

    public void SetToggleValue(bool v)
    {
        if (checkBox.isOn == v) return;
        checkBox.isOn = v;
        if (toggleHighlighter.IsSelected() == v) 
            return;

        toggleHighlighter.SetSelected(v);
    }
}
