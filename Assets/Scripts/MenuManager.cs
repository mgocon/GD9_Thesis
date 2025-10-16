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
        // store requested scene and delegate to GameManager
        // sceneToLoad = sceneName;
        // transitionAnimator.SetTrigger("FadeOut");
        // SceneManager.LoadScene(sceneName);
        transitionAnimator.gameObject.SetActive(true);
        sceneToLoad = sceneName;
        if (GameManager.instance != null)
        {
            GameManager.instance.LoadGameByName(sceneName);
        }
    }

    public void FadeOut()
    {
        transitionAnimator.SetTrigger("FadeOut");
    }
    public void FadeIn()
    {
        transitionAnimator.SetTrigger("FadeIn");
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
