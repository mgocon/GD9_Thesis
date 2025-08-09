using UnityEngine;
using TMPro;
using System.Collections;

public class VoskDialogText : MonoBehaviour 
{
    public VoskSpeechToText VoskSpeechToText;
    public TextMeshProUGUI dialogueBox;        // Dialog box for displaying text
    public TextMeshProUGUI speakerNameText;    // Optional: Text field for speaker name
    public float typingSpeed = 0.05f;          // Speed of text appearing
    
    private string currentText = "";
    private bool isTyping = false;

    void Awake()
    {
        VoskSpeechToText.OnTranscriptionResult += OnTranscriptionResult;
        
        // Initialize text components
        if (dialogueBox != null)
            dialogueBox.text = "";
        if (speakerNameText != null)
            speakerNameText.text = "Player"; // Name of Player
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
                DisplayDialogue("Player", phrase.Text);
                return;
            }
        }
    }

    public void DisplayDialogue(string speaker, string text)
    {
        // Update speaker name if available
        if (speakerNameText != null)
            speakerNameText.text = speaker;

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
