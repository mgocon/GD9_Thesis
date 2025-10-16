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
    private State state = State.COMPLETED;
    private enum State
    {
        PLAYING, COMPLETED
    }

    public void PlayScene(StoryScene scene)
    {
        currentScene = scene;
        sentenceIndex = -1;
        PlayNextSentence();
    }

    public void PlayNextSentence()
    {
        StartCoroutine(TypeText(currentScene.sentences[++sentenceIndex].text));
        personNameText.text = currentScene.sentences[sentenceIndex].speaker.speakerName;
        personNameText.color = currentScene.sentences[sentenceIndex].speaker.textColor;
    }

    public bool IsCompleted()
    {
        return state == State.COMPLETED;
    }

    public bool IsLastSentence()
    {
        return sentenceIndex + 1 == currentScene.sentences.Count;
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
