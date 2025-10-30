using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class BottomBarController : MonoBehaviour
{
    public TextMeshProUGUI barText;
    public TextMeshProUGUI personNameText;

    [Header("Speak Image Slide")]
    public RectTransform speakImage;
    public float slideDistance = 150f;
    public float slideDuration = 0.25f;

    [Header("Voice Recognition")]
    public VoskSpeechToText voskSpeechToText;

    private bool isSpeakImageVisible = false;
    private Coroutine slideCoroutine;
    [Header("Speak Button")]
    public Button speakButton;
    private bool isRecording = false;
    [Header("Done Button")]
    public Button doneButton;
    
    [Header("Popup Boxes")]
    public RectTransform leftBox;
    public RectTransform rightBox;
    public float boxesSlideDistance = 300f;
    public float boxesSlideDuration = 0.25f;

    private bool areBoxesVisible = false;
    private Coroutine leftBoxCoroutine;
    private Coroutine rightBoxCoroutine;
    private int sentenceIndex = -1;
    private StoryScene currentScene;
    private List<int> playbackOrder;
    private State state = State.COMPLETED;

    private enum State { PLAYING, COMPLETED }

    public void PlayScene(StoryScene scene)
    {
        currentScene = scene;
        sentenceIndex = -1;

        int count = currentScene.sentences != null ? currentScene.sentences.Count : 0;
        int targetCount = (currentScene.sentencesToUse <= 0) ? count : Mathf.Clamp(currentScene.sentencesToUse, 1, count);

        List<int> indices = new List<int>(count);
        for (int i = 0; i < count; i++) indices.Add(i);

        if (currentScene != null && currentScene.randomizeSentences && count > 1)
        {
            for (int i = count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                int tmp = indices[i];
                indices[i] = indices[j];
                indices[j] = tmp;
            }
            playbackOrder = indices.GetRange(0, Mathf.Min(targetCount, indices.Count));
        }
        else
        {
            playbackOrder = indices.GetRange(0, Mathf.Min(targetCount, indices.Count));
        }

        PlayNextSentence();
    }

    public void PlayNextSentence()
    {
        sentenceIndex++;
        if (playbackOrder == null || sentenceIndex < 0 || sentenceIndex >= playbackOrder.Count) return;

        int idx = playbackOrder[sentenceIndex];
        string sentenceText = currentScene.sentences[idx].text;

        StartCoroutine(TypeText(sentenceText));
        personNameText.text = currentScene.sentences[idx].speaker.speakerName;
        personNameText.color = currentScene.sentences[idx].speaker.textColor;

        // ✅ Log this sentence to CSV
        if (DataLogger.Instance != null)
        {
            DataLogger.Instance.LogSentence(sentenceText, idx);
        }
    }

    public bool IsCompleted() => state == State.COMPLETED;

    public bool IsLastSentence() => playbackOrder != null && (sentenceIndex + 1 == playbackOrder.Count);

    private IEnumerator TypeText(string text)
    {
        barText.text = "";
        state = State.PLAYING;
        int wordIndex = 0;

        while (state != State.COMPLETED)
        {
            barText.text += text[wordIndex];
            yield return new WaitForSeconds(0.05f);
            if (++wordIndex == text.Length)
            {
                state = State.COMPLETED;
                break;
            }
        }
    }

    public void OnSpeakButtonPressed()
    {
        if (speakImage == null) return;
        if (slideCoroutine != null)
        {
            StopCoroutine(slideCoroutine);
            slideCoroutine = null;
        }
        slideCoroutine = StartCoroutine(SlideSpeakImage(!isSpeakImageVisible));
        if (voskSpeechToText != null)
        {
            voskSpeechToText.ToggleRecording();
            isRecording = !isRecording;
        }

        // disable speak button after pressing once to avoid multiple toggles
        if (speakButton != null)
            speakButton.interactable = false;
    }

    private IEnumerator SlideSpeakImage(bool show)
    {
        isSpeakImageVisible = show;
        Vector2 start = speakImage.anchoredPosition;
        Vector2 end = start + (show ? Vector2.up * slideDistance : Vector2.down * slideDistance);
        float elapsed = 0f;
        if (show) speakImage.gameObject.SetActive(true);

        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / slideDuration);
            speakImage.anchoredPosition = Vector2.Lerp(start, end, t);
            yield return null;
        }

        speakImage.anchoredPosition = end;
        slideCoroutine = null;
        if (!show) speakImage.gameObject.SetActive(false);
    }

    // Toggle both popup boxes (left from left, right from right)
    public void OnPopupBoxesPressed()
    {
        areBoxesVisible = !areBoxesVisible;

        // Left box: slides horizontally to the right when showing
        if (leftBox != null)
        {
            if (leftBoxCoroutine != null)
            {
                StopCoroutine(leftBoxCoroutine);
                leftBoxCoroutine = null;
            }
            leftBoxCoroutine = StartCoroutine(SlideBox(leftBox, areBoxesVisible, Vector2.right, boxesSlideDistance, boxesSlideDuration, () => leftBoxCoroutine = null));
        }

        // Right box: slides horizontally to the left when showing
        if (rightBox != null)
        {
            if (rightBoxCoroutine != null)
            {
                StopCoroutine(rightBoxCoroutine);
                rightBoxCoroutine = null;
            }
            rightBoxCoroutine = StartCoroutine(SlideBox(rightBox, areBoxesVisible, Vector2.left, boxesSlideDistance, boxesSlideDuration, () => rightBoxCoroutine = null));
        }
    }

    // New Done button: toggles popup boxes and ensures Vosk recording is stopped
    public void OnDoneButtonPressed()
    {
        // If Vosk dialog text is empty, do not allow Done to proceed
        var voskDialog = FindObjectOfType<VoskDialogText>();
        if (voskDialog != null && (voskDialog.dialogueBox == null || string.IsNullOrWhiteSpace(voskDialog.dialogueBox.text)))
        {
            Debug.LogWarning("Done is disabled until there is recognized speech in the dialog.");
            return;
        }
        // Toggle boxes same as OnPopupBoxesPressed
        areBoxesVisible = !areBoxesVisible;

        if (leftBox != null)
        {
            if (leftBoxCoroutine != null)
            {
                StopCoroutine(leftBoxCoroutine);
                leftBoxCoroutine = null;
            }
            leftBoxCoroutine = StartCoroutine(SlideBox(leftBox, areBoxesVisible, Vector2.right, boxesSlideDistance, boxesSlideDuration, () => leftBoxCoroutine = null));
        }

        if (rightBox != null)
        {
            if (rightBoxCoroutine != null)
            {
                StopCoroutine(rightBoxCoroutine);
                rightBoxCoroutine = null;
            }
            rightBoxCoroutine = StartCoroutine(SlideBox(rightBox, areBoxesVisible, Vector2.left, boxesSlideDistance, boxesSlideDuration, () => rightBoxCoroutine = null));
        }

        // If Vosk is recording, stop it
        if (isRecording && voskSpeechToText != null)
        {
            voskSpeechToText.ToggleRecording();
            isRecording = false;
        }

        // disable Done button to avoid multiple presses until next question
        if (doneButton != null)
            doneButton.interactable = false;
    }

    private IEnumerator SlideBox(RectTransform box, bool show, Vector2 direction, float distance, float duration, System.Action onComplete)
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

        if (!show) box.gameObject.SetActive(false);
        onComplete?.Invoke();
    }

    private void Awake()
    {
        if (voskSpeechToText == null)
        {
            var voskManager = GameObject.Find("VoskManager");
            if (voskManager != null)
            {
                voskSpeechToText = voskManager.GetComponent<VoskSpeechToText>();
            }
            if (voskSpeechToText == null)
            {
                voskSpeechToText = FindObjectOfType<VoskSpeechToText>();
            }
            if (voskSpeechToText == null)
            {
                Debug.LogWarning("VoskSpeechToText not found in scene.");
            }
        }
    }

    // ✅ Optional UI helper methods for buttons
    // Tracks whether the player has selected an algorithm (PPO or DQN)
    [HideInInspector]
    public bool algorithmChosen = false;

    public void OnAlgorithmButtonClicked_DQN()
    {
        DataLogger.Instance?.LogAlgorithmChoice("DQN");
        algorithmChosen = true;
        // Hide speak image and popup boxes after choice
        HideUIAfterAlgorithmChoice();
        // Automatically advance to next question
        var gc = FindObjectOfType<GameController>();
        if (gc != null) gc.Advance();
    }

    public void OnAlgorithmButtonClicked_PPO()
    {
        DataLogger.Instance?.LogAlgorithmChoice("PPO");
        algorithmChosen = true;
        // Hide speak image and popup boxes after choice
        HideUIAfterAlgorithmChoice();
        // Automatically advance to next question
        var gc = FindObjectOfType<GameController>();
        if (gc != null) gc.Advance();
    }

    // Hide speak image and popup boxes using existing coroutines
    private void HideUIAfterAlgorithmChoice()
    {
        // Hide speak image if visible
        if (isSpeakImageVisible && speakImage != null)
        {
            if (slideCoroutine != null)
            {
                StopCoroutine(slideCoroutine);
                slideCoroutine = null;
            }
            slideCoroutine = StartCoroutine(SlideSpeakImage(false));
        }

        // Hide popup boxes if visible
        if (areBoxesVisible)
        {
            // OnPopupBoxesPressed toggles visibility and starts the hide coroutines
            OnPopupBoxesPressed();
        }
    }

    // Allow external code (GameController) to re-enable the speak button when moving to next question
    public void SetSpeakButtonInteractable(bool enabled)
    {
        if (speakButton != null)
            speakButton.interactable = enabled;
    }

    // Allow external code (GameController) to re-enable the Done button when moving to next question
    public void SetDoneButtonInteractable(bool enabled)
    {
        if (doneButton != null)
            doneButton.interactable = enabled;
    }
}
