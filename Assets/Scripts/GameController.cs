using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameController : MonoBehaviour
{
    public StoryScene currentScene;
    public BottomBarController bottomBar;
    public BackgroundController backgroundController;

    void Start()
    {
        bottomBar.PlayScene(currentScene);
        backgroundController.SetImage(currentScene.background);

        // ✅ Set current level in DataLogger
        if (DataLogger.Instance != null)
        {
            DataLogger.Instance.SetCurrentLevel(SceneManager.GetActiveScene().name);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Advance();
        }
    }

    // Public method to advance to the next sentence or scene (same logic as Space key)
    public void Advance()
    {
        if (!bottomBar.IsCompleted()) return;

        // Require algorithm choice before advancing
        if (bottomBar != null && !bottomBar.algorithmChosen)
        {
            Debug.LogWarning("Please choose an algorithm (PPO or DQN) before proceeding.");
            return;
        }

        if (bottomBar.IsLastSentence())
        {
            currentScene = currentScene.nextScene;
            bottomBar.PlayScene(currentScene);
            backgroundController.SwitchImage(currentScene.background);
            // reset algorithmChosen after advancing to the next scene/question
            if (bottomBar != null) bottomBar.algorithmChosen = false;
            // re-enable speak and done buttons for next question
            if (bottomBar != null)
            {
                bottomBar.SetSpeakButtonInteractable(true);
                bottomBar.SetDoneButtonInteractable(true);
            }
            // Clear any Vosk dialog text from the previous question
            var vosk = FindObjectOfType<VoskDialogText>();
            if (vosk != null) vosk.ClearDialogue();
        }
        else
        {
            bottomBar.PlayNextSentence();
            // reset algorithmChosen after advancing to the next sentence/question
            if (bottomBar != null) bottomBar.algorithmChosen = false;
            // re-enable speak and done buttons for next question
            if (bottomBar != null)
            {
                bottomBar.SetSpeakButtonInteractable(true);
                bottomBar.SetDoneButtonInteractable(true);
            }
            // Clear any Vosk dialog text from the previous question
            var vosk = FindObjectOfType<VoskDialogText>();
            if (vosk != null) vosk.ClearDialogue();
        }
    }
}
