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
    }

    void Update()
    {
        // Ignore spacebar when the end-of-level popup is active
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (bottomBar != null && bottomBar.IsEndPopupActive())
            {
                // Swallow the input while end popup is shown
                return;
            }
            Advance();
        }
    }

    // Check if we're currently in a tutorial scene
    private bool IsTutorialScene()
    {
        // Check all loaded scenes, not just the active one
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            string sceneName = scene.name;
            
            if (sceneName == "Tutorial" || 
                sceneName == "TutorialScene" || 
                sceneName == "Tutorial Scene")
            {
                return true;
            }
        }
        return false;
    }

    public void Advance()
    {
        if (!bottomBar.IsCompleted()) return;

        // Check if we're in tutorial dynamically
        bool isTutorialScene = IsTutorialScene();
        
        // Determine if algorithm choice is required:
        // - In tutorial: only require it if the current sentence is a question
        // - Outside tutorial: always require it
        bool isQuestion = bottomBar != null && bottomBar.IsCurrentSentenceAQuestion();
        bool requiresAlgorithmChoice = !isTutorialScene || isQuestion;

        if (requiresAlgorithmChoice && !bottomBar.algorithmChosen)
        {
            Debug.LogWarning("Please choose an algorithm (PPO or DQN) before proceeding.");
            return;
        }

        if (bottomBar.IsLastSentence())
        {
            Debug.Log($"Last sentence reached. currentScene.nextScene = {(currentScene.nextScene == null ? "NULL" : currentScene.nextScene.name)}");
            
            // If there is no next scene, show the end-of-level popup
            if (currentScene.nextScene == null)
            {
                Debug.Log("No next scene - calling ShowEndPopup()");
                if (bottomBar != null)
                {
                    bottomBar.ShowEndPopup();
                }
                var vosk = FindObjectOfType<VoskDialogText>();
                if (vosk != null) vosk.ClearDialogue();
                return;
            }

            // Otherwise proceed to the next scene
            currentScene = currentScene.nextScene;
            bottomBar.PlayScene(currentScene);
            backgroundController.SwitchImage(currentScene.background);
            
            // Reset algorithmChosen after advancing to the next scene/question
            if (bottomBar != null) bottomBar.algorithmChosen = false;
            
            // Re-enable speak and done buttons for next question
            if (bottomBar != null)
            {
                bottomBar.SetSpeakButtonInteractable(true);
                bottomBar.SetDoneButtonInteractable(true);
            }
            
            // Clear any Vosk dialog text from the previous question
            var vosk2 = FindObjectOfType<VoskDialogText>();
            if (vosk2 != null) vosk2.ClearDialogue();
        }
        else
        {
            bottomBar.PlayNextSentence();
            
            // Reset algorithmChosen after advancing to the next sentence/question
            if (bottomBar != null) bottomBar.algorithmChosen = false;
            
            // Re-enable speak and done buttons for next question
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
