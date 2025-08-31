using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public GameObject loadingScreen;
    public ProgressBar bar;
    public TextMeshProUGUI textField;
    private float lastLoggedProgress = -1f;
    private void Awake()
    {
        instance = this;
        //Load scene additively, load in addition to the persistent scene
        SceneManager.LoadSceneAsync((int)SceneIndexes.MAIN_MENU, LoadSceneMode.Additive);
    }

    List<AsyncOperation> scenesLoading = new List<AsyncOperation>();
    public void LoadGame()
    {
        loadingScreen.gameObject.SetActive(true);
        scenesLoading.Add(SceneManager.UnloadSceneAsync((int)SceneIndexes.MAIN_MENU));
        scenesLoading.Add(SceneManager.LoadSceneAsync((int)SceneIndexes.TUTORIAL_SCENE, LoadSceneMode.Additive));
        StartCoroutine(GetSceneLoadProgress());
    }

    float totalSceneProgress;
    public IEnumerator GetSceneLoadProgress()
    {
        lastLoggedProgress = -1f;
        for (int i = 0; i < scenesLoading.Count; i++)
        {
            while (!scenesLoading[i].isDone)
            {
                totalSceneProgress = 0;
                foreach (AsyncOperation operation in scenesLoading)
                {
                    totalSceneProgress += operation.progress;
                }
                // Guard against division by zero
                if (scenesLoading.Count > 0)
                    totalSceneProgress = (totalSceneProgress / scenesLoading.Count) * 100f; // multiply by 100 to get percentage
                else
                    totalSceneProgress = 100f;

                // Update progress bar (rounded as before)
                bar.current = Mathf.RoundToInt(totalSceneProgress);

                // Update UI text with 4 decimal places
                if (textField != null)
                    textField.text = string.Format("Loading: {0:F4}%", totalSceneProgress);

                // Log only when progress increases by at least 1% to reduce spam
                if (lastLoggedProgress < 0f || Mathf.Abs(totalSceneProgress - lastLoggedProgress) >= 1f)
                {
                    Debug.LogFormat("GameManager: Loading progress: {0:F4}%", totalSceneProgress);
                    lastLoggedProgress = totalSceneProgress;
                }

                yield return null;
            }
        }

        // Wait for VoskSpeechToText to be ready
        VoskSpeechToText vosk = FindObjectOfType<VoskSpeechToText>();
        if (vosk != null)
        {
            // While Vosk initializes, display its decompression progress with decimals
            while (!vosk.IsReady)
            {
                float voskProgress = Mathf.Clamp(vosk.progress, 0f, 100f);
                if (textField != null)
                    textField.text = string.Format("Loading model: {0:F4}%", voskProgress);
                Debug.LogFormat("GameManager: Vosk decompress progress: {0:F4}%", voskProgress);
                yield return null;
            }
            Debug.Log("GameManager: Vosk is ready.");
        }

        loadingScreen.gameObject.SetActive(false);
        scenesLoading.Clear(); // Clear for future loads
    }
}
