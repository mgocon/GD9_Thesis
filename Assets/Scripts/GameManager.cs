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

        // Keep GameManager alive across scene loads so we can safely use LoadSceneMode.Single
        DontDestroyOnLoad(gameObject);

        // Load main menu at startup (additive so persistent manager stays)
        SceneManager.LoadSceneAsync((int)SceneIndexes.MAIN_MENU, LoadSceneMode.Additive);
    }

    List<AsyncOperation> scenesLoading = new List<AsyncOperation>();

    // Backwards-compatible parameterless call: loads ENTRY_LEVEL
    // public void LoadGame()
    // {
    //     LoadGame(((int)SceneIndexes.ENTRY_LEVEL).ToString());
    // }

    // New API: load scene by name (requested from MenuManager)
    public void LoadGame(string sceneName)
    {
        if (loadingScreen != null) loadingScreen.gameObject.SetActive(true);
        // Load the requested scene additively but first unload any non-manager scenes
        // This preserves the entire persistent scene (index = MANAGER) and its GameObjects.
        scenesLoading.Clear();

        int targetIndex;
        bool targetIsIndex = int.TryParse(sceneName, out targetIndex);

        int managerIndex = (int)SceneIndexes.MANAGER;

        // Unload every loaded scene except the persistent manager and except the target scene (if already loaded)
        int loadedCount = SceneManager.sceneCount;
        for (int i = 0; i < loadedCount; i++)
        {
            var s = SceneManager.GetSceneAt(i);
            if (!s.isLoaded) continue;
            if (s.buildIndex == managerIndex) continue; // keep manager scene and its objects
            if (targetIsIndex && s.buildIndex == targetIndex) continue; // keep target if already loaded by index
            if (!targetIsIndex && s.name == sceneName) continue; // keep target if already loaded by name

            scenesLoading.Add(SceneManager.UnloadSceneAsync(s));
        }

        // Load target additively if not already loaded
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

    // Explicit helper to avoid ambiguous overload resolution from other callers
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

        // ✅ Wait for Vosk initialization
        VoskSpeechToText vosk = FindObjectOfType<VoskSpeechToText>();
        if (vosk != null)
        {
            while (!vosk.RecognizerReady)
            {
                yield return null;
            }
        }


        // Hide loading screen only when everything is ready
        // Cleanup duplicate EventSystems (keep the one in the persistent manager scene)
        EnsureSingleEventSystem();

        // Try to rebind common main-menu buttons if main menu was just (re)loaded
        TryRebindMainMenuButtons();

        loadingScreen.gameObject.SetActive(false);
    }

    // Ensure only one active EventSystem exists. Prefer the one in the manager scene.
    private void EnsureSingleEventSystem()
    {
        var systems = FindObjectsOfType<EventSystem>();
        if (systems == null || systems.Length <= 1) return;

        int managerIndex = (int)SceneIndexes.MANAGER;
        EventSystem keeper = null;

        // Prefer event system that belongs to manager scene
        foreach (var es in systems)
        {
            if (es.gameObject.scene.buildIndex == managerIndex)
            {
                keeper = es;
                break;
            }
        }

        // Otherwise just keep the first
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

    // Heuristic-based binding for main menu buttons that lost OnClick listeners.
    // Looks for Buttons in the main menu scene with no persistent listeners and binds common actions by name.
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
                // If it already has persistent listeners, skip
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
