using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using VE2;
using VE2.Common.API;
using VE2.Core.Player.API;


namespace VE2.Core.VComponents.Internal
{
    [AddComponentMenu("")] //Unlikely to be useful outside the infopoint context, so hide it from the menu
    public class VirtualKeyboard : MonoBehaviour
    {

        public GameObject fullKeyBoardGameObject;
        public GameObject numPadGameObject;
        public GameObject visualMeshObject;
        public GameObject separator;
        public GameObject body;

        private CanvasGroup resultPromptGroup;
        private Transform resultPromptTransform;

        public CanvasGroup resultPromptGroupNumPad;
        public Transform resultPromptTransformNumPad;

        public CanvasGroup resultPromptGroupFullKeyboard;
        public Transform resultPromptTransformFullKeyboard;
        public Transform parentObject;

        public TMP_Text inputPromptText;
        public TMP_Text outputTextLongUI;
        public TMP_Text outputTextshortUI;

        public Button[] numPadActionButtons;
        public Button[] fullKeyBoardActionButtons;
        public Button[] fullKeyBoardLowerCaseActionButtons;
        public Button[] fullKeyBoardNumbersActionButtons;
        public Button[] fullKeyBoardAltCharActionButtons;

        private bool isLookAtActive = true;

        private Button[] currentFullKeyboardActionButtons;
        private TMP_Text outputTextUI;

        private string defaultPromtText;
        private string keyboardOutput = "";
        private KeyboardConfig keyboardConfig;
        private bool isUserEntryCorrect = false;
        private bool isKeyBoardUpperCase = true;
        private Vector3 visualMeshObjectScale;
        private CanvasGroup sepCG, inputCG, bodyCG, parentCG;

        public UnityEvent<string> OnSubmitted;
        public UnityEvent<string> OnTextUpdate;

        //public V_GrabbableAdjustable grabbableAdjustable;

        private void OnEnable()
        {
            Keyboard.current.onTextInput += OnTextInput;
        }

        private void Awake()
        {
            sepCG = separator.GetComponent<CanvasGroup>();
            inputCG = inputPromptText.GetComponent<CanvasGroup>();
            bodyCG = body.GetComponent<CanvasGroup>();
            parentCG = parentObject.GetComponent<CanvasGroup>();

            defaultPromtText = inputPromptText.text;
            visualMeshObjectScale = visualMeshObject.transform.localScale;
        }

        public void SetKeyBoardZoomPosition(float value)
        {
            transform.DOLocalMoveZ(value, 0.01f);
            Debug.Log($"Actual Value is {value}");
        }
        void Update()
        {
            LookAtPlayer();
            //CheckForExternalInput();
        }

        private void CheckForExternalInput()
        {
            if (Input.anyKeyDown)
            {
                string input = Input.inputString;

                foreach (char c in input)
                {
                    if (char.IsLetterOrDigit(c) || char.IsPunctuation(c) || char.IsSymbol(c))
                    {
                        bool isUpper = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) || Input.GetKey(KeyCode.CapsLock);
                        ProcessInput(isUpper ? c.ToString().ToUpper() : c.ToString());
                    }
                    else if (c == '\b')
                    {
                        ProcessInput("BACKSPACE");
                    }
                }
            }
        }

        private void OnTextInput(char c)
        {
            if (char.IsLetterOrDigit(c) || char.IsPunctuation(c) || char.IsSymbol(c))
            {
                bool isUpper = Keyboard.current.leftShiftKey.wasPressedThisFrame || Keyboard.current.rightShiftKey.wasPressedThisFrame || Keyboard.current.capsLockKey.wasPressedThisFrame;
                ProcessInput(isUpper ? c.ToString().ToUpper() : c.ToString());
            }
            else if (c == '\b')
            {
                ProcessInput("BACKSPACE");
            }
        }
        private void LookAtPlayer()
        {
            Transform playerTransform;
            if (VE2API.Player.IsVRMode)
            {
                playerTransform = VE2API.Player.ActiveCamera.transform;

            }
            else
            {
                playerTransform = VE2API.Player.ActiveCamera.transform;
            }

            if (isLookAtActive)
            {
                Vector3 directionToPlayer = playerTransform.position - parentObject.position;
                parentObject.rotation = Quaternion.LookRotation(-directionToPlayer);
            }
            else
            {
                Vector3 directionToPlayer = playerTransform.position - transform.position;
                transform.rotation = Quaternion.LookRotation(-directionToPlayer);
            }





        }

        public void SetLookAtStatus(bool status)
        {
            isLookAtActive = status;

            if (isLookAtActive)
            {
                transform.localRotation = Quaternion.identity;
            }
        }
        public void SetParentPosition()
        {
            parentObject.position = transform.position;
            transform.localPosition = Vector3.zero;
        }
        public void EnableVirtualKeyboard(KeyboardConfig receivedKeyboardConfig)
        {

            keyboardConfig = receivedKeyboardConfig;

            if (!string.IsNullOrEmpty(keyboardConfig.inputPrompt))
            {
                inputPromptText.text = keyboardConfig.inputPrompt;
            }
            else
            {
                inputPromptText.text = defaultPromtText;
            }

            switch (keyboardConfig.KeyType)
            {
                case KeyType.NumPad:

                    ShowKeyBoard(numPadGameObject, true, outputTextshortUI, resultPromptTransformNumPad, resultPromptGroupNumPad);
                    break;
                case KeyType.FullKeyboard:
                    ShowKeyBoard(fullKeyBoardGameObject, true, outputTextLongUI, resultPromptTransformFullKeyboard, resultPromptGroupFullKeyboard);
                    currentFullKeyboardActionButtons = fullKeyBoardActionButtons;
                    break;
                default:
                    break;
            }

            OnTextUpdate.AddListener(UpdateOutputText);

            if (keyboardConfig.inheritCurrentText && keyboardConfig.inputField != null)
            {
                keyboardOutput = keyboardConfig.inputField.text;
                OnTextUpdate.Invoke(keyboardOutput);
            }
        }
        private void ShowKeyBoard(GameObject keyBoard, bool state, TMP_Text currentOutputTextUI, Transform currentResultPromptTransform, CanvasGroup currentresultPromptGroup)
        {
            outputTextUI = currentOutputTextUI;
            resultPromptTransform = currentResultPromptTransform;
            resultPromptGroup = currentresultPromptGroup;

            resultPromptTransform.localScale = Vector3.zero;
            resultPromptGroup.alpha = 0;
            keyBoard.SetActive(state);
            FadeIn();

        }
        public void FadeIn()
        {
            parentObject.transform.localScale = Vector3.zero;
            parentObject.transform.DOScale(Vector3.one, 1f).SetEase(Ease.OutBack);
        }

        public void FadeOut(GameObject objectToDestroy)
        {
            Sequence fadeOutSequence = DOTween.Sequence();

            fadeOutSequence.Join(separator.transform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack))
                .Join(
                DOTween.To(
                () => sepCG.alpha,
                x => sepCG.alpha = x,
                0f,
                0.2f)
            );
            //.Join(separator.GetComponent<CanvasGroup>().DOFade(0, 0.2f));

            fadeOutSequence.Join(inputPromptText.transform.DOScale(Vector3.zero, 0.5f).SetEase(Ease.InCubic))
                .Join(
                DOTween.To(
                () => inputCG.alpha,
                x => inputCG.alpha = x,
                0f,
                0.5f)
            );
            //.Join(inputPromptText.GetComponent<CanvasGroup>().DOFade(0, 0.5f));

            fadeOutSequence.Join(body.transform.DOScale(Vector3.zero, 0.5f).SetEase(Ease.InCubic))
                .Join(
                DOTween.To(
                () => bodyCG.alpha,
                x => bodyCG.alpha = x,
                0f,
                0.5f)
            );
            //.Join(body.GetComponent<CanvasGroup>().DOFade(0, 0.5f));

            fadeOutSequence.Join(parentObject.transform.DOScale(Vector3.zero, 0.5f).SetEase(Ease.InOutQuad))
                .Join(
                DOTween.To(
                () => parentCG.alpha,
                x => parentCG.alpha = x,
                0f,
                0.5f))
                //.Join(parentObject.GetComponent<CanvasGroup>().DOFade(0, 0.5f))
                .OnComplete(() =>
                {
                    DOTween.Kill(objectToDestroy);
                    Destroy(objectToDestroy);
                });

            fadeOutSequence.Play();
        }
        public void RegisterInput(string key)
        {
            ProcessInput(key);
        }

        private void ProcessInput(string key)
        {
            if (key.Length == 1)
            {
                if (keyboardOutput.Length == keyboardConfig.maxCharacters) return;

                keyboardOutput += key;
                OnTextUpdate.Invoke(keyboardOutput);
            }
            else if (key == "BACKSPACE")
            {
                if (keyboardOutput.Length > 0)
                {
                    keyboardOutput = keyboardOutput.Substring(0, keyboardOutput.Length - 1);
                    OnTextUpdate.Invoke(keyboardOutput);
                }
            }
            else if (key == "CHANGETONUMBERS")
            {
                ChangeFullKeyboard(fullKeyBoardNumbersActionButtons);
            }
            else if (key == "CHANGETOLETTERS")
            {
                ChangeFullKeyboard(fullKeyBoardActionButtons);
            }
            else if (key == "CHANGETOALTCHAR")
            {
                ChangeFullKeyboard(fullKeyBoardAltCharActionButtons);
            }
            else if (key == "CAPSLEFT" || key == "CAPSRIGHT")
            {
                if (isKeyBoardUpperCase)
                {
                    ChangeFullKeyboard(fullKeyBoardLowerCaseActionButtons);
                    isKeyBoardUpperCase = false;
                }
                else
                {
                    ChangeFullKeyboard(fullKeyBoardActionButtons);
                    isKeyBoardUpperCase = true;
                }

            }
            else
            {
                Debug.Log("Keyboard Ouput is Empty");
                return;
            }
        }

        private void UpdateOutputText(string text)
        {
            outputTextUI.text = text;

            if (keyboardConfig.inputField != null)
                keyboardConfig.inputField.text = text;

        }

        public void OnConfirmClicked()
        {
            if (keyboardConfig.acceptOnlyCorrectAnswer == true)
            {
                if (keyboardOutput == keyboardConfig.correctAnswer)
                {
                    ShowResultPrompt(true);
                    Debug.Log("Correct Answer");
                }
                else
                {
                    ShowResultPrompt(false);
                    Debug.Log("Incorrect Answer");
                }
            }
            else
            {
                InvokeSubmitEvent();
                Debug.Log("Confirm Just Clicked");
            }

            SetActionButtonStatus(false);
        }

        public void OnCancelClicked()
        {
            SetActionButtonStatus(false);
            DestroyKeyboard();
        }


        private void ChangeFullKeyboard(Button[] keysToChangeTo)
        {
            foreach (Button button in currentFullKeyboardActionButtons)
            {
                button.gameObject.SetActive(false);
            }
            foreach (Button button in keysToChangeTo)
            {
                button.gameObject.SetActive(true);
            }

            currentFullKeyboardActionButtons = keysToChangeTo;
        }
        private void SetActionButtonStatus(bool status)
        {
            switch (keyboardConfig.KeyType)
            {
                case KeyType.NumPad:
                    foreach (Button button in numPadActionButtons)
                    {
                        button.interactable = status;
                    }
                    break;

                case KeyType.FullKeyboard:
                    foreach (Button button in fullKeyBoardActionButtons)
                    {
                        button.interactable = status;
                    }
                    break;

                default:
                    break;
            }

        }

        public void ShowResultPrompt(bool isUserEntryCorrect)
        {

            this.isUserEntryCorrect = isUserEntryCorrect;
            if (isUserEntryCorrect)
            {
                resultPromptTransform.GetChild(0).gameObject.SetActive(true);
                resultPromptTransform.GetChild(1).gameObject.SetActive(false);
            }
            else
            {
                resultPromptTransform.GetChild(0).gameObject.SetActive(false);
                resultPromptTransform.GetChild(1).gameObject.SetActive(true);
            }

            Sequence sequence = DOTween.Sequence();
            sequence.Append(resultPromptTransform.DOScale(Vector3.one, 0.3f));
            sequence.Join(
                DOTween.To(
                () => resultPromptGroup.alpha,
                x => resultPromptGroup.alpha = x,
                1.0f,
                0.3f)
            );
            //sequence.Join(resultPromptGroup.DOFade(1.0f, 0.3f));

            sequence.OnComplete(InvokeSubmitEvent);

        }

        public void HideResultPrompt()
        {
            Sequence sequence = DOTween.Sequence();
            sequence.Append(resultPromptTransform.DOScale(Vector3.zero, 0.3f));
            sequence.Join(
                DOTween.To(
                () => resultPromptGroup.alpha,
                x => resultPromptGroup.alpha = x,
                0f,
                0.3f)
            );
            //sequence.Join(resultPromptGroup.DOFade(0.0f, 0.3f));

            sequence.OnComplete(() => SetActionButtonStatus(true));

        }

        private void InvokeSubmitEvent()
        {
            if (keyboardConfig.acceptOnlyCorrectAnswer)
            {
                DOVirtual.DelayedCall(0.6f, () =>
                {
                    keyboardConfig.OnSubmission.Invoke(keyboardOutput);
                    if (isUserEntryCorrect)
                    {
                        DestroyKeyboard();
                    }
                    else
                    {
                        HideResultPrompt();
                    }

                });

            }
            else
            {
                keyboardConfig.OnSubmission.Invoke(keyboardOutput);
                DestroyKeyboard();
            }

        }

        public void SetVisualMesh(bool status)
        {
            if (status)
            {
                visualMeshObject.SetActive(true);
                visualMeshObject.transform.localScale = Vector3.zero;
                visualMeshObject.transform.DOScale(visualMeshObjectScale, 0.5f).SetEase(Ease.OutBack).OnComplete(() =>
                {
                    DOVirtual.DelayedCall(5f, () => SetVisualMesh());
                });
            }
            else
            {
                if (!visualMeshObject.activeSelf) return;

                visualMeshObject.transform.DOScale(Vector3.zero, 0.5f).SetEase(Ease.InBack).OnComplete(() =>
                {
                    visualMeshObject.SetActive(false);
                });
            }
        }

        public void SetVisualMesh()
        {
            visualMeshObject.SetActive(false);
        }

        public void DestroyKeyboard()
        {
            if (transform.parent == null)
            {
                FadeOut(gameObject);
            }
            else
            {
                FadeOut(parentObject.gameObject);
            }
            //GameObject adjustablemirror = grabbableAdjustable.GetAdjustableProgrammaticMirror()?.gameObject;

            //if (adjustablemirror != null)
            //{
            //    Destroy(adjustablemirror);
            //}

        }

        private void OnDisable()
        {
            Keyboard.current.onTextInput -= OnTextInput;
        }
    }

    public enum KeyType
    {
        NumPad,
        FullKeyboard
    }
}

