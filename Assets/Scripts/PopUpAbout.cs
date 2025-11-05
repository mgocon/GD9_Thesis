using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// NOTE: The file is named `PopUpAbout.cs` and the MonoBehaviour class is `PopUpAbout`.
// Unity requires the class name and file name to match for the script to be addable via the Inspector.
// The user asked for a file named "Pop up About" which would not allow a valid C# class name
// (spaces are not permitted in identifiers), so this file uses the safe name `PopUpAbout`.
public class PopUpAbout : MonoBehaviour
{
    [Header("About Popup")]
    public RectTransform aboutBox;
    public Button toggleButton;
    [Header("Content Pages")]
    public GameObject pageA;
    public GameObject pageB;
    // Removed contentToggleButton as it is no longer needed.
    [Header("Direct Page Buttons")]
    public Button buttonA; // show page A
    public Button buttonB; // show page B

    [Header("Slide Animation")]
    public float slideDistance = 200f;
    public float slideDuration = 0.25f;

    private Coroutine aboutCoroutine;
    private bool isVisible = false;
    private bool isPageAActive = true;

    private void Awake()
    {
        // Start hidden
        if (aboutBox != null)
            aboutBox.gameObject.SetActive(false);

        // If a toggle button is assigned, ensure it calls the toggle method at runtime
        if (toggleButton != null)
        {
            // Remove existing listeners so we don't double-bind
            toggleButton.onClick.RemoveAllListeners();
            toggleButton.onClick.AddListener(ToggleAboutPopup);
        }

        // Wire content toggle button if provided
            // (No content-toggle button needed — direct buttons A/B are used)
        // Wire direct page buttons
        if (buttonA != null)
        {
            buttonA.onClick.RemoveAllListeners();
            buttonA.onClick.AddListener(ShowPageA);
        }
        if (buttonB != null)
        {
            buttonB.onClick.RemoveAllListeners();
            buttonB.onClick.AddListener(ShowPageB);
        }

        // Initialize pages: show pageA by default
        if (pageA != null || pageB != null)
        {
            isPageAActive = true;
            SetPageState(isPageAActive);
        }
    }

    // Toggle the about popup: show if hidden, hide if visible
    public void ToggleAboutPopup()
    {
        if (aboutBox == null) return;

        if (isVisible)
            HideAboutPopup();
        else
            ShowAboutPopup();
    }

    // Note: content is switched via ShowPageA()/ShowPageB() using dedicated buttons A and B.

    // Show page A directly (and ensure popup is visible)
    public void ShowPageA()
    {
        isPageAActive = true;
        SetPageState(true);
        if (!isVisible) ShowAboutPopup();
    }

    // Show page B directly (and ensure popup is visible)
    public void ShowPageB()
    {
        isPageAActive = false;
        SetPageState(false);
        if (!isVisible) ShowAboutPopup();
    }

    // Set which page is active (true -> pageA, false -> pageB)
    public void SetPageState(bool showA)
    {
        if (pageA != null) pageA.SetActive(showA);
        if (pageB != null) pageB.SetActive(!showA);
    }

    public void ShowAboutPopup()
    {
        if (aboutBox == null) return;

        if (aboutCoroutine != null)
        {
            StopCoroutine(aboutCoroutine);
            aboutCoroutine = null;
        }

        aboutCoroutine = StartCoroutine(SlideBox(aboutBox, true, Vector2.up, slideDistance, slideDuration, () =>
        {
            aboutCoroutine = null;
            isVisible = true;
        }));
    }

    public void HideAboutPopup()
    {
        if (aboutBox == null) return;

        if (aboutCoroutine != null)
        {
            StopCoroutine(aboutCoroutine);
            aboutCoroutine = null;
        }

        aboutCoroutine = StartCoroutine(SlideBox(aboutBox, false, Vector2.up, slideDistance, slideDuration, () =>
        {
            aboutCoroutine = null;
            isVisible = false;
            aboutBox.gameObject.SetActive(false);
        }));
    }

    private IEnumerator SlideBox(RectTransform box, bool show, Vector2 direction, float distance, float duration, Action onComplete)
    {
        if (box == null)
        {
            onComplete?.Invoke();
            yield break;
        }

        Vector2 start = box.anchoredPosition;
        Vector2 end = start + (show ? direction * distance : -direction * distance);
        float elapsed = 0f;

        if (show) box.gameObject.SetActive(true);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            box.anchoredPosition = Vector2.Lerp(start, end, t);
            yield return null;
        }

        box.anchoredPosition = end;
        onComplete?.Invoke();
    }
}
