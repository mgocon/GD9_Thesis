using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Displays a comprehensive score breakdown and summary at the end of the game.
/// Shows all individual question scores and overall statistics.
/// </summary>
public class GameSummaryScreen : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject summaryPanel;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI overallScoreText;
    [SerializeField] private TextMeshProUGUI totalQuestionsText;
    [SerializeField] private TextMeshProUGUI averageBreakdownText;
    [SerializeField] private Transform questionListContainer;
    [SerializeField] private GameObject questionScoreItemPrefab;
    [SerializeField] private Button closeButton;

    [Header("Score Colors")]
    [SerializeField] private Color excellentColor = new Color(0.2f, 0.8f, 0.2f);
    [SerializeField] private Color goodColor = new Color(1f, 0.8f, 0.2f);
    [SerializeField] private Color needsImprovementColor = new Color(0.9f, 0.3f, 0.3f);

    private FeedbackManager feedbackManager;
    private List<GameObject> questionItems = new List<GameObject>();

    private void Awake()
    {
        // Initialize even if GameObject is inactive
        if (closeButton != null)
            closeButton.onClick.AddListener(HideSummary);
    }

    private void Start()
    {
        feedbackManager = FeedbackManager.Instance;
        
        if (summaryPanel != null)
            summaryPanel.SetActive(false);
    }

    /// <summary>
    /// Show the summary screen with all scores
    /// </summary>
    public void ShowSummary()
    {
        Debug.Log("=== GameSummaryScreen.ShowSummary() called ===");
        
        // Get FeedbackManager instance (handles persistent scene case)
        if (feedbackManager == null)
        {
            Debug.Log("FeedbackManager was null, trying to get Instance...");
            
            // First try the singleton Instance
            feedbackManager = FeedbackManager.Instance;
            
            // If Instance is null, try FindObjectOfType (works across all loaded scenes including persistent)
            if (feedbackManager == null)
            {
                Debug.Log("FeedbackManager.Instance is null, searching all scenes...");
                feedbackManager = FindObjectOfType<FeedbackManager>(true); // true includes inactive objects
            }
        }
        
        if (feedbackManager == null)
        {
            Debug.LogError("Could not find FeedbackManager! Make sure the Persistent scene is loaded.");
            
            // Show a helpful message to the player
            if (titleText != null)
                titleText.text = "<b>Error: FeedbackManager Not Found</b>";
            if (overallScoreText != null)
                overallScoreText.text = "Please start the game from the Main Menu";
            if (summaryPanel != null)
                summaryPanel.SetActive(true);
            
            return;
        }
        else
        {
            Debug.Log("FeedbackManager found successfully!");
        }

        // Clear previous items
        ClearQuestionItems();

        // Get session data
        var breakdown = feedbackManager.GetSessionScoreBreakdown();
        Debug.Log($"Session breakdown: Questions={breakdown.questionCount}, Overall={breakdown.avgOverall:F2}");
        
        // Update title
        if (titleText != null)
        {
            titleText.text = "<b>Game Summary</b>";
            Debug.Log($"Title set: {titleText.text}");
        }
        else
        {
            Debug.LogWarning("titleText is NULL!");
        }

        // Update overall score
        if (overallScoreText != null)
        {
            if (breakdown.questionCount > 0)
            {
                string scoreText = GetColoredScore(breakdown.avgOverall, large: true);
                overallScoreText.text = $"<b>Overall Performance:</b>\n{scoreText}";
                Debug.Log($"Overall score set: {overallScoreText.text}");
            }
            else
            {
                overallScoreText.text = "<b>Overall Performance:</b>\n<color=#AAAAAA>No data</color>";
                Debug.Log("No data for overall score");
            }
        }
        else
        {
            Debug.LogWarning("overallScoreText is NULL!");
        }

        // Update total questions
        if (totalQuestionsText != null)
        {
            totalQuestionsText.text = $"<b>Questions Answered:</b> {breakdown.questionCount}";
            Debug.Log($"Total questions set: {totalQuestionsText.text}");
        }
        else
        {
            Debug.LogWarning("totalQuestionsText is NULL!");
        }

        // Update average breakdown
        if (averageBreakdownText != null && breakdown.questionCount > 0)
        {
            // Get DQN and PPO breakdowns
            var dqnBreakdown = feedbackManager.GetDQNScoreBreakdown();
            var ppoBreakdown = feedbackManager.GetPPOScoreBreakdown();
            
            Debug.Log($"Feedback B breakdown: Questions={dqnBreakdown.questionCount}, Overall={dqnBreakdown.avgOverall:F2}");
            Debug.Log($"Feedback A breakdown: Questions={ppoBreakdown.questionCount}, Overall={ppoBreakdown.avgOverall:F2}");

            string breakdownText = "<b>Average Performance Breakdown:</b>\n\n";
            
            // Overall average (from selected feedback)
            breakdownText += $"<b>Your Selected Feedback:</b>\n";
            breakdownText += $"  Overall: {GetColoredScore(breakdown.avgOverall)}\n";
            breakdownText += $"  Confidence: {GetColoredScore(breakdown.avgConfidence)}\n";
            breakdownText += $"  Clarity: {GetColoredScore(breakdown.avgClarity)}\n";
            breakdownText += $"  Pace: {GetColoredScore(breakdown.avgPace)}\n";
            breakdownText += $"  Tone: {GetColoredScore(breakdown.avgTone)}\n\n";

            // DQN average
            if (dqnBreakdown.questionCount > 0)
            {
                breakdownText += $"<b>Feedback B Algorithm Scores:</b>\n";
                breakdownText += $"  Overall: {GetColoredScore(dqnBreakdown.avgOverall)}\n";
                breakdownText += $"  Confidence: {GetColoredScore(dqnBreakdown.avgConfidence)}\n";
                breakdownText += $"  Clarity: {GetColoredScore(dqnBreakdown.avgClarity)}\n";
                breakdownText += $"  Pace: {GetColoredScore(dqnBreakdown.avgPace)}\n";
                breakdownText += $"  Tone: {GetColoredScore(dqnBreakdown.avgTone)}\n\n";
            }

            // PPO average
            if (ppoBreakdown.questionCount > 0)
            {
                breakdownText += $"<b>Feedback A Algorithm Scores:</b>\n";
                breakdownText += $"  Overall: {GetColoredScore(ppoBreakdown.avgOverall)}\n";
                breakdownText += $"  Confidence: {GetColoredScore(ppoBreakdown.avgConfidence)}\n";
                breakdownText += $"  Clarity: {GetColoredScore(ppoBreakdown.avgClarity)}\n";
                breakdownText += $"  Pace: {GetColoredScore(ppoBreakdown.avgPace)}\n";
                breakdownText += $"  Tone: {GetColoredScore(ppoBreakdown.avgTone)}";
            }

            averageBreakdownText.text = breakdownText;
            Debug.Log($"Breakdown text set ({breakdownText.Length} chars):\n{breakdownText}");
        }
        else if (averageBreakdownText != null)
        {
            averageBreakdownText.text = "";
            Debug.Log("Breakdown text cleared (no data)");
        }
        else
        {
            Debug.LogWarning("averageBreakdownText is NULL!");
        }

        // TODO: Add individual question scores if FeedbackManager tracks them
        // For now, we show the summary stats

        // Show panel
        if (summaryPanel != null)
        {
            summaryPanel.SetActive(true);
            Debug.Log("Summary panel activated!");
        }
        else
        {
            Debug.LogWarning("summaryPanel is NULL!");
        }
    }

    /// <summary>
    /// Hide the summary screen
    /// </summary>
    public void HideSummary()
    {
        if (summaryPanel != null)
            summaryPanel.SetActive(false);
    }

    /// <summary>
    /// Create a question score item (for individual question breakdown)
    /// </summary>
    private void CreateQuestionScoreItem(int questionNumber, float score, InterviewPerformance performance)
    {
        if (questionScoreItemPrefab == null || questionListContainer == null)
            return;

        GameObject item = Instantiate(questionScoreItemPrefab, questionListContainer);
        questionItems.Add(item);

        // Find text components in the item
        TextMeshProUGUI[] texts = item.GetComponentsInChildren<TextMeshProUGUI>();
        if (texts.Length > 0)
        {
            // First text: Question number and overall score
            texts[0].text = $"<b>Question {questionNumber}:</b> {GetColoredScore(score)}";
            
            // Second text (if exists): Detailed breakdown
            if (texts.Length > 1)
            {
                texts[1].text = $"Confidence: {GetColoredScore(performance.confidence)} | " +
                              $"Clarity: {GetColoredScore(performance.clarity)} | " +
                              $"Pace: {GetColoredScore(performance.pace)} | " +
                              $"Tone: {GetColoredScore(performance.tone)}";
            }
        }
    }

    /// <summary>
    /// Clear all question score items
    /// </summary>
    private void ClearQuestionItems()
    {
        foreach (var item in questionItems)
        {
            if (item != null)
                Destroy(item);
        }
        questionItems.Clear();
    }

    /// <summary>
    /// Get colored score string
    /// </summary>
    private string GetColoredScore(float score, bool large = false)
    {
        int percentage = Mathf.RoundToInt(score * 100f);
        Color color = GetScoreColor(score);
        string hexColor = ColorUtility.ToHtmlStringRGB(color);
        
        if (large)
            return $"<size=150%><color=#{hexColor}><b>{percentage}%</b></color></size>";
        else
            return $"<color=#{hexColor}>{percentage}%</color>";
    }

    /// <summary>
    /// Get color based on score
    /// </summary>
    private Color GetScoreColor(float score)
    {
        if (score >= 0.7f)
            return excellentColor;
        else if (score >= 0.5f)
            return goodColor;
        else
            return needsImprovementColor;
    }

    /// <summary>
    /// Toggle summary panel
    /// </summary>
    public void ToggleSummary()
    {
        if (summaryPanel != null)
        {
            if (summaryPanel.activeSelf)
                HideSummary();
            else
                ShowSummary();
        }
    }
}
