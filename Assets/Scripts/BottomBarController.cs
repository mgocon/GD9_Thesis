using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

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
        }
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
    public void OnAlgorithmButtonClicked_DQN() => DataLogger.Instance?.LogAlgorithmChoice("DQN");
    public void OnAlgorithmButtonClicked_PPO() => DataLogger.Instance?.LogAlgorithmChoice("PPO");
}
