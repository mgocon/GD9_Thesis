using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DataLogger : MonoBehaviour
{
    public static DataLogger Instance;

    private string fileName = "InterviewResults.csv";
    private string fullPath;

    // --- Runtime state ---
    private string currentLevelName = "";
    private string currentAlgorithm = "";
    private string currentQuestion = "";
    private int currentSentenceIndex = 0;
    private string accumulatedAnswer = ""; // ✅ Accumulate answers until silence timer

    // ✅ Keep memory of the most recent "real" level
    private string lastNonPersistentLevel = "";

    // ✅ Cache all CSV rows in memory for fast lookup and updates
    private List<CsvRow> csvData = new List<CsvRow>();
    private readonly object dataLock = new object();

    private class CsvRow
    {
        public string Timestamp;
        public string LevelName;
        public string Algorithm;
        public string SceneName;
        public string Question;
        public string PlayerAnswer;
        public int SentenceIndex;

        public string ToCsvLine()
        {
            string Escape(string s)
            {
                if (s == null) return "\"\"";
                s = s.Replace("\"", "\"\"");
                return $"\"{s}\"";
            }

            string si = SentenceIndex >= 0 ? SentenceIndex.ToString() : "";
            return string.Join(",", new string[]
            {
                Escape(Timestamp),
                Escape(LevelName),
                Escape(Algorithm),
                Escape(SceneName),
                Escape(Question),
                Escape(PlayerAnswer),
                Escape(si)
            });
        }
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitFile();

            // Listen to scene load events
            SceneManager.sceneLoaded += OnAnySceneLoaded;

            // ✅ Subscribe to silence timer event
            StartCoroutine(SubscribeToVoiceProcessor());
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // ✅ Subscribe to VoiceProcessor silence timer
    private System.Collections.IEnumerator SubscribeToVoiceProcessor()
    {
        // Wait for VoskManager to initialize
        while (VoskManager.Instance == null || VoskManager.Instance.GetSpeechToText() == null)
        {
            yield return null;
        }

        var voskSTT = VoskManager.Instance.GetSpeechToText();
        if (voskSTT?.VoiceProcessor != null)
        {
            voskSTT.VoiceProcessor.OnRecordingStop += OnSilenceTimerComplete;
            Debug.Log("✅ DataLogger subscribed to silence timer");
        }
    }

    // ✅ When silence timer completes, save accumulated answer
    private void OnSilenceTimerComplete()
    {
        if (!ShouldLogCurrentScene())
        {
            accumulatedAnswer = "";
            return;
        }

        if (!string.IsNullOrEmpty(accumulatedAnswer))
        {
            SaveAccumulatedAnswer();
            accumulatedAnswer = ""; // Reset for next phrase
            Debug.Log("🔄 Silence timer complete - answer saved and reset");
        }
    }

    // --- INITIALIZE CSV FILE ---
    private void InitFile()
    {
        fullPath = Path.Combine(Application.persistentDataPath, fileName);

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath) ?? Application.persistentDataPath);

            // ✅ Always reset the CSV file when game starts
            string header = "Timestamp,Level Name,Algorithm Chosen,Scene Name,Question/Dialogue,Player Answer,Sentence Index";
            File.WriteAllText(fullPath, header + Environment.NewLine, Encoding.UTF8);
            
            // ✅ Clear in-memory data
            csvData.Clear();
            
            Debug.Log($"📄 DataLogger: Reset CSV file at {fullPath}");
        }
        catch (Exception ex)
        {
            Debug.LogError("DataLogger: Failed to initialize file: " + ex.Message);
        }
    }

    // --- AUTO-DETECT SCENE CHANGE ---
    private void OnAnySceneLoaded(Scene scene, LoadSceneMode mode)
    {
        string sceneName = scene.name;
        string levelLabel = GetLevelNameFromSceneName(sceneName);

        // Only update if it's not Persistent Scene or Main Menu
        if (levelLabel != "Persistent Scene" && levelLabel != "Main Menu")
        {
            currentLevelName = levelLabel;
            lastNonPersistentLevel = levelLabel;
            Debug.Log($"📘 Scene loaded '{sceneName}' → Level set as {currentLevelName}");
        }
        else
        {
            Debug.Log($"📘 Scene loaded '{sceneName}' (ignored for level tracking)");
        }
    }

    // --- SCENE NAME TO FRIENDLY NAME ---
    private string GetLevelNameFromSceneName(string sceneName)
    {
        // Match by scene name instead of build index
        return sceneName switch
        {
            "Persistent Scene" => "Persistent Scene",
            "MAIN_MENU" => "Main Menu",
            "Tutorial Scene" => "Tutorial Level",
            "Tutorial" => "Tutorial Level",
            "TutorialScene" => "Tutorial Level",
            "Entry Level" => "Entry Level",
            "EntryLevel" => "Entry Level",
            "Senior Level" => "Senior Level",
            "SeniorLevel" => "Senior Level",
            _ => sceneName // Use actual scene name if no mapping found
        };
    }

    // ✅ Helper method to check if current scene should be logged
    private bool ShouldLogCurrentScene()
    {
        // Check the lastNonPersistentLevel instead of current active scene
        // This way we log based on the actual gameplay level, not the persistent scene
        return lastNonPersistentLevel != "Tutorial Level" && 
               !string.IsNullOrEmpty(lastNonPersistentLevel);
    }

    // --- ALGORITHM CHOICE ---
    public void LogAlgorithmChoice(string algorithm)
    {
        // ✅ Skip logging if in Tutorial
        if (!ShouldLogCurrentScene())
        {
            Debug.Log("⏭ Skipping algorithm log - Tutorial scene");
            return;
        }

        currentAlgorithm = algorithm ?? "";

        string effectiveLevel = string.IsNullOrEmpty(currentLevelName) || currentLevelName == "Persistent Scene" || currentLevelName == "Main Menu"
            ? lastNonPersistentLevel
            : currentLevelName;

        // ✅ Find existing row for current question and update algorithm
        lock (dataLock)
        {
            var existingRow = csvData.FirstOrDefault(r => 
                r.Question == currentQuestion && 
                r.SentenceIndex == currentSentenceIndex &&
                r.LevelName == effectiveLevel);

            if (existingRow != null)
            {
                existingRow.Algorithm = currentAlgorithm;
                existingRow.Timestamp = DateTime.Now.ToString("s");
                Debug.Log($"🔄 Updated algorithm for existing question: {currentQuestion}");
            }
            else
            {
                // Create new row if question doesn't exist yet
                var newRow = new CsvRow
                {
                    Timestamp = DateTime.Now.ToString("s"),
                    LevelName = effectiveLevel,
                    Algorithm = currentAlgorithm,
                    SceneName = SceneManager.GetActiveScene().name,
                    Question = currentQuestion,
                    PlayerAnswer = "",
                    SentenceIndex = currentSentenceIndex
                };
                csvData.Add(newRow);
            }

            WriteAllDataToFile();
        }

        Debug.Log($"🧠 Algorithm chosen: {currentAlgorithm} at level {effectiveLevel}");
    }

    // --- QUESTION/DIALOGUE LOGGING ---
    public void LogSentence(string question, int sentenceIndex)
    {
        // ✅ Skip logging if in Tutorial
        if (!ShouldLogCurrentScene())
        {
            Debug.Log("⏭ Skipping question log - Tutorial scene");
            return;
        }

        currentQuestion = question ?? "";
        currentSentenceIndex = sentenceIndex;

        string timestamp = DateTime.Now.ToString("s");
        string sceneName = SceneManager.GetActiveScene().name;

        string effectiveLevel = string.IsNullOrEmpty(currentLevelName) || currentLevelName == "Persistent Scene" || currentLevelName == "Main Menu"
            ? lastNonPersistentLevel
            : currentLevelName;

        lock (dataLock)
        {
            // ✅ Check if this exact question already exists
            var existingRow = csvData.FirstOrDefault(r => 
                r.Question == question && 
                r.SentenceIndex == sentenceIndex &&
                r.LevelName == effectiveLevel);

            if (existingRow != null)
            {
                existingRow.Timestamp = timestamp;
                Debug.Log($"🔄 Question already exists, updated timestamp: {question}");
            }
            else
            {
                // Add new question
                csvData.Add(new CsvRow
                {
                    Timestamp = timestamp,
                    LevelName = effectiveLevel,
                    Algorithm = currentAlgorithm,
                    SceneName = sceneName,
                    Question = question,
                    PlayerAnswer = "",
                    SentenceIndex = sentenceIndex
                });
                Debug.Log($"➕ Added new question: {question}");
            }

            WriteAllDataToFile();
        }

        // ✅ Reset accumulated answer when new question starts
        accumulatedAnswer = "";
    }

    // --- PLAYER ANSWER LOGGING (Accumulation) ---
    public void LogAnswer(string partialAnswer)
    {
        // ✅ Skip logging if in Tutorial
        if (!ShouldLogCurrentScene())
        {
            Debug.Log("⏭ Skipping answer log - Tutorial scene");
            return;
        }

        // ✅ Accumulate the answer instead of replacing
        if (!string.IsNullOrEmpty(accumulatedAnswer))
        {
            accumulatedAnswer += " ";
        }
        accumulatedAnswer += partialAnswer;

        Debug.Log($"🎙 Accumulated answer: {accumulatedAnswer}");
    }

    // ✅ Save the complete accumulated answer to CSV
    private void SaveAccumulatedAnswer()
    {
        string timestamp = DateTime.Now.ToString("s");
        string sceneName = SceneManager.GetActiveScene().name;

        string effectiveLevel = string.IsNullOrEmpty(currentLevelName) || currentLevelName == "Persistent Scene" || currentLevelName == "Main Menu"
            ? lastNonPersistentLevel
            : currentLevelName;

        lock (dataLock)
        {
            var existingRow = csvData.FirstOrDefault(r => 
                r.Question == currentQuestion && 
                r.SentenceIndex == currentSentenceIndex &&
                r.LevelName == effectiveLevel);

            if (existingRow != null)
            {
                existingRow.PlayerAnswer = accumulatedAnswer;
                existingRow.Timestamp = timestamp;
                Debug.Log($"💾 Saved complete answer for question: {currentQuestion}");
            }
            else
            {
                csvData.Add(new CsvRow
                {
                    Timestamp = timestamp,
                    LevelName = effectiveLevel,
                    Algorithm = currentAlgorithm,
                    SceneName = sceneName,
                    Question = currentQuestion,
                    PlayerAnswer = accumulatedAnswer,
                    SentenceIndex = currentSentenceIndex
                });
                Debug.Log($"➕ Added new row with complete answer: {accumulatedAnswer}");
            }

            WriteAllDataToFile();
        }
    }

    // ✅ Write all data to file (replaces entire file)
    private void WriteAllDataToFile()
    {
        try
        {
            bool success = false;
            int retries = 0;

            while (!success && retries < 5)
            {
                try
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("Timestamp,Level Name,Algorithm Chosen,Scene Name,Question/Dialogue,Player Answer,Sentence Index");

                    foreach (var row in csvData)
                    {
                        sb.AppendLine(row.ToCsvLine());
                    }

                    File.WriteAllText(fullPath, sb.ToString(), Encoding.UTF8);
                    success = true;
                }
                catch (IOException ex)
                {
                    if (ex.Message.Contains("Sharing violation"))
                    {
                        retries++;
                        Thread.Sleep(200);
                    }
                    else
                        throw;
                }
            }

            if (!success)
                Debug.LogError("❌ Could not write to CSV after multiple retries.");
        }
        catch (Exception ex)
        {
            Debug.LogError("DataLogger: Could not write file: " + ex.Message);
        }
    }

    public void SaveCSV()
    {
        lock (dataLock)
        {
            WriteAllDataToFile();
        }
        Debug.Log($"💾 File saved at {fullPath}");
    }

    private void OnApplicationQuit()
    {
        SaveCSV();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnAnySceneLoaded;
        
        // Unsubscribe from voice processor
        if (VoskManager.Instance?.GetSpeechToText()?.VoiceProcessor != null)
        {
            VoskManager.Instance.GetSpeechToText().VoiceProcessor.OnRecordingStop -= OnSilenceTimerComplete;
        }
    }
}
