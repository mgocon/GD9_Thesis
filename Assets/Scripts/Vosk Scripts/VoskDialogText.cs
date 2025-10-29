using UnityEngine;
using TMPro;
using System.Collections;

public class VoskDialogText : MonoBehaviour 
{
    public VoskSpeechToText VoskSpeechToText;  // Auto-linked to persistent one
    public TextMeshProUGUI dialogueBox;        // Dialog box for displaying text
    public float typingSpeed = 0.05f;          // Speed of text appearing
    
    private string currentText = "";
    private bool isTyping = false;

    void Awake()
    {
        // Ask VoskManager for the speech-to-text instance
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

        // Initialize text field
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
                // Display the recognized speech in the dialogue box
                DisplayDialogue(phrase.Text);
                return;
            }
        }
    }

    public void DisplayDialogue(string text)
    {
        // Stop any existing typing coroutine
        if (isTyping)
        {
            StopAllCoroutines();
            isTyping = false;
        }

        // Start typing effect
        StartCoroutine(TypeText(text));
    }

    private IEnumerator TypeText(string text)
    {
        isTyping = true;
        currentText = "";
        dialogueBox.text = "";

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
}
