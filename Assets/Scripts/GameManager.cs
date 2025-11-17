using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public GameObject loadingScreen;

    private void Awake()
    {
        instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.LoadSceneAsync((int)SceneIndexes.MAIN_MENU, LoadSceneMode.Additive);
    }

    List<AsyncOperation> scenesLoading = new List<AsyncOperation>();

    public void LoadGame(string sceneName)
    {
        if (loadingScreen != null) loadingScreen.gameObject.SetActive(true);
        scenesLoading.Clear();

        int targetIndex;
        bool targetIsIndex = int.TryParse(sceneName, out targetIndex);

        int managerIndex = (int)SceneIndexes.MANAGER;

        int loadedCount = SceneManager.sceneCount;
        for (int i = 0; i < loadedCount; i++)
        {
            var s = SceneManager.GetSceneAt(i);
            if (!s.isLoaded) continue;
            if (s.buildIndex == managerIndex) continue;
            if (targetIsIndex && s.buildIndex == targetIndex) continue;
            if (!targetIsIndex && s.name == sceneName) continue;

            scenesLoading.Add(SceneManager.UnloadSceneAsync(s));
        }

        bool targetLoaded = false;
        if (targetIsIndex)
        {
            var sc = SceneManager.GetSceneByBuildIndex(targetIndex);
            targetLoaded = sc.isLoaded;
        }
        else
        {
            var sc = SceneManager.GetSceneByName(sceneName);
            targetLoaded = sc.isLoaded;
        }

        if (!targetLoaded)
        {
            if (targetIsIndex)
                scenesLoading.Add(SceneManager.LoadSceneAsync(targetIndex, LoadSceneMode.Additive));
            else
                scenesLoading.Add(SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive));
        }

        StartCoroutine(GetSceneLoadProgress());
    }

    public void LoadGameByName(string sceneName)
    {
        LoadGame(sceneName);
    }

    public IEnumerator GetSceneLoadProgress()
    {
        // Wait for Unity scenes to load
        for (int i = 0; i < scenesLoading.Count; i++)
        {
            while (!scenesLoading[i].isDone)
            {
                yield return null;
            }
        }

        // ✅ Wait for Vosk initialization (success or failure)
        VoskSpeechToText vosk = FindObjectOfType<VoskSpeechToText>();
        if (vosk != null)
        {
            // Wait until initialization is complete (either success or failure)
            while (!vosk.IsInitializationComplete)
            {
                yield return null;
            }

            if (vosk.HasInitializationFailed)
            {
                Debug.LogError("❌ Vosk initialization failed - returning to main menu");
                
                // Hide loading screen
                if (loadingScreen != null)
                    loadingScreen.gameObject.SetActive(false);
                
                // Show error dialog
                ShowVoskErrorDialog();
                
                yield break; // Stop here, don't proceed to game
            }
        }

        // Hide loading screen only when everything is ready
        EnsureSingleEventSystem();
        TryRebindMainMenuButtons();
        if (loadingScreen != null)
            loadingScreen.gameObject.SetActive(false);
    }

    private void ShowVoskErrorDialog()
    {
        // Create a simple error dialog
        GameObject dialogObj = new GameObject("VoskErrorDialog");
        dialogObj.transform.SetParent(transform);
        
        Canvas canvas = dialogObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;
        
        dialogObj.AddComponent<CanvasScaler>();
        dialogObj.AddComponent<GraphicRaycaster>();

        // Background panel
        GameObject panel = new GameObject("Panel");
        panel.transform.SetParent(dialogObj.transform);
        Image panelImg = panel.AddComponent<Image>();
        panelImg.color = new Color(0, 0, 0, 0.8f);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        // Error message box
        GameObject messageBox = new GameObject("MessageBox");
        messageBox.transform.SetParent(panel.transform);
        Image boxImg = messageBox.AddComponent<Image>();
        boxImg.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        RectTransform boxRect = messageBox.GetComponent<RectTransform>();
        boxRect.sizeDelta = new Vector2(500, 300);
        boxRect.anchoredPosition = Vector2.zero;

        // Title text
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(messageBox.transform);
        Text titleText = titleObj.AddComponent<Text>();
        titleText.text = "Voice Recognition Error";
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 24;
        titleText.color = Color.red;
        titleText.alignment = TextAnchor.MiddleCenter;
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 0.7f);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.offsetMin = new Vector2(20, -10);
        titleRect.offsetMax = new Vector2(-20, -10);

        // Message text
        GameObject msgObj = new GameObject("Message");
        msgObj.transform.SetParent(messageBox.transform);
        Text msgText = msgObj.AddComponent<Text>();
        msgText.text = "Failed to initialize voice recognition.\n\n" +
                       "Possible causes:\n" +
                       "• Not enough disk space (2GB required)\n" +
                       "• Missing voice model files\n" +
                       "• Corrupted installation\n\n" +
                       "The game cannot continue without voice input.";
        msgText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        msgText.fontSize = 16;
        msgText.color = Color.white;
        msgText.alignment = TextAnchor.UpperLeft;
        RectTransform msgRect = msgObj.GetComponent<RectTransform>();
        msgRect.anchorMin = new Vector2(0, 0.3f);
        msgRect.anchorMax = new Vector2(1, 0.7f);
        msgRect.offsetMin = new Vector2(20, 10);
        msgRect.offsetMax = new Vector2(-20, -10);

        // Main Menu button
        GameObject menuBtnObj = new GameObject("MainMenuButton");
        menuBtnObj.transform.SetParent(messageBox.transform);
        Button menuBtn = menuBtnObj.AddComponent<Button>();
        Image menuBtnImg = menuBtnObj.AddComponent<Image>();
        menuBtnImg.color = new Color(0.3f, 0.6f, 0.9f, 1f);
        RectTransform menuBtnRect = menuBtnObj.GetComponent<RectTransform>();
        menuBtnRect.sizeDelta = new Vector2(200, 40);
        menuBtnRect.anchoredPosition = new Vector2(-110, 30);

        GameObject menuBtnText = new GameObject("Text");
        menuBtnText.transform.SetParent(menuBtnObj.transform);
        Text menuText = menuBtnText.AddComponent<Text>();
        menuText.text = "Main Menu";
        menuText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        menuText.fontSize = 18;
        menuText.color = Color.white;
        menuText.alignment = TextAnchor.MiddleCenter;
        RectTransform menuTextRect = menuBtnText.GetComponent<RectTransform>();
        menuTextRect.anchorMin = Vector2.zero;
        menuTextRect.anchorMax = Vector2.one;
        menuTextRect.offsetMin = Vector2.zero;
        menuTextRect.offsetMax = Vector2.zero;

        menuBtn.onClick.AddListener(() => {
            Destroy(dialogObj);
            LoadGameByName(((int)SceneIndexes.MAIN_MENU).ToString());
        });

        // Quit button
        GameObject quitBtnObj = new GameObject("QuitButton");
        quitBtnObj.transform.SetParent(messageBox.transform);
        Button quitBtn = quitBtnObj.AddComponent<Button>();
        Image quitBtnImg = quitBtnObj.AddComponent<Image>();
        quitBtnImg.color = new Color(0.8f, 0.3f, 0.3f, 1f);
        RectTransform quitBtnRect = quitBtnObj.GetComponent<RectTransform>();
        quitBtnRect.sizeDelta = new Vector2(200, 40);
        quitBtnRect.anchoredPosition = new Vector2(110, 30);

        GameObject quitBtnText = new GameObject("Text");
        quitBtnText.transform.SetParent(quitBtnObj.transform);
        Text quitText = quitBtnText.AddComponent<Text>();
        quitText.text = "Quit Game";
        quitText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        quitText.fontSize = 18;
        quitText.color = Color.white;
        quitText.alignment = TextAnchor.MiddleCenter;
        RectTransform quitTextRect = quitBtnText.GetComponent<RectTransform>();
        quitTextRect.anchorMin = Vector2.zero;
        quitTextRect.anchorMax = Vector2.one;
        quitTextRect.offsetMin = Vector2.zero;
        quitTextRect.offsetMax = Vector2.zero;

        quitBtn.onClick.AddListener(() => {
            Debug.Log("Quitting application due to Vosk initialization failure");
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        });
    }

    private void EnsureSingleEventSystem()
    {
        var systems = FindObjectsOfType<EventSystem>();
        if (systems == null || systems.Length <= 1) return;

        int managerIndex = (int)SceneIndexes.MANAGER;
        EventSystem keeper = null;

        foreach (var es in systems)
        {
            if (es.gameObject.scene.buildIndex == managerIndex)
            {
                keeper = es;
                break;
            }
        }

        if (keeper == null) keeper = systems[0];

        foreach (var es in systems)
        {
            if (es == keeper) continue;
            try
            {
                Debug.Log("Destroying duplicate EventSystem: " + es.gameObject.name);
                Destroy(es.gameObject);
            }
            catch { }
        }
    }

    private void TryRebindMainMenuButtons()
    {
        int mainMenuIndex = (int)SceneIndexes.MAIN_MENU;
        var scene = SceneManager.GetSceneByBuildIndex(mainMenuIndex);
        if (!scene.isLoaded) return;

        var roots = scene.GetRootGameObjects();
        foreach (var root in roots)
        {
            var buttons = root.GetComponentsInChildren<Button>(true);
            foreach (var btn in buttons)
            {
                if (btn.onClick.GetPersistentEventCount() > 0) continue;

                string name = btn.gameObject.name.ToLowerInvariant();
                bool bound = false;

                if (name.Contains("play") || name.Contains("start"))
                {
                    int idx = (int)SceneIndexes.ENTRY_LEVEL;
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => LoadGameByName(idx.ToString()));
                    bound = true;
                }
                else if (name.Contains("tutorial"))
                {
                    int idx = (int)SceneIndexes.TUTORIAL_SCENE;
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => LoadGameByName(idx.ToString()));
                    bound = true;
                }
                else if (name.Contains("senior") || name.Contains("entry"))
                {
                    int idx = (int)SceneIndexes.ENTRY_LEVEL;
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => LoadGameByName(idx.ToString()));
                    bound = true;
                }
                else if (name.Contains("quit") || name.Contains("exit"))
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => Application.Quit());
                    bound = true;
                }

                if (bound)
                {
                    Debug.Log($"Rebound main-menu button '{btn.gameObject.name}' at runtime.");
                }
            }
        }
    }
}
