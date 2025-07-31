using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Mime;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VE2;
using VE2.Common.API;
using VE2.Core.Player.API;

//"Spawns a virtual keyboard based on Keyboard config.Keyboard input feeds the attached InputField"
namespace VE2.Core.VComponents.Internal
{
    [RequireComponent(typeof(TMP_InputField))]
    public class V_InputFieldHandler : MonoBehaviour
    {
        private static int _keyboardInstanceCounter = 0;

        [SerializeField, HideLabel, IgnoreParent]
        KeyboardConfig keyboardConfig;

        [Range(0f, 1.0f)]
        public float distanceFromPlayer = 0.15f;

        private VirtualKeyboard virtualKeyboard;
        private Vector3 spawnPosition;
        private Quaternion spawnRotation;
        void OnEnable()
        {

            if (keyboardConfig.inputField != null || TryGetComponent(out keyboardConfig.inputField))
            {
                InitialiseConnectedInputField();
            }
        }

        private void InitialiseConnectedInputField()
        {
            if (keyboardConfig.inputField != null)
            {
                keyboardConfig.inputField.onSelect.AddListener(SpawnKeyBoard);

                keyboardConfig.inputField.onSelect.AddListener(ActivateInputField);
                keyboardConfig.inputField.onEndEdit.AddListener(DeactivateInputField);
            }
        }
        public void ActivateInputField(string message)
        {
            var playerServiceInternal = VE2API.Player as IPlayerServiceInternal;
            if (playerServiceInternal != null)
            {
                //keyboardConfig.inputField.onSelect.AddListener((_) => playerServiceInternal.InputFieldSelected(null, keyboardConfig.inputField));
                //keyboardConfig.inputField.onDeselect.AddListener((_) => playerServiceInternal.InputFieldDeselected(null, keyboardConfig.inputField));
                playerServiceInternal.InputFieldActive = true;
                keyboardConfig.inputField.ActivateInputField();
            }
        }

        public void DeactivateInputField(string message)
        {
            var playerServiceInternal = VE2API.Player as IPlayerServiceInternal;
            if (playerServiceInternal != null)
            {
                //keyboardConfig.inputField.onSelect.AddListener((_) => playerServiceInternal.InputFieldSelected(null, keyboardConfig.inputField));
                //keyboardConfig.inputField.onDeselect.AddListener((_) => playerServiceInternal.InputFieldDeselected(null, keyboardConfig.inputField));
                playerServiceInternal.InputFieldActive = false;
            }
        }
        public void DisableConnectedKeyboard()
        {
            virtualKeyboard.DestroyKeyboard();
        }
        public void SpawnKeyBoard(string selectedText = "")
        {
            if (!VE2API.Player.IsVRMode) return;

            Debug.Log("Spawning Keyboard");

            if (virtualKeyboard == null)
            {
                VirtualKeyboard existingKeyboard = FindFirstObjectByType<VirtualKeyboard>();
                if (existingKeyboard != null)
                {
                    existingKeyboard.DestroyKeyboard();
                }
                //if (StaticData.Utils.activeVirtualKeyboard != null)
                //{
                //    StaticData.Utils.activeVirtualKeyboard.DestroyKeyboard();
                //    StaticData.Utils.activeVirtualKeyboard = null;
                //}
                Transform playerTransform;
                GameObject objectToSpawn = Resources.Load<GameObject>("VirtualKeyboard");

                if (VE2API.Player.IsVRMode)
                {
                    distanceFromPlayer = 0.5f;
                    playerTransform = VE2API.Player.ActiveCamera.transform;
                    Vector3 downwardOffset = new Vector3(0, -0.3f, 0);
                    spawnPosition = playerTransform.position + playerTransform.forward * distanceFromPlayer + downwardOffset;
                    spawnRotation = Quaternion.Euler(15f, playerTransform.eulerAngles.y, 0f);
                }
                else
                {
                    distanceFromPlayer = 0.5f;
                    playerTransform = VE2API.Player.ActiveCamera.transform;
                    spawnPosition = playerTransform.position + playerTransform.forward * distanceFromPlayer;
                    spawnRotation = objectToSpawn.transform.rotation;
                }



                //GameObject spawnedObject = Instantiate(objectToSpawn, spawnPosition, spawnRotation);
                GameObject spawnedObject = SpawnVirtualKeyboardObject(objectToSpawn, spawnPosition, spawnRotation);

                //virtualKeyboard = spawnedObject.GetComponent<VirtualKeyboard>();
                virtualKeyboard = spawnedObject.GetComponentInChildren<VirtualKeyboard>();

                virtualKeyboard.EnableVirtualKeyboard(keyboardConfig);
                //StaticData.Utils.activeVirtualKeyboard = virtualKeyboard;
            }

        }

        private GameObject SpawnVirtualKeyboardObject(GameObject gameObjectToSpawn, Vector3 position, Quaternion rotation)
        {
            GameObject boot = new GameObject(name + "_boot");
            boot.SetActive(false);
            _keyboardInstanceCounter++;
            GameObject newGO = Instantiate(gameObjectToSpawn, position, rotation, boot.transform);
            newGO.name = $"{gameObjectToSpawn.name}_{_keyboardInstanceCounter}";
            newGO.transform.SetParent(null);
            Destroy(boot);
            newGO.SetActive(true);

            return newGO;
        }
    }

    [Serializable]
    public class KeyboardConfig
    {
        public TMP_InputField inputField;

        [HideInInspector] public TMP_InputField.ContentType KeyType => inputField.contentType;

        [HideInInspector] public int maxCharacters => inputField.characterLimit;

        [Tooltip("Gets and shows the current text in the connected input field when keyboard spawns")]
        public bool inheritCurrentText = false;

        public string inputPrompt = "Enter Here";

        [Tooltip("Accepts only correct keyboard input predefined in the correct answer field")]
        public bool acceptOnlyCorrectAnswer;

        //[ShowIf("acceptOnlyCorrectAnswer"), FoldoutGroup("Prompts")]
        //[InfoBox("@GetMaxCharErrorMessage()", InfoMessageType.Error, "@ValidateInputFromUser()")]
        public string correctAnswer = "";
        //[ShowIf("acceptOnlyCorrectAnswer"), FoldoutGroup("Prompts")]
        public string correctAnswerPrompt = "Correct Answer!";
        //[ShowIf("acceptOnlyCorrectAnswer"), FoldoutGroup("Prompts")]
        public string incorrectAnswerPrompt = "Incorrect Answer!";

        public UnityEvent<string> OnSubmission;

        public string GetMaxCharErrorMessage()
        {
            if (correctAnswer.Length > maxCharacters)
            {
                return "Correct Answer cannot be longer than maximum Characters Allowed";
            }

            return "";
        }

        public bool ValidateInputFromUser()
        {
            if (correctAnswer.Length > maxCharacters)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}

