using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

// ModelType enum needs to match FeedbackManager
public enum ModelType
{
    DQN,
    PPO
}

/// <summary>
/// Displays DQN and PPO feedback side-by-side for player comparison and selection
/// </summary>
public class FeedbackComparisonUI : MonoBehaviour
{
    [System.Serializable]
    public class FeedbackChoice
    {
        public ModelType chosenModel;
        public FeedbackMessage dqnFeedback;
        public FeedbackMessage ppoFeedback;
        public float responseTime; // Time taken to choose
    }

    [Header("Main Panel")]
    [SerializeField] private GameObject comparisonPanel;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI instructionText;
    [SerializeField] private TextMeshProUGUI overallQuestionScoreText; // Average of DQN and PPO overall scores
    
    [Header("DQN Feedback (Left)")]
    [SerializeField] private GameObject dqnPanel;
    [SerializeField] private TextMeshProUGUI dqnTitle;
    [SerializeField] private TextMeshProUGUI dqnMessage;
    [SerializeField] private TextMeshProUGUI dqnPerformanceText;
    [SerializeField] private Button chooseDQNButton;
    [SerializeField] private Slider dqnConfidenceBar;
    [SerializeField] private Slider dqnClarityBar;
    [SerializeField] private Slider dqnPaceBar;
    [SerializeField] private Slider dqnToneBar;
    [SerializeField] private Slider dqnOverallBar;
    
    [Header("DQN Slider Value Labels")]
    [SerializeField] private TextMeshProUGUI dqnConfidenceValue;
    [SerializeField] private TextMeshProUGUI dqnClarityValue;
    [SerializeField] private TextMeshProUGUI dqnPaceValue;
    [SerializeField] private TextMeshProUGUI dqnToneValue;
    [SerializeField] private TextMeshProUGUI dqnOverallValue;
    
    [Header("PPO Feedback (Right)")]
    [SerializeField] private GameObject ppoPanel;
    [SerializeField] private TextMeshProUGUI ppoTitle;
    [SerializeField] private TextMeshProUGUI ppoMessage;
    [SerializeField] private TextMeshProUGUI ppoPerformanceText;
    [SerializeField] private Button choosePPOButton;
    [SerializeField] private Slider ppoConfidenceBar;
    [SerializeField] private Slider ppoClarityBar;
    [SerializeField] private Slider ppoPaceBar;
    [SerializeField] private Slider ppoToneBar;
    [SerializeField] private Slider ppoOverallBar;

    [Header("PPO Slider Value Labels")]
    [SerializeField] private TextMeshProUGUI ppoConfidenceValue;
    [SerializeField] private TextMeshProUGUI ppoClarityValue;
    [SerializeField] private TextMeshProUGUI ppoPaceValue;
    [SerializeField] private TextMeshProUGUI ppoToneValue;
    [SerializeField] private TextMeshProUGUI ppoOverallValue;

    [Header("Visual Feedback")]
    [SerializeField] private Color excellentColor = new Color(0.2f, 0.8f, 0.2f);
    [SerializeField] private Color goodColor = new Color(1f, 0.8f, 0.2f);
    [SerializeField] private Color needsImprovementColor = new Color(0.9f, 0.3f, 0.3f);
    [SerializeField] private Color selectedColor = new Color(0.3f, 0.6f, 1f);
    [SerializeField] private float fadeInDuration = 0.5f;

    [Header("Events")]
    public UnityEvent<FeedbackChoice> OnFeedbackChosen;

    private FeedbackMessage currentDQNFeedback;
    private FeedbackMessage currentPPOFeedback;
    private float comparisonStartTime;
    private bool isDisplaying = false;

    // Public property to check if comparison is currently being displayed
    public bool IsDisplaying => isDisplaying;

    // Public methods to get current feedback messages
    public FeedbackMessage GetCurrentDQNFeedback() => currentDQNFeedback;
    public FeedbackMessage GetCurrentPPOFeedback() => currentPPOFeedback;

    private void Awake()
    {
        if (canvasGroup == null && comparisonPanel != null)
            canvasGroup = comparisonPanel.GetComponent<CanvasGroup>();

        if (canvasGroup == null && comparisonPanel != null)
            canvasGroup = comparisonPanel.AddComponent<CanvasGroup>();

        // Setup button listeners
        if (chooseDQNButton != null)
            chooseDQNButton.onClick.AddListener(() => OnChoiceMade(ModelType.DQN));

        if (choosePPOButton != null)
            choosePPOButton.onClick.AddListener(() => OnChoiceMade(ModelType.PPO));

        // Initially hide
        if (comparisonPanel != null)
            comparisonPanel.SetActive(false);
    }

    /// <summary>
    /// Show both DQN and PPO feedback for comparison
    /// </summary>
    public void ShowComparison(FeedbackMessage dqnFeedback, FeedbackMessage ppoFeedback)
    {
        if (isDisplaying)
        {
            StopAllCoroutines();
        }

        currentDQNFeedback = dqnFeedback;
        currentPPOFeedback = ppoFeedback;

        // Record DQN and PPO scores separately for end-of-game summary
        if (FeedbackManager.Instance != null)
        {
            FeedbackManager.Instance.RecordDQNScore(dqnFeedback.currentPerformance);
            FeedbackManager.Instance.RecordPPOScore(ppoFeedback.currentPerformance);
        }

        StartCoroutine(DisplayComparisonCoroutine());
    }

    private IEnumerator DisplayComparisonCoroutine()
    {
        isDisplaying = true;
        Debug.Log("🎯 FeedbackComparisonUI: isDisplaying = TRUE");
        comparisonStartTime = Time.time;

        // Set instruction text
        if (instructionText != null)
            instructionText.text = "Choose the feedback that would help you most:";

        // Populate DQN side
        PopulateFeedbackPanel(
            dqnTitle, dqnMessage, dqnPerformanceText,
            dqnConfidenceBar, dqnClarityBar, dqnPaceBar, dqnToneBar, dqnOverallBar,
            dqnConfidenceValue, dqnClarityValue, dqnPaceValue, dqnToneValue,
            currentDQNFeedback, "DQN"
        );

        // Populate PPO side
        PopulateFeedbackPanel(
            ppoTitle, ppoMessage, ppoPerformanceText,
            ppoConfidenceBar, ppoClarityBar, ppoPaceBar, ppoToneBar, ppoOverallBar,
            ppoConfidenceValue, ppoClarityValue, ppoPaceValue, ppoToneValue,
            currentPPOFeedback, "PPO"
        );

        // Calculate and display overall question score (average of DQN and PPO)
        if (overallQuestionScoreText != null && currentDQNFeedback != null && currentPPOFeedback != null)
        {
            float dqnOverall = currentDQNFeedback.currentPerformance.overall;
            float ppoOverall = currentPPOFeedback.currentPerformance.overall;
            float averageOverall = (dqnOverall + ppoOverall) / 2f;
            int percentage = Mathf.RoundToInt(averageOverall * 100f);
            
            Color scoreColor = GetScoreColor(averageOverall);
            string hexColor = ColorUtility.ToHtmlStringRGB(scoreColor);
            
            overallQuestionScoreText.text = $"<b>Overall Question Score:</b> <color=#{hexColor}>{percentage}%</color>";
        }

        // Enable buttons
        if (chooseDQNButton != null) chooseDQNButton.interactable = true;
        if (choosePPOButton != null) choosePPOButton.interactable = true;

        // Show panel with fade in
        if (comparisonPanel != null)
            comparisonPanel.SetActive(true);

        yield return StartCoroutine(FadeIn());
    }

    private void PopulateFeedbackPanel(
        TextMeshProUGUI title,
        TextMeshProUGUI message,
        TextMeshProUGUI performanceText,
        Slider confidenceBar, Slider clarityBar, Slider paceBar, Slider toneBar, Slider overallBar,
        TextMeshProUGUI confidenceValue, TextMeshProUGUI clarityValue, TextMeshProUGUI paceValue, TextMeshProUGUI toneValue,
        FeedbackMessage feedback,
        string modelName)
    {
        if (title != null)
            title.text = $"{modelName}";

        if (message != null)
            message.text = feedback.message;

        if (performanceText != null)
        {
            performanceText.text = $"Overall: {(feedback.currentPerformance.overall * 100):F0}%\n" +
                                   $"Confidence: {(feedback.confidence * 100):F0}%";
        }

        // Set performance bars with value labels (Overall doesn't need a label - it's shown in performanceText)
        UpdateSlider(confidenceBar, feedback.currentPerformance.confidence, confidenceValue);
        UpdateSlider(clarityBar, feedback.currentPerformance.clarity, clarityValue);
        UpdateSlider(paceBar, feedback.currentPerformance.pace, paceValue);
        UpdateSlider(toneBar, feedback.currentPerformance.tone, toneValue);
        UpdateSlider(overallBar, feedback.currentPerformance.overall);
    }

    private void UpdateSlider(Slider slider, float value, TextMeshProUGUI valueLabel = null)
    {
        if (slider == null) return;

        slider.value = value;
        
        // Update numerical value label
        if (valueLabel != null)
        {
            valueLabel.text = $"{(value * 100):F0}%";
        }
        
        // Color code based on value
        var fillImage = slider.fillRect?.GetComponent<Image>();
        if (fillImage != null)
        {
            if (value >= 0.7f)
                fillImage.color = excellentColor;
            else if (value >= 0.5f)
                fillImage.color = goodColor;
            else
                fillImage.color = needsImprovementColor;
        }
    }

    private void OnChoiceMade(ModelType chosenModel)
    {
        float responseTime = Time.time - comparisonStartTime;

        // Create choice record
        var choice = new FeedbackChoice
        {
            chosenModel = chosenModel,
            dqnFeedback = currentDQNFeedback,
            ppoFeedback = currentPPOFeedback,
            responseTime = responseTime
        };

        // Visual feedback - highlight chosen panel
        StartCoroutine(HighlightChoice(chosenModel));

        // Invoke event
        OnFeedbackChosen?.Invoke(choice);

        Debug.Log($"Player chose {chosenModel} feedback in {responseTime:F2}s");
    }

    private IEnumerator HighlightChoice(ModelType chosen)
    {
        // Disable buttons
        if (chooseDQNButton != null) chooseDQNButton.interactable = false;
        if (choosePPOButton != null) choosePPOButton.interactable = false;

        // Highlight chosen panel
        GameObject chosenPanel = (chosen == ModelType.DQN) ? dqnPanel : ppoPanel;
        var chosenImage = chosenPanel?.GetComponent<Image>();
        if (chosenImage != null)
        {
            chosenImage.color = selectedColor;
        }

        // Wait a moment
        yield return new WaitForSeconds(1.5f);

        // Fade out
        yield return StartCoroutine(FadeOut());

        // Clear the overall question score text
        if (overallQuestionScoreText != null)
            overallQuestionScoreText.text = "";

        // Hide panel
        if (comparisonPanel != null)
            comparisonPanel.SetActive(false);

        isDisplaying = false;
        Debug.Log("🎯 FeedbackComparisonUI: isDisplaying = FALSE (after fade)");
    }

    private IEnumerator FadeIn()
    {
        if (canvasGroup == null) yield break;

        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
            yield return null;
        }

        canvasGroup.alpha = 1f;
    }

    private IEnumerator FadeOut()
    {
        if (canvasGroup == null) yield break;

        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeInDuration);
            yield return null;
        }

        canvasGroup.alpha = 0f;
    }

    /// <summary>
    /// Get color based on score performance
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
    /// Manually close the comparison panel
    /// </summary>
    public void HideComparison()
    {
        if (isDisplaying)
        {
            StopAllCoroutines();
            
            // Clear the overall question score text
            if (overallQuestionScoreText != null)
                overallQuestionScoreText.text = "";
            
            if (comparisonPanel != null)
                comparisonPanel.SetActive(false);
            isDisplaying = false;
            Debug.Log("🎯 FeedbackComparisonUI: isDisplaying = FALSE (manual hide)");
        }
    }
}
