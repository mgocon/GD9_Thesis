using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class BottomBarController : MonoBehaviour
{
    public TextMeshProUGUI barText;
    public TextMeshProUGUI personNameText;
    [Header("Speak Image Slide")]
    public RectTransform speakImage; // assign the image's RectTransform in inspector
    public float slideDistance = 150f; // how far to slide up
    public float slideDuration = 0.25f; // seconds

    private bool isSpeakImageVisible = false;

    private int sentenceIndex = -1;
    private StoryScene currentScene;
    private List<int> playbackOrder;
    private State state = State.COMPLETED;
    private enum State
    {
        PLAYING, COMPLETED
    }

    public void PlayScene(StoryScene scene)
    {
        currentScene = scene;
        sentenceIndex = -1;
        // build playback order
        int count = currentScene.sentences != null ? currentScene.sentences.Count : 0;
        int targetCount = (currentScene.sentencesToUse <= 0) ? count : Mathf.Clamp(currentScene.sentencesToUse, 1, count);

        // start with full index list
        List<int> indices = new List<int>(count);
        for (int i = 0; i < count; i++) indices.Add(i);

        if (currentScene != null && currentScene.randomizeSentences && count > 1)
        {
            // Fisher-Yates shuffle on indices
            for (int i = count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                int tmp = indices[i];
                indices[i] = indices[j];
                indices[j] = tmp;
            }
            // take the first targetCount shuffled indices
            playbackOrder = indices.GetRange(0, Mathf.Min(targetCount, indices.Count));
        }
        else
        {
            // take the first targetCount indices in order
            playbackOrder = indices.GetRange(0, Mathf.Min(targetCount, indices.Count));
        }
        PlayNextSentence();
    }

    public void PlayNextSentence()
    {
        sentenceIndex++;
        if (playbackOrder == null || sentenceIndex < 0 || sentenceIndex >= playbackOrder.Count) return;
        int idx = playbackOrder[sentenceIndex];
        StartCoroutine(TypeText(currentScene.sentences[idx].text));
        personNameText.text = currentScene.sentences[idx].speaker.speakerName;
        personNameText.color = currentScene.sentences[idx].speaker.textColor;
    }

    public bool IsCompleted()
    {
        return state == State.COMPLETED;
    }

    public bool IsLastSentence()
    {
        return playbackOrder != null && (sentenceIndex + 1 == playbackOrder.Count);
    }

    private IEnumerator TypeText(string text)
    {
        barText.text = "";
        state = State.PLAYING;
        int wordIndex = 0;

        while (state != State.COMPLETED)
        {
            barText.text += text[wordIndex];
            yield return new WaitForSeconds(0.05f);
            if(++wordIndex == text.Length)
            {
                state = State.COMPLETED;
                break;
            }
        }
    }

    // Called by the Speak button (wire this method to the button OnClick)
    public void OnSpeakButtonPressed()
    {
        if (speakImage == null) return;
        StopAllCoroutines(); // stop any running slide coroutines
        StartCoroutine(SlideSpeakImage(!isSpeakImageVisible));
    }

    private IEnumerator SlideSpeakImage(bool show)
    {
        isSpeakImageVisible = show;

        Vector2 start = speakImage.anchoredPosition;
        Vector2 end = start + (show ? Vector2.up * slideDistance : Vector2.down * slideDistance);
        float elapsed = 0f;

        // Optionally enable the object when showing
        if (show) speakImage.gameObject.SetActive(true);

        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / slideDuration);
            // smooth step for nicer movement
            t = Mathf.SmoothStep(0f, 1f, t);
            speakImage.anchoredPosition = Vector2.Lerp(start, end, t);
            yield return null;
        }

        speakImage.anchoredPosition = end;

        // Optionally disable when hidden
        if (!show) speakImage.gameObject.SetActive(false);
    }
}
