using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    public Animator transitionAnimator; // assign in inspector
    private string sceneToLoad;

    public static MenuManager Instance; // Singleton

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // persist across scenes
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ChangeSceneByName(string sceneName)
    {
        transitionAnimator.gameObject.SetActive(true); // enable before fade-out
        sceneToLoad = sceneName;
        transitionAnimator.SetTrigger("FadeOut");
    }

    public void OnFadeComplete()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.LoadScene(sceneToLoad);
    }

    public void OnFadeInComplete()
    {
        transitionAnimator.gameObject.SetActive(false); // disables the Image object
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Play fade-in animation
        transitionAnimator.SetTrigger("FadeIn");
        SceneManager.sceneLoaded -= OnSceneLoaded; // remove listener
    }

    public void QuitGame()
    {
        Debug.Log("Quitting game...");
        Application.Quit();

        // If testing in editor
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
