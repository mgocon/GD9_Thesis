using UnityEngine;
using UnityEngine.SceneManagement;

public class MusicVolumeController : MonoBehaviour
{
    private static MusicVolumeController _instance;
    public static MusicVolumeController Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<MusicVolumeController>();
            }
            return _instance;
        }
    }

    private const string VOLUME_PREF_KEY = "MusicVolume";
    private float _currentVolume = 1f;

    public float CurrentVolume
    {
        get { return _currentVolume; }
        set
        {
            _currentVolume = Mathf.Clamp01(value);
            ApplyVolumeToAllMusicSources();
            SaveVolume();
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this);
            return;
        }

        _instance = this;

        // Load saved volume
        LoadVolume();

        // Listen for scene changes
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Apply volume to any new music sources when a scene loads
        ApplyVolumeToAllMusicSources();
    }

    private void LoadVolume()
    {
        if (PlayerPrefs.HasKey(VOLUME_PREF_KEY))
        {
            _currentVolume = PlayerPrefs.GetFloat(VOLUME_PREF_KEY);
        }
        else
        {
            _currentVolume = 1f; // Default volume
        }

        ApplyVolumeToAllMusicSources();
    }

    private void SaveVolume()
    {
        PlayerPrefs.SetFloat(VOLUME_PREF_KEY, _currentVolume);
        PlayerPrefs.Save();
    }

    private void ApplyVolumeToAllMusicSources()
    {
        // Find all AudioSource components in the current scene
        AudioSource[] allAudioSources = FindObjectsOfType<AudioSource>();

        foreach (AudioSource audioSource in allAudioSources)
        {
            // Apply volume to audio sources (you can add filtering logic here if needed)
            if (IsMusicSource(audioSource))
            {
                audioSource.volume = _currentVolume;
            }
        }
    }

    /// <summary>
    /// Determines if an AudioSource is a music source
    /// </summary>
    private bool IsMusicSource(AudioSource audioSource)
    {
        // Option 1: Check by GameObject tag
        if (audioSource.gameObject.CompareTag("Music"))
            return true;

        // Option 2: Check by GameObject name (case-insensitive)
        string name = audioSource.gameObject.name.ToLower();
        if (name.Contains("music") || name.Contains("bgm") || name.Contains("background"))
            return true;

        // Option 3: Check by scene name (for your specific scenes)
        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName == "Main Menu" || sceneName == "Entry Level" || 
            sceneName == "Senior Level" || sceneName == "Tutorial")
        {
            // In these scenes, assume any looping AudioSource is music
            if (audioSource.loop)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Manually apply volume to a specific AudioSource
    /// </summary>
    public void ApplyVolumeToSource(AudioSource source)
    {
        if (source != null && IsMusicSource(source))
        {
            source.volume = _currentVolume;
        }
    }
}