using UnityEngine;
using TMPro;

/// <summary>
/// Displays algorithm speed comparison for DQN vs PPO
/// Shows/hides automatically when feedback comparison appears/disappears
/// </summary>
public class SpeedTrackerUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI dqnSpeedText;
    [SerializeField] private TextMeshProUGUI ppoSpeedText;
    [SerializeField] private TextMeshProUGUI comparisonText;
    [SerializeField] private GameObject speedPanel;

    [Header("Settings")]
    [SerializeField] private bool updateRealtime = true;
    [SerializeField] private float updateInterval = 0.5f;

    private FeedbackManager feedbackManager;
    private FeedbackComparisonUI feedbackComparisonUI;
    private float lastUpdateTime;

    private void Start()
    {
        feedbackManager = FeedbackManager.Instance;
        feedbackComparisonUI = FindObjectOfType<FeedbackComparisonUI>();
        
        Debug.Log($"⚡ SpeedTrackerUI: Started. FeedbackManager={feedbackManager != null}, FeedbackComparisonUI={feedbackComparisonUI != null}");
        
        // If no speedPanel is assigned, use this GameObject
        if (speedPanel == null)
            speedPanel = this.gameObject;
        
        // Start hidden - will show when feedback comparison appears
        speedPanel.SetActive(false);
    }

    private void Update()
    {
        if (!updateRealtime || feedbackManager == null)
            return;

        // Check if feedback comparison is showing
        bool shouldShow = feedbackComparisonUI != null && feedbackComparisonUI.IsDisplaying;
        
        if (speedPanel != null && speedPanel.activeSelf != shouldShow)
        {
            speedPanel.SetActive(shouldShow);
            if (shouldShow)
            {
                Debug.Log("⚡ SpeedTrackerUI: Showing panel");
                UpdateSpeedDisplay(); // Update immediately when showing
            }
            else
            {
                Debug.Log("⚡ SpeedTrackerUI: Hiding panel");
            }
        }
        else if (speedPanel == null && gameObject.activeSelf != shouldShow)
        {
            gameObject.SetActive(shouldShow);
            if (shouldShow)
            {
                Debug.Log("⚡ SpeedTrackerUI: Showing GameObject");
                UpdateSpeedDisplay();
            }
        }

        // Update speed display periodically when visible
        if (shouldShow && Time.time - lastUpdateTime >= updateInterval)
        {
            UpdateSpeedDisplay();
            lastUpdateTime = Time.time;
        }
    }

    public void ShowSpeed()
    {
        if (speedPanel != null)
            speedPanel.SetActive(true);
        else
            gameObject.SetActive(true);
        UpdateSpeedDisplay();
    }

    public void HideSpeed()
    {
        if (speedPanel != null)
            speedPanel.SetActive(false);
        else
            gameObject.SetActive(false);
    }

    public void UpdateSpeedDisplay()
    {
        if (feedbackManager == null)
            return;

        var stats = feedbackManager.GetSpeedStats();
        float dqnAvg = stats.dqnAvg;
        float ppoAvg = stats.ppoAvg;
        float dqnLast = stats.dqnLast;
        float ppoLast = stats.ppoLast;
        int dqnCount = stats.dqnCount;
        int ppoCount = stats.ppoCount;

        // Update DQN speed
        if (dqnSpeedText != null)
        {
            dqnSpeedText.text = $"<b>DQN Speed</b>\n" +
                               $"Last: <color=#FFD700>{dqnLast:F2}ms</color>\n" +
                               $"Avg: {dqnAvg:F2}ms\n" +
                               $"Runs: {dqnCount}";
        }

        // Update PPO speed
        if (ppoSpeedText != null)
        {
            ppoSpeedText.text = $"<b>PPO Speed</b>\n" +
                               $"Last: <color=#FFD700>{ppoLast:F2}ms</color>\n" +
                               $"Avg: {ppoAvg:F2}ms\n" +
                               $"Runs: {ppoCount}";
        }

        // Update comparison
        if (comparisonText != null && dqnCount > 0 && ppoCount > 0)
        {
            string faster = dqnAvg < ppoAvg ? "DQN" : "PPO";
            float difference = Mathf.Abs(dqnAvg - ppoAvg);
            float percentDiff = (difference / Mathf.Max(dqnAvg, ppoAvg)) * 100f;

            comparisonText.text = $"<b>{faster}</b> is <color=#00FF00>{percentDiff:F1}%</color> faster";
        }
    }

    public void TogglePanel()
    {
        bool isActive = speedPanel != null ? speedPanel.activeSelf : gameObject.activeSelf;
        
        if (speedPanel != null)
            speedPanel.SetActive(!isActive);
        else
            gameObject.SetActive(!isActive);
    }
}
