using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    public Animator transitionAnimator;
    private string sceneToLoad;
    public static MenuManager Instance;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ChangeSceneByName(string sceneName)
    {
        transitionAnimator.gameObject.SetActive(true);
        sceneToLoad = sceneName;
        if (GameManager.instance != null)
        {
            GameManager.instance.LoadGameByName(sceneName);
        }
    }

    public void FadeOut() => transitionAnimator.SetTrigger("FadeOut");
    public void FadeIn() => transitionAnimator.SetTrigger("FadeIn");

    public void OnFadeComplete()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.LoadScene(sceneToLoad);
    }

    public void OnFadeInComplete()
    {
        transitionAnimator.gameObject.SetActive(false);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        transitionAnimator.SetTrigger("FadeIn");
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public void QuitGame()
    {
        Debug.Log("🚪 Quitting game...");

        // ✅ Save CSV before quitting
        if (DataLogger.Instance != null)
        {
            DataLogger.Instance.SaveCSV();
        }

        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
