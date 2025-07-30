using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VE2.Common.API;
using VE2.Core.VComponents.API;

//This sits on all interactable elements in UI - buttons, toggles, input boxes
//And acts as a pseudo mouse event detector, triggering appropriate actions
//in the controls
public class Q_MouseEventDetector : MonoBehaviour
{
    private Q_Pointer pointer;
    RectTransform rt;

    public void SetQuiz(Q_Quiz quiz)
    {
        pointer = quiz.PointerForQuiz;
        if (pointer == null) 
            enabled = false;
    }

    Button buttonComponent;
    TMPro.TMP_InputField inputFieldComponent;
    Toggle toggleComponent;
    private void Start()
    {
        buttonComponent = GetComponent<Button>();
        toggleComponent = GetComponent<Toggle>();
        inputFieldComponent = GetComponent<TMPro.TMP_InputField>();
        rt = GetComponent<RectTransform>();
    }

    bool wasPointedAt = false;
    // Update is called once per frame
    void Update()
    {
        bool pointedAt = pointer.IsGrabbed 
            && RayIntersectsRectTransform(pointer.Origin.position, pointer.Origin.forward, rt, pointer.MaxRange)
            && ComponentEnabled();
            
        if (pointedAt && !wasPointedAt)
        {
            pointer.GetComponent<IV_HandheldActivatable>().OnActivate.AddListener(Activated);
        }
        if (!pointedAt && wasPointedAt)
        {
            pointer.GetComponent<IV_HandheldActivatable>().OnActivate.RemoveListener(Activated);
        }
        wasPointedAt = pointedAt;

        if (pointedAt) //tell pointer to show visual indicator of 'hovering'
            pointer.SetHover();
    }

    private bool ComponentEnabled()
    {
        if (buttonComponent != null)
            return buttonComponent.interactable;
        if (toggleComponent!=null)
            return toggleComponent.enabled;
        if (inputFieldComponent != null) 
            return true;
        return true;

    }

    private void Activated()
    {
        if (pointer.IsGrabbedLocally)
        {
            if (rt.TryGetComponent(out TMPro.TMP_InputField field))
            {
                FocusInputField(field);
            }
        }

        if (VE2API.InstanceService.IsHost)
        {
            if (rt.TryGetComponent(out Button button))
            {
                SimulateButtonClick(button);
            }

            if (rt.TryGetComponent(out Toggle toggle))
            {
                SimulateToggleClick(toggle);
            }
        }
    }

    private void OnDisable()
    {
        if (wasPointedAt)
        {
            wasPointedAt= false;
            pointer.GetComponent<IV_HandheldActivatable>().OnActivate.RemoveListener(Activated);
        }
    }

    //Stuff below - ChatGPT :-)

    /// <summary>
    /// Checks if a ray from a given origin in a given direction intersects a world-space RectTransform
    /// within a specified maximum distance.
    /// </summary>
    /// <param name="origin">The ray origin in world space.</param>
    /// <param name="direction">The normalized direction of the ray.</param>
    /// <param name="rectTransform">The RectTransform to test against.</param>
    /// <param name="maxDistance">The maximum range to detect intersections.</param>
    /// <returns>True if the ray intersects the RectTransform quad within range, false otherwise.</returns>
    public static bool RayIntersectsRectTransform(Vector3 origin, Vector3 direction, RectTransform rectTransform, float maxDistance)
    {
        Vector3[] worldCorners = new Vector3[4];
        rectTransform.GetWorldCorners(worldCorners);

        // Triangles from quad corners
        Vector3[] tri1 = new Vector3[] { worldCorners[0], worldCorners[1], worldCorners[2] };
        Vector3[] tri2 = new Vector3[] { worldCorners[2], worldCorners[3], worldCorners[0] };

        Ray ray = new Ray(origin, direction);

        // Check triangle 1
        if (RayIntersectsTriangle(ray, tri1[0], tri1[1], tri1[2], out float distance1) && distance1 <= maxDistance)
            return true;

        // Check triangle 2
        if (RayIntersectsTriangle(ray, tri2[0], tri2[1], tri2[2], out float distance2) && distance2 <= maxDistance)
            return true;

        return false;
    }

    /// <summary>
    /// Ray-triangle intersection using Möller–Trumbore algorithm.
    /// Returns true and outputs the intersection distance if hit.
    /// </summary>
    private static bool RayIntersectsTriangle(Ray ray, Vector3 v0, Vector3 v1, Vector3 v2, out float distance)
    {
        distance = 0f;

        Vector3 edge1 = v1 - v0;
        Vector3 edge2 = v2 - v0;

        Vector3 h = Vector3.Cross(ray.direction, edge2);
        float a = Vector3.Dot(edge1, h);
        if (Mathf.Abs(a) < Mathf.Epsilon)
            return false;

        float f = 1.0f / a;
        Vector3 s = ray.origin - v0;
        float u = f * Vector3.Dot(s, h);
        if (u < 0.0f || u > 1.0f)
            return false;

        Vector3 q = Vector3.Cross(s, edge1);
        float v = f * Vector3.Dot(ray.direction, q);
        if (v < 0.0f || u + v > 1.0f)
            return false;

        float t = f * Vector3.Dot(edge2, q);
        if (t > 0.0001f)
        {
            distance = t;
            return true;
        }

        return false;
    }

    public void FocusInputField(TMPro.TMP_InputField field)
    {
        if (field == null)
            return;

        // Set input field as selected
        EventSystem.current.SetSelectedGameObject(field.gameObject);

        // Activate the input field (this makes the caret visible and starts editing)
        field.OnPointerClick(new PointerEventData(EventSystem.current));
    }


    /// <summary>
    /// Simulates a full click on a Unity UI Button, including visual feedback.
    /// </summary>
    public static void SimulateButtonClick(Button button)
    {
        if (button == null || !button.gameObject.activeInHierarchy || !button.interactable)
            return;

        // Simulate pointer down
        ExecuteEvents.Execute(button.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerDownHandler);

        // Simulate pointer up
        ExecuteEvents.Execute(button.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerUpHandler);

        // Simulate click
        ExecuteEvents.Execute(button.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.submitHandler);
        // Fire the click event
        //button.onClick.Invoke();
    }

    /// <summary>
    /// Simulates a full click on a Unity UI Toggle, including visual transitions and state change.
    /// </summary>
    /// <param name="toggle">The Toggle to simulate a click on.</param>
    public static void SimulateToggleClick(Toggle toggle)
    {
        if (toggle == null || !toggle.gameObject.activeInHierarchy || !toggle.interactable)
            return;

        // 1. Trigger pointer down effect (e.g., color transition)
        var pointerData = new PointerEventData(EventSystem.current);
        ExecuteEvents.Execute(toggle.gameObject, pointerData, ExecuteEvents.pointerDownHandler);

        // 2. Trigger pointer up effect
        ExecuteEvents.Execute(toggle.gameObject, pointerData, ExecuteEvents.pointerUpHandler);

        // 3. Trigger submit behavior (equivalent to clicking or pressing Enter)
        ExecuteEvents.Execute(toggle.gameObject, pointerData, ExecuteEvents.submitHandler);
    }
}
