using UnityEngine;
using TMPro;
using System.Collections;

public class VoskDialogText : MonoBehaviour 
{
    public VoskSpeechToText VoskSpeechToText;
    public TextMeshProUGUI dialogueBox;
    public TextMeshProUGUI speakerNameText;
    public float typingSpeed = 0.05f;
    
    private string currentText = "";
    private bool isTyping = false;

    void Awake()
    {
        if (VoskManager.Instance != null)
        {
            VoskSpeechToText = VoskManager.Instance.GetSpeechToText();
        }

        if (VoskSpeechToText != null)
        {
            VoskSpeechToText.OnTranscriptionResult += OnTranscriptionResult;
        }
        else
        {
            Debug.LogWarning("No VoskSpeechToText found. Did you place VoskManager in your persistent scene?");
        }

        if (dialogueBox != null)
            dialogueBox.text = "";
    }

    void OnDestroy()
    {
        if (VoskSpeechToText != null)
        {
            VoskSpeechToText.OnTranscriptionResult -= OnTranscriptionResult;
        }
    }

    private void OnTranscriptionResult(string obj)
    {
        Debug.Log("Voice Input: " + obj);
        var result = new RecognitionResult(obj);
        
        foreach (RecognizedPhrase phrase in result.Phrases)
        {
            if (!string.IsNullOrEmpty(phrase.Text))
            {
                // Always append - continuous recognition
                AppendDialogue(phrase.Text);
                return;
            }
        }
    }

    public void DisplayDialogue(string speaker, string text)
    {
        // Stop any existing typing coroutine
        if (isTyping)
        {
            StopAllCoroutines();
            isTyping = false;
        }

        // Start typing effect (replaces existing text)
        StartCoroutine(TypeText(text, false));
    }

    public void AppendDialogue(string text)
    {
        // Stop any existing typing coroutine
        if (isTyping)
        {
            StopAllCoroutines();
            isTyping = false;
        }

        // Add space before appending if there's already text
        if (!string.IsNullOrEmpty(currentText))
        {
            text = " " + text;
        }

        // Start typing effect (appends to existing text)
        StartCoroutine(TypeText(text, true));
    }

    private IEnumerator TypeText(string text, bool append)
    {
        isTyping = true;
        
        if (!append)
        {
            currentText = "";
            dialogueBox.text = "";
        }

        foreach (char letter in text.ToCharArray())
        {
            currentText += letter;
            dialogueBox.text = currentText;
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
    }

    // Optional: Method to skip typing animation
    public void SkipTyping()
    {
        if (isTyping)
        {
            StopAllCoroutines();
            dialogueBox.text = currentText;
            isTyping = false;
        }
    }

    // Clear or reset the dialogue text (stop typing and empty fields)
    public void ClearDialogue()
    {
        if (isTyping)
        {
            StopAllCoroutines();
            isTyping = false;
        }

        currentText = "";

        if (dialogueBox != null)
            dialogueBox.text = "";
    }

    // Get current dialogue text (for feedback system)
    public string GetCurrentText()
    {
        return currentText;
    }

    // Get full displayed text
    public string GetDisplayedText()
    {
        return dialogueBox != null ? dialogueBox.text : "";
    }
}
