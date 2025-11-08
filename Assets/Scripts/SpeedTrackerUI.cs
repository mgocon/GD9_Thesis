using UnityEngine;
using TMPro;

/// <summary>
/// Displays algorithm speed comparison for DQN vs PPO
/// </summary>
public class SpeedTrackerUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI dqnSpeedText;
    [SerializeField] private TextMeshProUGUI ppoSpeedText;
    [SerializeField] private TextMeshProUGUI comparisonText;
    [SerializeField] private GameObject speedPanel;

    [Header("Settings")]
    [SerializeField] private bool showOnStart = false;
    [SerializeField] private bool updateRealtime = true;
    [SerializeField] private float updateInterval = 0.5f;

    private FeedbackManager feedbackManager;
    private float lastUpdateTime;

    private void Start()
    {
        feedbackManager = FeedbackManager.Instance;
        
        if (speedPanel != null)
            speedPanel.SetActive(showOnStart);
        else
            gameObject.SetActive(showOnStart);
    }

    private void Update()
    {
        if (!updateRealtime || feedbackManager == null)
            return;

        if (Time.time - lastUpdateTime >= updateInterval)
        {
            UpdateSpeedDisplay();
            lastUpdateTime = Time.time;
        }
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
