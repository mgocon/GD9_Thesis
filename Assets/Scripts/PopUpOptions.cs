using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class PopUpOptions : MonoBehaviour
{
    [Header("Options Popup")]
    public RectTransform optionsBox;
    public Button toggleButton;

    [Header("Microphone Dropdown")]
    public TMP_Dropdown microphoneDropdown;
    public VoiceProcessor voiceProcessor;

    [Header("Refresh")]
    public Button refreshButton;

    [Header("Music Volume")]
    public Slider musicVolumeSlider;
    public TMP_Text volumePercentageText; // Optional: displays percentage

    [Header("Slide Animation")]
    public float slideDistance = 200f;
    public float slideDuration = 0.25f;

    private Coroutine optionsCoroutine;
    private bool isVisible = false;

    private void Awake()
    {
        // Start hidden
        if (optionsBox != null)
            optionsBox.gameObject.SetActive(false);

        // Wire toggle button
        if (toggleButton != null)
        {
            toggleButton.onClick.RemoveAllListeners();
            toggleButton.onClick.AddListener(ToggleOptionsPopup);
        }

        // Wire dropdown
        if (microphoneDropdown != null)
        {
            microphoneDropdown.onValueChanged.RemoveAllListeners();
            microphoneDropdown.onValueChanged.AddListener(OnMicrophoneChanged);
        }

        // Wire refresh button
        if (refreshButton != null)
        {
            refreshButton.onClick.RemoveAllListeners();
            refreshButton.onClick.AddListener(RefreshDeviceList);
        }

        // Wire music volume slider
        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.onValueChanged.RemoveAllListeners();
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            musicVolumeSlider.minValue = 0f;
            musicVolumeSlider.maxValue = 1f;
        }

        // Try to auto-assign voiceProcessor from the persistent scene / other active scenes
        TryAutoAssignVoiceProcessor();

        // Listen for scene loads in case the persistent scene is loaded after this
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Attempt to find the VoiceProcessor when scenes change
        TryAutoAssignVoiceProcessor();
    }

    private void TryAutoAssignVoiceProcessor()
    {
        if (voiceProcessor != null)
            return;

        // Look for an existing VoiceProcessor in the loaded scenes
        voiceProcessor = FindObjectOfType<VoiceProcessor>();
        if (voiceProcessor != null)
        {
            PopulateMicrophoneDropdown();
        }
    }

    private void Start()
    {
        // Populate dropdown with available devices
        PopulateMicrophoneDropdown();
        
        // Load previously saved device if available
        LoadSavedDevice();

        // Load music volume
        LoadMusicVolume();
    }

    private void LoadMusicVolume()
    {
        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.value = MusicVolumeController.Instance.CurrentVolume;
            UpdateVolumeText(MusicVolumeController.Instance.CurrentVolume);
        }
    }

    private void OnMusicVolumeChanged(float volume)
    {
        MusicVolumeController.Instance.CurrentVolume = volume;
        UpdateVolumeText(volume);
    }

    private void UpdateVolumeText(float volume)
    {
        if (volumePercentageText != null)
        {
            volumePercentageText.text = Mathf.RoundToInt(volume * 100f) + "%";
        }
    }

    private void LoadSavedDevice()
    {
        if (voiceProcessor == null || voiceProcessor.Devices == null || voiceProcessor.Devices.Count == 0)
            return;

        // Check if there's a saved device preference
        if (PlayerPrefs.HasKey("SelectedMicrophoneIndex"))
        {
            int savedIndex = PlayerPrefs.GetInt("SelectedMicrophoneIndex");
            string savedName = PlayerPrefs.GetString("SelectedMicrophoneName", "");
            
            // Verify the saved device still exists
            if (savedIndex >= 0 && savedIndex < voiceProcessor.Devices.Count && 
                voiceProcessor.Devices[savedIndex] == savedName)
            {
                voiceProcessor.ChangeDevice(savedIndex);
                microphoneDropdown.value = savedIndex;
                microphoneDropdown.RefreshShownValue();
                Debug.Log($"Loaded saved microphone: {savedName}");
            }
        }
    }

    private void PopulateMicrophoneDropdown()
    {
        if (microphoneDropdown == null)
            return;

        if (voiceProcessor == null)
        {
            // No voice processor available yet — show placeholder and disable interaction
            microphoneDropdown.ClearOptions();
            microphoneDropdown.AddOptions(new List<string> { "No Microphone Available" });
            microphoneDropdown.interactable = false;
            microphoneDropdown.RefreshShownValue();
            return;
        }

        // Update devices list on the voice processor
        voiceProcessor.UpdateDevices();

        // Clear existing options
        microphoneDropdown.ClearOptions();

        // Add device names to dropdown
        if (voiceProcessor.Devices != null && voiceProcessor.Devices.Count > 0)
        {
            microphoneDropdown.AddOptions(new List<string>(voiceProcessor.Devices));
            microphoneDropdown.interactable = true;

            // Ensure CurrentDeviceIndex is valid
            int index = voiceProcessor.CurrentDeviceIndex;
            if (index < 0 || index >= microphoneDropdown.options.Count)
                index = 0;

            microphoneDropdown.value = index;
            microphoneDropdown.RefreshShownValue();
        }
        else
        {
            // No devices available
            List<string> noDeviceOption = new List<string> { "No Microphone Detected" };
            microphoneDropdown.AddOptions(noDeviceOption);
            microphoneDropdown.interactable = false;
            microphoneDropdown.RefreshShownValue();
        }
    }

    private void OnMicrophoneChanged(int deviceIndex)
    {
        if (voiceProcessor != null && voiceProcessor.Devices != null && deviceIndex >= 0 && deviceIndex < voiceProcessor.Devices.Count)
        {
            string deviceName = voiceProcessor.Devices[deviceIndex];
            Debug.Log($"Microphone changed to: {deviceName}");
            voiceProcessor.ChangeDevice(deviceIndex);
            
            // Save the device selection to PlayerPrefs for persistence across sessions
            PlayerPrefs.SetInt("SelectedMicrophoneIndex", deviceIndex);
            PlayerPrefs.SetString("SelectedMicrophoneName", deviceName);
            PlayerPrefs.Save();
            
            Debug.Log($"Device changed to: {deviceName} (Index: {deviceIndex})");
        }
    }

    public void ToggleOptionsPopup()
    {
        if (optionsBox == null) return;

        if (isVisible)
            HideOptionsPopup();
        else
            ShowOptionsPopup();
    }

    public void ShowOptionsPopup()
    {
        if (optionsBox == null) return;

        // Refresh device list when showing options
        PopulateMicrophoneDropdown();

        if (optionsCoroutine != null)
        {
            StopCoroutine(optionsCoroutine);
            optionsCoroutine = null;
        }

        optionsCoroutine = StartCoroutine(SlideBox(optionsBox, true, Vector2.up, slideDistance, slideDuration, () =>
        {
            optionsCoroutine = null;
            isVisible = true;
        }));
    }

    public void HideOptionsPopup()
    {
        if (optionsBox == null) return;

        if (optionsCoroutine != null)
        {
            StopCoroutine(optionsCoroutine);
            optionsCoroutine = null;
        }

        optionsCoroutine = StartCoroutine(SlideBox(optionsBox, false, Vector2.up, slideDistance, slideDuration, () =>
        {
            optionsCoroutine = null;
            isVisible = false;
            optionsBox.gameObject.SetActive(false);
        }));
    }

    private IEnumerator SlideBox(RectTransform box, bool show, Vector2 direction, float distance, float duration, Action onComplete)
    {
        if (box == null)
        {
            onComplete?.Invoke();
            yield break;
        }

        Vector2 start = box.anchoredPosition;
        Vector2 end = start + (show ? direction * distance : -direction * distance);
        float elapsed = 0f;

        if (show) box.gameObject.SetActive(true);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            box.anchoredPosition = Vector2.Lerp(start, end, t);
            yield return null;
        }

        box.anchoredPosition = end;
        onComplete?.Invoke();
    }

    // Refresh device list and repopulate dropdown (also callable from UI)
    public void RefreshDeviceList()
    {
        Debug.Log("Refreshing device list...");
        
        if (voiceProcessor == null)
            TryAutoAssignVoiceProcessor();

        if (voiceProcessor != null)
        {
            // Store currently selected device name
            string currentDeviceName = "";
            if (voiceProcessor.Devices != null && voiceProcessor.CurrentDeviceIndex >= 0 && 
                voiceProcessor.CurrentDeviceIndex < voiceProcessor.Devices.Count)
            {
                currentDeviceName = voiceProcessor.Devices[voiceProcessor.CurrentDeviceIndex];
            }

            // Repopulate the dropdown
            PopulateMicrophoneDropdown();

            // Try to maintain selection if the device still exists
            if (!string.IsNullOrEmpty(currentDeviceName) && voiceProcessor.Devices != null)
            {
                int newIndex = voiceProcessor.Devices.IndexOf(currentDeviceName);
                if (newIndex >= 0)
                {
                    microphoneDropdown.value = newIndex;
                    microphoneDropdown.RefreshShownValue();
                    Debug.Log($"Maintained selection: {currentDeviceName}");
                }
                else
                {
                    Debug.Log($"Previously selected device '{currentDeviceName}' is no longer available");
                }
            }

            Debug.Log($"Device list refreshed. Found {voiceProcessor.Devices.Count} device(s)");
        }
        else
        {
            Debug.LogWarning("VoiceProcessor not found. Cannot refresh device list.");
        }
    }
}