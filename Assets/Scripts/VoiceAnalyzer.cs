using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Analyzes player's voice input to extract performance metrics
/// This is a simplified analyzer - you can enhance this with actual audio analysis
/// </summary>
public class VoiceAnalyzer : MonoBehaviour
{
    [Header("Analysis Settings")]
    [SerializeField] private float analysisThreshold = 0.1f;
    [SerializeField] private bool useSimulation = true;  // Set false when real audio analysis is ready

    private VoskSpeechToText voskSpeechToText;
    private VoiceProcessor voiceProcessor;

    // Track historical data for better analysis
    private List<float> volumeHistory = new List<float>();
    private List<string> recentTranscripts = new List<string>();
    
    private const int MAX_HISTORY = 10;

    private void Awake()
    {
        voskSpeechToText = FindObjectOfType<VoskSpeechToText>();
        voiceProcessor = FindObjectOfType<VoiceProcessor>();
    }

    /// <summary>
    /// Analyze the player's response and return performance metrics
    /// </summary>
    public InterviewPerformance AnalyzeResponse(string transcribedText, float duration)
    {
        var performance = new InterviewPerformance();

        if (useSimulation)
        {
            // Simulated analysis based on text length, word count, etc.
            performance = SimulateAnalysis(transcribedText, duration);
        }
        else
        {
            // Real analysis (implement this when ready)
            performance = RealAnalysis(transcribedText, duration);
        }

        return performance;
    }

    /// <summary>
    /// Simulated analysis based on heuristics
    /// </summary>
    private InterviewPerformance SimulateAnalysis(string text, float duration)
    {
        var performance = new InterviewPerformance();

        if (string.IsNullOrEmpty(text))
        {
            // Poor performance if no speech detected
            performance.confidence = 0.3f;
            performance.clarity = 0.3f;
            performance.pace = 0.5f;
            performance.tone = 0.4f;
            performance.overall = 0.35f;
            return performance;
        }

        // Word count analysis
        string[] words = text.Split(new char[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        int wordCount = words.Length;
        float wordLength = text.Length / Mathf.Max(wordCount, 1f);

        // Confidence: based on response length and complexity
        if (wordCount > 15)
            performance.confidence = UnityEngine.Random.Range(0.7f, 0.9f);
        else if (wordCount > 8)
            performance.confidence = UnityEngine.Random.Range(0.5f, 0.75f);
        else
            performance.confidence = UnityEngine.Random.Range(0.3f, 0.6f);

        // Clarity: based on average word length and sentence structure
        if (wordLength >= 4f && wordLength <= 7f)
            performance.clarity = UnityEngine.Random.Range(0.7f, 0.9f);
        else
            performance.clarity = UnityEngine.Random.Range(0.5f, 0.75f);

        // Pace: based on words per second
        float wordsPerSecond = duration > 0 ? wordCount / duration : 0f;
        if (wordsPerSecond >= 1.5f && wordsPerSecond <= 3.5f)
            performance.pace = UnityEngine.Random.Range(0.7f, 0.9f);
        else if (wordsPerSecond < 1.5f)
            performance.pace = UnityEngine.Random.Range(0.4f, 0.65f);  // Too slow
        else
            performance.pace = UnityEngine.Random.Range(0.5f, 0.7f);  // Too fast

        // Tone: simulated based on word variety
        HashSet<string> uniqueWords = new HashSet<string>(words, StringComparer.OrdinalIgnoreCase);
        float variety = uniqueWords.Count / Mathf.Max(wordCount, 1f);
        performance.tone = Mathf.Clamp(variety * 1.5f + UnityEngine.Random.Range(-0.1f, 0.1f), 0.3f, 0.9f);

        // Overall: weighted average
        performance.overall = (performance.confidence * 0.3f + 
                              performance.clarity * 0.25f + 
                              performance.pace * 0.2f + 
                              performance.tone * 0.25f);

        // Add some random variation to make it realistic
        performance.confidence = Mathf.Clamp01(performance.confidence + UnityEngine.Random.Range(-0.05f, 0.05f));
        performance.clarity = Mathf.Clamp01(performance.clarity + UnityEngine.Random.Range(-0.05f, 0.05f));
        performance.pace = Mathf.Clamp01(performance.pace + UnityEngine.Random.Range(-0.05f, 0.05f));
        performance.tone = Mathf.Clamp01(performance.tone + UnityEngine.Random.Range(-0.05f, 0.05f));

        return performance;
    }

    /// <summary>
    /// Real voice analysis using audio features
    /// TODO: Implement actual audio analysis using librosa-like features
    /// </summary>
    private InterviewPerformance RealAnalysis(string text, float duration)
    {
        // Placeholder for real implementation
        // You would extract features like:
        // - MFCCs (Mel-frequency cepstral coefficients)
        // - Pitch variation
        // - Energy levels
        // - Speaking rate
        // - Pause patterns
        
        Debug.LogWarning("Real audio analysis not yet implemented. Using simulation.");
        return SimulateAnalysis(text, duration);
    }

    /// <summary>
    /// Generate speech features vector (25 dimensions) for ML model input
    /// </summary>
    public float[] ExtractSpeechFeatures(string text, float duration)
    {
        // This should extract real audio features in production
        // For now, generate simulated features based on text analysis
        float[] features = new float[25];

        if (string.IsNullOrEmpty(text))
        {
            // Return low-confidence features
            for (int i = 0; i < 25; i++)
                features[i] = UnityEngine.Random.Range(-0.5f, 0.5f);
            return features;
        }

        // Simulate features based on text characteristics
        string[] words = text.Split(new char[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        int wordCount = words.Length;
        float wordLength = text.Length / Mathf.Max(wordCount, 1f);
        float wordsPerSecond = duration > 0 ? wordCount / duration : 0f;

        // Fill features with simulated values
        // In real implementation, these would be MFCCs, pitch, energy, etc.
        for (int i = 0; i < 25; i++)
        {
            float baseValue = 0f;
            
            // First 13 features: MFCC-like features
            if (i < 13)
                baseValue = UnityEngine.Random.Range(-1f, 1f) * (1f + wordLength / 10f);
            // Next 6 features: Prosody features (pitch, energy)
            else if (i < 19)
                baseValue = UnityEngine.Random.Range(0f, 1f) * wordsPerSecond / 3f;
            // Last 6 features: Speaking patterns
            else
                baseValue = UnityEngine.Random.Range(-0.5f, 0.5f) * (wordCount / 20f);

            features[i] = baseValue;
        }

        return features;
    }

    /// <summary>
    /// Get overall quality score from performance
    /// </summary>
    public float GetQualityScore(InterviewPerformance performance)
    {
        return performance.overall;
    }
}
