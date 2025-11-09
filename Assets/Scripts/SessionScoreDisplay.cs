using UnityEngine;
using TMPro;

/// <summary>
/// Displays the overall session score based on all answered questions
/// Shows/hides automatically when feedback comparison appears/disappears
/// </summary>
public class SessionScoreDisplay : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI overallScoreText;
    [SerializeField] private TextMeshProUGUI questionCountText;
    [SerializeField] private TextMeshProUGUI detailsText;
    [SerializeField] private GameObject scorePanel;

    [Header("Display Settings")]
    [SerializeField] private bool autoUpdate = true;
    [SerializeField] private float updateInterval = 1f;
    [SerializeField] private bool showDetails = true;

    [Header("Score Colors")]
    [SerializeField] private Color excellentColor = new Color(0.2f, 0.8f, 0.2f);
    [SerializeField] private Color goodColor = new Color(1f, 0.8f, 0.2f);
    [SerializeField] private Color needsImprovementColor = new Color(0.9f, 0.3f, 0.3f);

    private FeedbackManager feedbackManager;
    private FeedbackComparisonUI feedbackComparisonUI;
    private float lastUpdateTime;

    private void Start()
    {
        feedbackManager = FeedbackManager.Instance;
        feedbackComparisonUI = FindObjectOfType<FeedbackComparisonUI>();
        
        Debug.Log($"📊 SessionScoreDisplay: Started. FeedbackManager={feedbackManager != null}, FeedbackComparisonUI={feedbackComparisonUI != null}");
        
        // If no scorePanel is assigned, use this GameObject
        if (scorePanel == null)
            scorePanel = this.gameObject;
        
        // Start hidden - will show when feedback comparison appears
        scorePanel.SetActive(false);
    }

    private void Update()
    {
        if (!autoUpdate || feedbackManager == null)
            return;

        // Check if feedback comparison is showing
        bool shouldShow = feedbackComparisonUI != null && feedbackComparisonUI.IsDisplaying;
        
        if (scorePanel != null && scorePanel.activeSelf != shouldShow)
        {
            scorePanel.SetActive(shouldShow);
            if (shouldShow)
            {
                Debug.Log($"📊 SessionScoreDisplay: Showing panel (Active={scorePanel.activeSelf})");
                UpdateScoreDisplay(); // Update immediately when showing
            }
            else
            {
                Debug.Log($"📊 SessionScoreDisplay: Hiding panel (Active={scorePanel.activeSelf})");
            }
        }
        else if (shouldShow)
        {
            // Panel should be visible - log its state
            Debug.Log($"📊 SessionScoreDisplay: Panel should show. IsDisplaying={feedbackComparisonUI.IsDisplaying}, PanelActive={scorePanel.activeSelf}");
        }

        // Update score periodically when visible
        if (shouldShow && Time.time - lastUpdateTime >= updateInterval)
        {
            UpdateScoreDisplay();
            lastUpdateTime = Time.time;
        }
    }

    public void ShowScore()
    {
        if (scorePanel != null)
            scorePanel.SetActive(true);
        UpdateScoreDisplay();
    }

    public void HideScore()
    {
        if (scorePanel != null)
            scorePanel.SetActive(false);
    }

    public void UpdateScoreDisplay()
    {
        if (feedbackManager == null)
            return;

        var breakdown = feedbackManager.GetSessionScoreBreakdown();
        float avgOverall = breakdown.avgOverall;
        int questionCount = breakdown.questionCount;

        // Update overall score
        if (overallScoreText != null)
        {
            if (questionCount > 0)
            {
                overallScoreText.text = GetColoredScore(avgOverall);
            }
            else
            {
                overallScoreText.text = "<color=#AAAAAA>--</color>";
            }
        }

        // Update question count
        if (questionCountText != null)
        {
            questionCountText.text = questionCount > 0 ? $"Based on {questionCount} question(s)" : "No questions answered yet";
        }

        // Update details breakdown
        if (detailsText != null && showDetails && questionCount > 0)
        {
            detailsText.text = $"Confidence: {GetColoredScore(breakdown.avgConfidence)}\n" +
                              $"Clarity: {GetColoredScore(breakdown.avgClarity)}\n" +
                              $"Pace: {GetColoredScore(breakdown.avgPace)}\n" +
                              $"Tone: {GetColoredScore(breakdown.avgTone)}";
        }
        else if (detailsText != null)
        {
            detailsText.text = "";
        }
    }

    private string GetColoredScore(float score)
    {
        int percentage = Mathf.RoundToInt(score * 100f);
        Color color = GetScoreColor(score);
        string hexColor = ColorUtility.ToHtmlStringRGB(color);
        return $"<color=#{hexColor}>{percentage}%</color>";
    }

    private Color GetScoreColor(float score)
    {
        if (score >= 0.7f)
            return excellentColor;
        else if (score >= 0.5f)
            return goodColor;
        else
            return needsImprovementColor;
    }

    public void ToggleDetailsDisplay()
    {
        showDetails = !showDetails;
        UpdateScoreDisplay();
    }

    public void TogglePanel()
    {
        if (scorePanel != null)
            scorePanel.SetActive(!scorePanel.activeSelf);
    }
}
