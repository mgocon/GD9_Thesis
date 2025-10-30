using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    private string sceneToLoad;
    public static MenuManager Instance;

    void Awake()
    {
        int managerIndex = (int)SceneIndexes.MANAGER;

        if (Instance == null)
        {
            Instance = this;
            // Only make this MenuManager persistent if it lives in the persistent manager scene.
            // If it's placed in the Main Menu scene, keep it scene-local so button OnClick references remain valid.
            if (gameObject.scene.buildIndex == managerIndex)
            {
                DontDestroyOnLoad(gameObject);
            }
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    public void ChangeSceneByName(string sceneName)
    {
        sceneToLoad = sceneName;
        if (GameManager.instance != null)
        {
            GameManager.instance.LoadGameByName(sceneName);
        }
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
