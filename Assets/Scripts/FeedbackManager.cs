using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Manages AI feedback generation for interview responses
/// Supports both ML inference (native neural network) and rule-based fallback
/// </summary>
public class FeedbackManager : MonoBehaviour
{
    public static FeedbackManager Instance { get; private set; }

    [Header("Feedback System")]
    [SerializeField] private bool verboseLogging = false;
    [SerializeField] private bool useMLInference = true;  // Now using native C# neural network!

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

    [Header("ML Models (Native Neural Network)")]
    private NativeNeuralNetwork dqnNetwork;
    private NativeNeuralNetwork ppoNetwork;
    private bool modelsLoaded = false;

    // Mapping from 6 original actions to 10 current actions
    private static readonly FeedbackAction[] ActionMapping = new FeedbackAction[]
    {
        FeedbackAction.ImproveSpeechPace,      // 0
        FeedbackAction.EncourageConfidence,    // 1
        FeedbackAction.OptimizeTone,           // 2
        FeedbackAction.ImproveVocalVariety,    // 3
        FeedbackAction.ReduceNervousness,      // 4
        FeedbackAction.MaintainCurrentApproach // 5
    };

    // Note: FeedbackUI removed - using FeedbackComparisonUI in BottomBarController instead

    [Header("Performance Tracking")]
    [SerializeField] private bool trackPerformance = true;
    [SerializeField] private int maxHistorySize = 8;

    // Performance history
    private List<InterviewPerformance> performanceHistory = new List<InterviewPerformance>();
    private List<FeedbackAction> feedbackHistory = new List<FeedbackAction>();
    // Cached overall scores for quick graphing
    private List<float> overallHistory = new List<float>();
    // If set, this value will be used for the next recorded history entry's overall
    private float? pendingDisplayedOverall = null;

    // Speed tracking for algorithms
    public float LastDQNInferenceTime { get; private set; }
    public float LastPPOInferenceTime { get; private set; }
    public float AverageDQNInferenceTime { get; private set; }
    public float AveragePPOInferenceTime { get; private set; }
    private List<float> dqnInferenceTimes = new List<float>();
    private List<float> ppoInferenceTimes = new List<float>();

    // Overall session score tracking
    private float sessionConfidenceTotal = 0f;
    private float sessionClarityTotal = 0f;
    private float sessionPaceTotal = 0f;
    private float sessionToneTotal = 0f;
    private float sessionOverallTotal = 0f;
    private int sessionQuestionCount = 0;

    // Separate tracking for DQN and PPO
    private float dqnConfidenceTotal = 0f;
    private float dqnClarityTotal = 0f;
    private float dqnPaceTotal = 0f;
    private float dqnToneTotal = 0f;
    private float dqnOverallTotal = 0f;
    private int dqnQuestionCount = 0;

    private float ppoConfidenceTotal = 0f;
    private float ppoClarityTotal = 0f;
    private float ppoPaceTotal = 0f;
    private float ppoToneTotal = 0f;
    private float ppoOverallTotal = 0f;
    private int ppoQuestionCount = 0;

    public float SessionAverageConfidence => sessionQuestionCount > 0 ? sessionConfidenceTotal / sessionQuestionCount : 0f;
    public float SessionAverageClarity => sessionQuestionCount > 0 ? sessionClarityTotal / sessionQuestionCount : 0f;
    public float SessionAveragePace => sessionQuestionCount > 0 ? sessionPaceTotal / sessionQuestionCount : 0f;
    public float SessionAverageTone => sessionQuestionCount > 0 ? sessionToneTotal / sessionQuestionCount : 0f;
    public float SessionAverageOverall => sessionQuestionCount > 0 ? sessionOverallTotal / sessionQuestionCount : 0f;
    public int SessionQuestionCount => sessionQuestionCount;

    // Current session tracking
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

        // Try to load ONNX models if ML inference is enabled
        if (useMLInference)
        {
            LoadONNXModels();
            
            // Test ML inference after loading (if verbose)
            if (verboseLogging && modelsLoaded)
            {
                TestMLInference();
            }
        }
        else if (verboseLogging)
        {
            Debug.Log("ML inference disabled (using rule-based feedback with DQN/PPO strategy differences)");
        }
    }
    
    /// <summary>
    /// Test method to verify ML inference is working
    /// </summary>
    private void TestMLInference()
    {
        Debug.Log("Testing ML Inference...");
        
        // Create test performance data
        InterviewPerformance testPerf = new InterviewPerformance
        {
            confidence = 0.6f,
            clarity = 0.7f,
            pace = 0.5f,
            tone = 0.65f,
            overall = 0.6f
        };
        
        // Test DQN
        currentModelType = ModelType.DQN;
        FeedbackAction dqnAction = GetMLFeedback(testPerf, out float dqnConf);
        Debug.Log($"   DQN -> {dqnAction} (confidence: {dqnConf:F2})");
        
        // Test PPO
        currentModelType = ModelType.PPO;
        FeedbackAction ppoAction = GetMLFeedback(testPerf, out float ppoConf);
        Debug.Log($"   PPO -> {ppoAction} (confidence: {ppoConf:F2})");
        
        Debug.Log("ML Inference test complete!");
    }

    private void LoadONNXModels()
    {
        try
        {
            string dqnPath = Path.Combine(Application.streamingAssetsPath, "dqn_weights.json");
            string ppoPath = Path.Combine(Application.streamingAssetsPath, "ppo_weights.json");

            if (File.Exists(dqnPath) && File.Exists(ppoPath))
            {
                Debug.Log($"Loading neural network weights from StreamingAssets...");
                
                // Create and load native neural networks
                dqnNetwork = new NativeNeuralNetwork();
                ppoNetwork = new NativeNeuralNetwork();
                
                bool dqnLoaded = dqnNetwork.LoadFromJSON(dqnPath);
                bool ppoLoaded = ppoNetwork.LoadFromJSON(ppoPath);
                
                if (dqnLoaded && ppoLoaded)
                {
                    modelsLoaded = true;
                    Debug.Log($"Native neural networks loaded successfully!");
                    Debug.Log($"   DQN: {dqnPath}");
                    Debug.Log($"   PPO: {ppoPath}");
                }
                else
                {
                    Debug.LogWarning("Failed to load one or more models, using rule-based fallback");
                    modelsLoaded = false;
                }
            }
            else
            {
                Debug.LogWarning("Model weight files not found in StreamingAssets, using rule-based fallback");
                if (!File.Exists(dqnPath)) Debug.LogWarning($"   Missing: {dqnPath}");
                if (!File.Exists(ppoPath)) Debug.LogWarning($"   Missing: {ppoPath}");
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Failed to load neural networks: {e.Message}");
            Debug.LogWarning("   Using rule-based feedback fallback");
            modelsLoaded = false;
        }
    }

    private void OnDestroy()
    {
        // Native networks don't need cleanup
    }

    public void SetModelType(ModelType modelType)
    {
        currentModelType = modelType;
        if (verboseLogging)
        {
            Debug.Log($"Switched to {modelType} model");
        }
    }

    /// <summary>
    /// Generate feedback based on voice analysis
    /// </summary>
    public FeedbackMessage GenerateFeedback(string transcribedText, float duration)
    {
        return GenerateFeedback(transcribedText, duration, updateSessionScore: true, recordHistory: true);
    }

    /// <summary>
    /// Generate feedback based on voice analysis with option to update session score
    /// </summary>
    public FeedbackMessage GenerateFeedback(string transcribedText, float duration, bool updateSessionScore, bool recordHistory = true)
    {
        // Start timing
        float startTime = Time.realtimeSinceStartup;

        // Analyze voice/text performance
        InterviewPerformance currentPerformance = voiceAnalyzer != null 
            ? voiceAnalyzer.AnalyzeResponse(transcribedText, duration)
            : new InterviewPerformance { overall = 0.5f };

        // Choose between ML inference or rule-based
        FeedbackAction action;
        float confidence;

        if (modelsLoaded && useMLInference)
        {
            action = GetMLFeedback(currentPerformance, out confidence);
            
            // Track inference time
            float inferenceTime = (Time.realtimeSinceStartup - startTime) * 1000f; // Convert to milliseconds
            TrackInferenceTime(currentModelType, inferenceTime);
            
            if (verboseLogging)
            {
                Debug.Log($"ML Inference ({currentModelType}): {action} (confidence: {confidence:F2}, time: {inferenceTime:F2}ms)");
            }
        }
        else
        {
            action = GetRuleBasedFeedback(currentPerformance);
            confidence = 0.7f; // Simulated confidence for rule-based
            
            // Track rule-based time
            float processingTime = (Time.realtimeSinceStartup - startTime) * 1000f;
            TrackInferenceTime(currentModelType, processingTime);
            
            if (verboseLogging)
            {
                Debug.Log($"Rule-Based ({currentModelType}): {action} (time: {processingTime:F2}ms)");
            }
        }

        // Predict performance improvement
        InterviewPerformance expectedImprovement = PredictImprovement(action, currentPerformance);

        // Create feedback message
        FeedbackMessage feedback = FeedbackMessage.Create(action, confidence, currentPerformance, expectedImprovement);

        // Update history (only if tracking is enabled and caller allows recording)
        if (trackPerformance && recordHistory)
        {
            UpdateHistory(currentPerformance, action);
        }

        // Update session score tracking only if requested
        if (updateSessionScore)
        {
            UpdateSessionScore(currentPerformance);
        }

        lastPerformance = currentPerformance;

        return feedback;
    }

    /// <summary>
    /// Get feedback using ML inference (Barracuda)
    /// </summary>
    private FeedbackAction GetMLFeedback(InterviewPerformance performance, out float confidence)
    {
        try
        {
            // Build 30D observation vector (matching training format)
            // 25 speech features + 5 performance metrics
            float[] observation = new float[30];
            
            // First 25 elements: simulated speech features (would come from real audio analysis)
            for (int i = 0; i < 25; i++)
            {
                observation[i] = UnityEngine.Random.Range(0f, 1f);
            }
            
            // Last 5 elements: performance metrics
            observation[25] = performance.confidence;
            observation[26] = performance.clarity;
            observation[27] = performance.pace;
            observation[28] = performance.tone;
            observation[29] = performance.overall;

            // Run inference using native neural network
            NativeNeuralNetwork network = (currentModelType == ModelType.DQN) ? dqnNetwork : ppoNetwork;
            int actionIndex = network.GetAction(observation, out confidence);
            
            // Map from 6 original actions to current FeedbackAction enum
            if (actionIndex >= 0 && actionIndex < ActionMapping.Length)
            {
                return ActionMapping[actionIndex];
            }
            else
            {
                Debug.LogWarning($"Invalid action index {actionIndex}, using fallback");
                confidence = 0.5f;
                return GetRuleBasedFeedback(performance);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"ML inference failed: {e.Message}, falling back to rules");
            confidence = 0.5f;
            return GetRuleBasedFeedback(performance);
        }
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
        
        if (verboseLogging)
        {
            Debug.Log($"Performance: conf={performance.confidence:F2}, clarity={performance.clarity:F2}, pace={performance.pace:F2}, tone={performance.tone:F2}, overall={overall:F2}");
        }
        
        // Excellent performance - celebrate it!
        if (overall >= 0.9f)
            return FeedbackAction.ExcellentPerformance;
        
        if (isDQN)
        {
            // DQN: Conservative, fix weakest speech metric directly
            if (performance.confidence < 0.4f)
                return FeedbackAction.EncourageConfidence;
            else if (performance.pace < 0.3f)
                return FeedbackAction.SpeedUpPacing;
            else if (performance.pace > 0.8f)
                return FeedbackAction.SlowDownPacing;
            else if (performance.tone < 0.45f)
                return FeedbackAction.AddEnthusiasm;
            else if (performance.confidence < 0.55f)
                return FeedbackAction.EncourageConfidence;
            else if (performance.pace < 0.5f)
                return FeedbackAction.ImproveSpeechPace;
            else if (overall < 0.65f)
                return FeedbackAction.ReduceNervousness;
            else if (performance.tone < 0.65f)
                return FeedbackAction.OptimizeTone;
            else
                return FeedbackAction.MaintainCurrentApproach;
        }
        else
        {
            // PPO: More exploratory, considers tone patterns and vocal variety
            // Add randomness to thresholds to simulate exploration
            float exploration = UnityEngine.Random.Range(-0.05f, 0.05f);
            
            if (overall < 0.5f)
                return FeedbackAction.ReduceNervousness;
            else if (performance.confidence < (0.45f + exploration) && performance.tone < 0.5f)
                return FeedbackAction.ImproveVocalVariety;
            else if (performance.confidence < (0.5f + exploration))
                return FeedbackAction.EncourageConfidence;
            else if (performance.pace > (0.78f + exploration))
                return FeedbackAction.SlowDownPacing;
            else if (performance.pace < (0.35f + exploration))
                return FeedbackAction.SpeedUpPacing;
            else if (performance.tone < (0.5f + exploration))
                return FeedbackAction.AddEnthusiasm;
            else if (overall < (0.7f + exploration) && performance.tone > 0.5f)
                return FeedbackAction.ImproveVocalVariety;
            else if (performance.pace < (0.5f + exploration) || performance.pace > (0.7f + exploration))
                return FeedbackAction.ImproveSpeechPace;
            else if (performance.tone < (0.65f + exploration))
                return FeedbackAction.OptimizeTone;
            else if (performance.confidence < (0.7f + exploration))
                return FeedbackAction.EncourageConfidence;
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
                improvement.overall = baseImprovement * 0.5f;
                break;
                
            case FeedbackAction.SlowDownPacing:
                improvement.pace = baseImprovement * 1.0f;
                improvement.confidence = baseImprovement * 0.3f;
                improvement.overall = baseImprovement * 0.6f;
                break;
                
            case FeedbackAction.SpeedUpPacing:
                improvement.pace = baseImprovement * 1.0f;
                improvement.overall = baseImprovement * 0.5f;
                break;

            case FeedbackAction.OptimizeTone:
                improvement.tone = baseImprovement * 1.0f;
                improvement.confidence = baseImprovement * 0.3f;
                improvement.overall = baseImprovement * 0.5f;
                break;
                
            case FeedbackAction.AddEnthusiasm:
                improvement.tone = baseImprovement * 1.2f;
                improvement.overall = baseImprovement * 0.6f;
                break;

            case FeedbackAction.ReduceNervousness:
                improvement.confidence = baseImprovement * 0.9f;
                improvement.tone = baseImprovement * 0.6f;
                improvement.pace = baseImprovement * 0.4f;
                improvement.overall = baseImprovement * 0.6f;
                break;

            case FeedbackAction.ImproveVocalVariety:
                improvement.tone = baseImprovement * 1.0f;
                improvement.pace = baseImprovement * 0.5f;
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

        // Keep a parallel list of overall scores for easy access by UI graphs
        // Prefer the displayed overall (set by the comparison UI) if provided
        float overallToAdd = performance.overall;
        if (pendingDisplayedOverall.HasValue)
        {
            overallToAdd = Mathf.Clamp01(pendingDisplayedOverall.Value);
            pendingDisplayedOverall = null;
            if (verboseLogging)
                Debug.Log($"FeedbackManager: using displayed overall {overallToAdd:F2} for history entry");
        }
        overallHistory.Add(overallToAdd);

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

    /// <summary>
    /// Return a copy of the recorded per-question overall scores for the current session.
    /// </summary>
    public List<float> GetSessionOverallHistory()
    {
        return new List<float>(overallHistory);
    }

    /// <summary>
    /// Track inference time for algorithm speed comparison
    /// </summary>
    private void TrackInferenceTime(ModelType modelType, float timeMs)
    {
        if (modelType == ModelType.DQN)
        {
            LastDQNInferenceTime = timeMs;
            dqnInferenceTimes.Add(timeMs);
            
            // Calculate running average
            float sum = 0f;
            foreach (float time in dqnInferenceTimes)
                sum += time;
            AverageDQNInferenceTime = sum / dqnInferenceTimes.Count;
        }
        else // PPO
        {
            LastPPOInferenceTime = timeMs;
            ppoInferenceTimes.Add(timeMs);
            
            // Calculate running average
            float sum = 0f;
            foreach (float time in ppoInferenceTimes)
                sum += time;
            AveragePPOInferenceTime = sum / ppoInferenceTimes.Count;
        }
    }

    /// <summary>
    /// Get speed comparison statistics
    /// </summary>
    public (float dqnAvg, float ppoAvg, float dqnLast, float ppoLast, int dqnCount, int ppoCount) GetSpeedStats()
    {
        return (AverageDQNInferenceTime, AveragePPOInferenceTime, 
                LastDQNInferenceTime, LastPPOInferenceTime,
                dqnInferenceTimes.Count, ppoInferenceTimes.Count);
    }

    /// <summary>
    /// Update session score totals with current performance
    /// </summary>
    private void UpdateSessionScore(InterviewPerformance performance)
    {
        sessionConfidenceTotal += performance.confidence;
        sessionClarityTotal += performance.clarity;
        sessionPaceTotal += performance.pace;
        sessionToneTotal += performance.tone;
        sessionOverallTotal += performance.overall;
        sessionQuestionCount++;

        if (verboseLogging)
        {
            Debug.Log($"Session Score Updated - Question {sessionQuestionCount}: Overall Avg = {SessionAverageOverall:F2}");
        }
    }

    /// <summary>
    /// Manually update session score with a specific performance (used when player chooses feedback)
    /// </summary>
    public void RecordPerformanceScore(InterviewPerformance performance)
    {
        UpdateSessionScore(performance);
    }

    /// <summary>
    /// Set the overall score that should be used for the next history entry.
    /// This allows the UI (comparison) to dictate what the bar graph will display.
    /// </summary>
    public void SetNextHistoryOverall(float overallDisplayed)
    {
        pendingDisplayedOverall = overallDisplayed;
        if (verboseLogging)
            Debug.Log($"FeedbackManager: pendingDisplayedOverall set to {overallDisplayed:F2}");
    }

    /// <summary>
    /// Public wrapper to record a performance into the session history (used when player chooses feedback).
    /// </summary>
    public void RecordPerformanceHistory(InterviewPerformance performance, FeedbackAction action)
    {
        if (!trackPerformance || performance == null) return;
        UpdateHistory(performance, action);
        Debug.Log($"FeedbackManager: Recorded performance overall={performance.overall:F2} (action={action}), historyCount={performanceHistory.Count}");
    }

    /// <summary>
    /// Record DQN algorithm's performance score
    /// </summary>
    public void RecordDQNScore(InterviewPerformance performance)
    {
        dqnConfidenceTotal += performance.confidence;
        dqnClarityTotal += performance.clarity;
        dqnPaceTotal += performance.pace;
        dqnToneTotal += performance.tone;
        dqnOverallTotal += performance.overall;
        dqnQuestionCount++;
    }

    /// <summary>
    /// Record PPO algorithm's performance score
    /// </summary>
    public void RecordPPOScore(InterviewPerformance performance)
    {
        ppoConfidenceTotal += performance.confidence;
        ppoClarityTotal += performance.clarity;
        ppoPaceTotal += performance.pace;
        ppoToneTotal += performance.tone;
        ppoOverallTotal += performance.overall;
        ppoQuestionCount++;
    }

    /// <summary>
    /// Get detailed session score breakdown
    /// </summary>
    public (float avgConfidence, float avgClarity, float avgPace, float avgTone, float avgOverall, int questionCount) GetSessionScoreBreakdown()
    {
        return (SessionAverageConfidence, SessionAverageClarity, SessionAveragePace, 
                SessionAverageTone, SessionAverageOverall, sessionQuestionCount);
    }

    /// <summary>
    /// Get DQN algorithm score breakdown
    /// </summary>
    public (float avgConfidence, float avgClarity, float avgPace, float avgTone, float avgOverall, int questionCount) GetDQNScoreBreakdown()
    {
        float avgConf = dqnQuestionCount > 0 ? dqnConfidenceTotal / dqnQuestionCount : 0f;
        float avgClar = dqnQuestionCount > 0 ? dqnClarityTotal / dqnQuestionCount : 0f;
        float avgPace = dqnQuestionCount > 0 ? dqnPaceTotal / dqnQuestionCount : 0f;
        float avgTone = dqnQuestionCount > 0 ? dqnToneTotal / dqnQuestionCount : 0f;
        float avgOverall = dqnQuestionCount > 0 ? dqnOverallTotal / dqnQuestionCount : 0f;
        
        return (avgConf, avgClar, avgPace, avgTone, avgOverall, dqnQuestionCount);
    }

    /// <summary>
    /// Get PPO algorithm score breakdown
    /// </summary>
    public (float avgConfidence, float avgClarity, float avgPace, float avgTone, float avgOverall, int questionCount) GetPPOScoreBreakdown()
    {
        float avgConf = ppoQuestionCount > 0 ? ppoConfidenceTotal / ppoQuestionCount : 0f;
        float avgClar = ppoQuestionCount > 0 ? ppoClarityTotal / ppoQuestionCount : 0f;
        float avgPace = ppoQuestionCount > 0 ? ppoPaceTotal / ppoQuestionCount : 0f;
        float avgTone = ppoQuestionCount > 0 ? ppoToneTotal / ppoQuestionCount : 0f;
        float avgOverall = ppoQuestionCount > 0 ? ppoOverallTotal / ppoQuestionCount : 0f;
        
        return (avgConf, avgClar, avgPace, avgTone, avgOverall, ppoQuestionCount);
    }

    public void ResetSession()
    {
        performanceHistory.Clear();
        feedbackHistory.Clear();
        overallHistory.Clear();
        lastPerformance = new InterviewPerformance { overall = 0.5f };
        
        // Reset session scores
        sessionConfidenceTotal = 0f;
        sessionClarityTotal = 0f;
        sessionPaceTotal = 0f;
        sessionToneTotal = 0f;
        sessionOverallTotal = 0f;
        sessionQuestionCount = 0;
        
        // Reset DQN scores
        dqnConfidenceTotal = 0f;
        dqnClarityTotal = 0f;
        dqnPaceTotal = 0f;
        dqnToneTotal = 0f;
        dqnOverallTotal = 0f;
        dqnQuestionCount = 0;
        
        // Reset PPO scores
        ppoConfidenceTotal = 0f;
        ppoClarityTotal = 0f;
        ppoPaceTotal = 0f;
        ppoToneTotal = 0f;
        ppoOverallTotal = 0f;
        ppoQuestionCount = 0;
        
        Debug.Log("Feedback session reset");
    }
}