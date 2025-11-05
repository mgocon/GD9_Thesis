using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages AI feedback generation for interview responses
/// Currently using rule-based feedback system
/// </summary>
public class FeedbackManager : MonoBehaviour
{
    public static FeedbackManager Instance { get; private set; }

    [Header("Feedback System")]
    [SerializeField] private bool verboseLogging = false;

    public enum ModelType
    {
        DQN,
        PPO
    }

    [Header("Model Configuration")]
    [SerializeField] private ModelType currentModelType = ModelType.DQN;

    // Public property to access current model type
    public ModelType CurrentModelType => currentModelType;

    [Header("Components")]
    [SerializeField] private VoiceAnalyzer voiceAnalyzer;

    // Note: FeedbackUI removed - using FeedbackComparisonUI in BottomBarController instead

    [Header("Performance Tracking")]
    [SerializeField] private bool trackPerformance = true;
    [SerializeField] private int maxHistorySize = 8;

    // Performance history
    private List<InterviewPerformance> performanceHistory = new List<InterviewPerformance>();
    private List<FeedbackAction> feedbackHistory = new List<FeedbackAction>();

    // Current session tracking
    private int currentQuestionIndex = 0;
    private InterviewPerformance lastPerformance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (voiceAnalyzer == null)
            voiceAnalyzer = FindObjectOfType<VoiceAnalyzer>();

        if (voiceAnalyzer == null)
        {
            voiceAnalyzer = gameObject.AddComponent<VoiceAnalyzer>();
        }
    }

    private void Start()
    {
        lastPerformance = new InterviewPerformance
        {
            confidence = 0.5f,
            clarity = 0.5f,
            pace = 0.5f,
            tone = 0.5f,
            overall = 0.5f
        };
    }

    public void SetModelType(ModelType modelType)
    {
        currentModelType = modelType;
        if (verboseLogging)
        {
            Debug.Log($"🔄 Switched to {modelType} model");
        }
    }

    /// <summary>
    /// Generate feedback based on voice analysis
    /// </summary>
    public FeedbackMessage GenerateFeedback(string transcribedText, float duration)
    {
        // Analyze voice/text performance
        InterviewPerformance currentPerformance = voiceAnalyzer != null 
            ? voiceAnalyzer.AnalyzeResponse(transcribedText, duration)
            : new InterviewPerformance { overall = 0.5f };

        // Rule-based feedback logic
        FeedbackAction action = GetRuleBasedFeedback(currentPerformance);
        float confidence = 0.7f; // Simulated confidence

        // Predict performance improvement
        InterviewPerformance expectedImprovement = PredictImprovement(action, currentPerformance);

        // Create feedback message
        FeedbackMessage feedback = FeedbackMessage.Create(action, confidence, currentPerformance, expectedImprovement);

        // Update history
        if (trackPerformance)
        {
            UpdateHistory(currentPerformance, action);
        }

        lastPerformance = currentPerformance;

        if (verboseLogging)
        {
            Debug.Log($"📊 Feedback Generated ({currentModelType}): {action}");
        }

        return feedback;
    }

    /// <summary>
    /// <summary>
    /// Rule-based feedback with DQN vs PPO strategy differences
    /// Now with expanded feedback types!
    /// </summary>
    private FeedbackAction GetRuleBasedFeedback(InterviewPerformance performance)
    {
        // DQN: Conservative, fix biggest weakness first
        // PPO: Holistic, optimize overall performance
        
        bool isDQN = (currentModelType == ModelType.DQN);
        float overall = performance.overall;
        
        // Excellent performance - celebrate it!
        if (overall >= 0.9f)
            return FeedbackAction.ExcellentPerformance;
        
        if (isDQN)
        {
            // DQN: Target weakest metric directly
            if (performance.confidence < 0.4f)
                return FeedbackAction.EncourageConfidence;
            else if (performance.clarity < 0.45f)
                return FeedbackAction.StructureAnswersBetter;
            else if (performance.pace < 0.3f)
                return FeedbackAction.ImproveSpeechPace;
            else if (performance.pace > 0.75f)
                return FeedbackAction.BeMoreConcise;
            else if (performance.tone < 0.45f)
                return FeedbackAction.ShowMoreEnthusiasm;
            else if (performance.confidence < 0.55f)
                return FeedbackAction.HighlightAchievements;
            else if (performance.clarity < 0.6f)
                return FeedbackAction.AddMoreDetails;
            else if (overall < 0.65f)
                return FeedbackAction.ReduceNervousness;
            else if (performance.tone < 0.65f)
                return FeedbackAction.BuildRapport;
            else
                return FeedbackAction.MaintainCurrentApproach;
        }
        else
        {
            // PPO: More creative, considers interactions and soft skills
            if (overall < 0.5f)
                return FeedbackAction.ReduceNervousness;
            else if (performance.confidence < 0.45f && performance.tone < 0.5f)
                return FeedbackAction.ImproveBodyLanguage;
            else if (performance.confidence < 0.5f)
                return FeedbackAction.DemonstrateLeadership;
            else if (performance.pace > 0.75f)
                return FeedbackAction.BeMoreConcise;
            else if (performance.pace < 0.35f)
                return FeedbackAction.ImproveSpeechPace;
            else if (performance.clarity < 0.5f)
                return FeedbackAction.AddMoreDetails;
            else if (performance.tone < 0.5f)
                return FeedbackAction.ShowMoreEnthusiasm;
            else if (overall < 0.7f && performance.confidence > 0.6f)
                return FeedbackAction.ExpressCuriosity;
            else if (performance.clarity > 0.7f && performance.confidence > 0.65f)
                return FeedbackAction.ShowProblemSolving;
            else if (performance.tone < 0.65f)
                return FeedbackAction.MatchInterviewerEnergy;
            else if (performance.overall < 0.75f)
                return FeedbackAction.ListenMoreActively;
            else
                return FeedbackAction.MaintainCurrentApproach;
        }
    }

    private InterviewPerformance PredictImprovement(FeedbackAction action, InterviewPerformance current)
    {
        var improvement = new InterviewPerformance();
        float baseImprovement = 0.08f;

        switch (action)
        {
            case FeedbackAction.EncourageConfidence:
                improvement.confidence = baseImprovement * 1.2f;
                improvement.overall = baseImprovement * 0.6f;
                break;

            case FeedbackAction.ImproveSpeechPace:
                improvement.pace = baseImprovement * 1.0f;
                improvement.clarity = baseImprovement * 0.4f;
                improvement.overall = baseImprovement * 0.5f;
                break;

            case FeedbackAction.EnhanceClarity:
                improvement.clarity = baseImprovement * 1.1f;
                improvement.overall = baseImprovement * 0.7f;
                break;

            case FeedbackAction.OptimizeTone:
                improvement.tone = baseImprovement * 1.0f;
                improvement.confidence = baseImprovement * 0.3f;
                improvement.overall = baseImprovement * 0.5f;
                break;

            case FeedbackAction.ReduceNervousness:
                improvement.confidence = baseImprovement * 0.9f;
                improvement.tone = baseImprovement * 0.6f;
                improvement.pace = baseImprovement * 0.4f;
                improvement.overall = baseImprovement * 0.6f;
                break;

            case FeedbackAction.MaintainCurrentApproach:
                improvement.overall = baseImprovement * 0.1f;
                break;
        }

        return improvement;
    }

    private void UpdateHistory(InterviewPerformance performance, FeedbackAction action)
    {
        performanceHistory.Add(performance);
        feedbackHistory.Add(action);

        if (performanceHistory.Count > maxHistorySize)
        {
            performanceHistory.RemoveAt(0);
            feedbackHistory.RemoveAt(0);
        }
    }

    public (float averageScore, float improvement) GetSessionStats()
    {
        if (performanceHistory.Count == 0)
            return (0.5f, 0f);

        float sum = 0f;
        foreach (var perf in performanceHistory)
            sum += perf.overall;

        float average = sum / performanceHistory.Count;
        
        float improvement = 0f;
        if (performanceHistory.Count > 1)
        {
            improvement = performanceHistory[performanceHistory.Count - 1].overall - 
                         performanceHistory[0].overall;
        }

        return (average, improvement);
    }

    public void ResetSession()
    {
        performanceHistory.Clear();
        feedbackHistory.Clear();
        currentQuestionIndex = 0;
        lastPerformance = new InterviewPerformance { overall = 0.5f };
        
        Debug.Log("🔄 Feedback session reset");
    }
}
