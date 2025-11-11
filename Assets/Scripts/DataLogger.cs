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
    private string accumulatedAnswer = "";
    
    // Tracks the current playthrough number for the loaded level
    private int currentPlaythroughNumber = 1;

    // Cache all CSV rows for the CURRENT SESSION in memory
    private List<CsvRow> csvData = new List<CsvRow>();
    private readonly object dataLock = new object();

    // MODIFIED: Added "RowType" column
    private const string CsvHeader = "Timestamp,Level Name,PlaythroughNumber,RowType,Algorithm Chosen,Scene Name,Question/Dialogue,Player Answer,Sentence Index," +
                                     "DQN_Confidence,DQN_Clarity,DQN_Pace,DQN_Tone,DQN_Overall," +
                                     "PPO_Confidence,PPO_Clarity,PPO_Pace,PPO_Tone,PPO_Overall," +
                                     "Session_Confidence,Session_Clarity,Session_Pace,Session_Tone,Session_Overall";


    private class CsvRow
    {
        // MODIFIED: All strings initialized to string.Empty for safety
        public string Timestamp = string.Empty;
        public string LevelName = string.Empty;
        public int PlaythroughNumber = 1;
        public string RowType = string.Empty; // NEW
        public string Algorithm = string.Empty;
        public string SceneName = string.Empty;
        public string Question = string.Empty;
        public string PlayerAnswer = string.Empty;
        public int SentenceIndex = -1;

        // NEW: Fields for DQN feedback
        public string DQN_Confidence = string.Empty;
        public string DQN_Clarity = string.Empty;
        public string DQN_Pace = string.Empty;
        public string DQN_Tone = string.Empty;
        public string DQN_Overall = string.Empty;

        // NEW: Fields for PPO feedback
        public string PPO_Confidence = string.Empty;
        public string PPO_Clarity = string.Empty;
        public string PPO_Pace = string.Empty;
        public string PPO_Tone = string.Empty;
        public string PPO_Overall = string.Empty;
        
        // NEW: Fields for Session summary (used in the final summary row)
        public string Session_Confidence = string.Empty;
        public string Session_Clarity = string.Empty;
        public string Session_Pace = string.Empty;
        public string Session_Tone = string.Empty;
        public string Session_Overall = string.Empty;

        // MODIFIED: Simplified to use the improved Escape function everywhere
        public string ToCsvLine()
        {
            string si = SentenceIndex >= 0 ? SentenceIndex.ToString() : "";
            
            return string.Join(",", new string[]
            {
                Escape(Timestamp),
                Escape(LevelName),
                PlaythroughNumber.ToString(), // This is a number, no escape needed
                Escape(RowType), // NEW
                Escape(Algorithm),
                Escape(SceneName),
                Escape(Question),
                Escape(PlayerAnswer),
                Escape(si),
                // DQN
                Escape(DQN_Confidence),
                Escape(DQN_Clarity),
                Escape(DQN_Pace),
                Escape(DQN_Tone),
                Escape(DQN_Overall),
                // PPO
                Escape(PPO_Confidence),
                Escape(PPO_Clarity),
                Escape(PPO_Pace),
                Escape(PPO_Tone),
                Escape(PPO_Overall),
                // Session
                Escape(Session_Confidence),
                Escape(Session_Clarity),
                Escape(Session_Pace),
                Escape(Session_Tone),
                Escape(Session_Overall)
            });
        }
        
        // Helper function to create a CsvRow object from a CSV line
        // Note: This is a simple parser and assumes no commas *within* escaped quotes
        public static CsvRow FromCsvLine(string line)
        {
            // Simple split, as our Escape function removes internal newlines
            string[] values = line.Split(',');

            // Helper to unescape
            string Unescape(string s)
            {
                if (s.StartsWith("\"") && s.EndsWith("\""))
                {
                    s = s.Substring(1, s.Length - 2); // Remove surrounding quotes
                    s = s.Replace("\"\"", "\""); // Un-double quotes
                }
                return s;
            }

            try
            {
                return new CsvRow
                {
                    Timestamp = Unescape(values[0]),
                    LevelName = Unescape(values[1]),
                    PlaythroughNumber = int.TryParse(values[2], out int pn) ? pn : 0,
                    RowType = Unescape(values[3]),
                    Algorithm = Unescape(values[4]),
                    SceneName = Unescape(values[5]),
                    Question = Unescape(values[6]),
                    PlayerAnswer = Unescape(values[7]),
                    SentenceIndex = int.TryParse(Unescape(values[8]), out int si) ? si : -1,
                    
                    DQN_Confidence = Unescape(values[9]),
                    DQN_Clarity = Unescape(values[10]),
                    DQN_Pace = Unescape(values[11]),
                    DQN_Tone = Unescape(values[12]),
                    DQN_Overall = Unescape(values[13]),
                    
                    PPO_Confidence = Unescape(values[14]),
                    PPO_Clarity = Unescape(values[15]),
                    PPO_Pace = Unescape(values[16]),
                    PPO_Tone = Unescape(values[17]),
                    PPO_Overall = Unescape(values[18]),
                    
                    Session_Confidence = Unescape(values[19]),
                    Session_Clarity = Unescape(values[20]),
                    Session_Pace = Unescape(values[21]),
                    Session_Tone = Unescape(values[22]),
                    Session_Overall = Unescape(values[23]),
                };
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"DataLogger: Failed to parse CSV line: {line}. Error: {ex.Message}");
                return null; // Return null if parsing fails
            }
        }

        // This function now cleans up newlines and properly handles null/empty strings
        private string Escape(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return "\"\""; // Return empty quotes
            }
            s = s.Replace("\n", " ").Replace("\r", " ");
            s = s.Replace("\"", "\"\"");
            return $"\"{s}\"";
        }
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            fullPath = Path.Combine(Application.persistentDataPath, fileName);
            Debug.Log($"📄 DataLogger: All data will be saved to {fullPath}");

            // --- RESET FILE AND PLAY COUNTS ON LAUNCH ---
            try
            {
                // 1. Delete the old CSV file
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                    Debug.Log("DataLogger: Deleted old CSV file.");
                }

                // 2. Reset the playthrough counts
                PlayerPrefs.DeleteKey($"PlayCount_{GetLevelNameFromSceneName("Entry Level")}");
                PlayerPrefs.DeleteKey($"PlayCount_{GetLevelNameFromSceneName("SENIORLEVEL_SCENE")}");
                PlayerPrefs.Save();
                Debug.Log("DataLogger: Reset playthrough counts for Entry and Senior levels.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"DataLogger: Error during reset: {ex.Message}");
            }
            // --- END OF RESET ---

            // Listen to scene load events
            SceneManager.sceneLoaded += OnAnySceneLoaded;
            StartCoroutine(SubscribeToVoiceProcessor());
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private System.Collections.IEnumerator SubscribeToVoiceProcessor()
    {
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

    private void OnSilenceTimerComplete()
    {
        if (!IsLoggableScene(currentLevelName)) // Use currentLevelName
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

    
    // --- AUTO-DETECT SCENE CHANGE ---
    private void OnAnySceneLoaded(Scene scene, LoadSceneMode mode)
    {
        string newSceneName = scene.name;
        string newLevelLabel = GetLevelNameFromSceneName(newSceneName);

        // 1. Check if the PREVIOUS level had data to save.
        if (csvData.Count > 0 && IsLoggableScene(currentLevelName))
        {
            Debug.Log($"Saving data for previous level: {currentLevelName}");
            // MODIFIED: Pass true to save and sort
            WriteAllDataToFile(true); 
        }
        
        // 2. Clear the cache for the new session
        csvData.Clear();

        // 3. Now, set up the state for the NEW level
        currentLevelName = newLevelLabel; // Set the new level name

        if (IsLoggableScene(currentLevelName))
        {
            // This is a playable level
            string key = $"PlayCount_{currentLevelName}";
            currentPlaythroughNumber = PlayerPrefs.GetInt(key, 0) + 1; // Get current count, add 1
            
            Debug.Log($"📘 Scene loaded: {newSceneName} | Set to level: {currentLevelName} | This is Playthrough #{currentPlaythroughNumber}");
        }
        else
        {
            // This is the menu or manager scene
            Debug.Log($"📘 Scene loaded: {newSceneName} (ignored for level tracking)");
        }
    }

    // --- SCENE NAME TO FRIENDLY NAME ---
    // MODIFIED: Made case-insensitive for safety and put SENIOR first
    private string GetLevelNameFromSceneName(string sceneName)
    {
        string lowerSceneName = sceneName.ToLowerInvariant();

        if (lowerSceneName.Contains("tutorial")) return "Tutorial Level";
        // --- FIX ---
        // Check for senior first, as a scene name might contain both
        if (lowerSceneName.Contains("senior")) return "Senior Level"; 
        if (lowerSceneName.Contains("entry")) return "Entry Level";
        // --- END FIX ---
        if (lowerSceneName.Contains("main_menu")) return "Main Menu";
        if (lowerSceneName.Contains("persistent")) return "Persistent Scene";
        
        return sceneName; // Fallback
    }

    // NEW helper function, very important
    private bool IsLoggableScene(string levelName)
    {
        // This ensures we only log when a *playable level* is currently active.
        return levelName != "Tutorial Level" && 
               !string.IsNullOrEmpty(levelName) &&
               levelName != "Main Menu" &&
               levelName != "Persistent Scene";
    }

    // --- ALGORITHM CHOICE ---
    public void LogAlgorithmChoice(string algorithm)
    {
        if (!IsLoggableScene(currentLevelName))
        {
            Debug.Log("⏭ Skipping algorithm log - not in a loggable scene.");
            return;
        }

        currentAlgorithm = algorithm ?? "";
        string effectiveLevel = currentLevelName;

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
                var newRow = new CsvRow
                {
                    Timestamp = DateTime.Now.ToString("s"),
                    LevelName = effectiveLevel,
                    PlaythroughNumber = currentPlaythroughNumber,
                    RowType = "Question", // NEW
                    Algorithm = currentAlgorithm,
                    SceneName = SceneManager.GetActiveScene().name,
                    Question = currentQuestion,
                    PlayerAnswer = "",
                    SentenceIndex = currentSentenceIndex
                };
                csvData.Add(newRow);
            }
        }

        Debug.Log($"🧠 Algorithm chosen: {currentAlgorithm} at level {effectiveLevel}");
    }

    // --- QUESTION/DIALOGUE LOGGING ---
    public void LogSentence(string question, int sentenceIndex)
    {
        if (!IsLoggableScene(currentLevelName))
        {
            Debug.Log("⏭ Skipping question log - not in a loggable scene.");
            return;
        }

        currentQuestion = question ?? "";
        currentSentenceIndex = sentenceIndex;

        string timestamp = DateTime.Now.ToString("s");
        string sceneName = SceneManager.GetActiveScene().name;
        string effectiveLevel = currentLevelName;

        lock (dataLock)
        {
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
                csvData.Add(new CsvRow
                {
                    Timestamp = timestamp,
                    LevelName = effectiveLevel,
                    PlaythroughNumber = currentPlaythroughNumber,
                    RowType = "Question", // NEW
                    Algorithm = currentAlgorithm,
                    SceneName = sceneName,
                    Question = question,
                    PlayerAnswer = "",
                    SentenceIndex = sentenceIndex
                });
                Debug.Log($"➕ Added new question: {question}");
            }
        }
        
        accumulatedAnswer = "";
    }

    // --- PLAYER ANSWER LOGGING (Accumulation) ---
    public void LogAnswer(string partialAnswer)
    {
        if (!IsLoggableScene(currentLevelName))
        {
            Debug.Log("⏭ Skipping answer log - not in a loggable scene.");
            return;
        }

        if (!string.IsNullOrEmpty(accumulatedAnswer))
        {
            accumulatedAnswer += " ";
        }
        accumulatedAnswer += partialAnswer;

        Debug.Log($"🎙 Accumulated answer: {accumulatedAnswer}");
    }

    private void SaveAccumulatedAnswer()
    {
        if (!IsLoggableScene(currentLevelName)) return; // Extra safety check

        string timestamp = DateTime.Now.ToString("s");
        string sceneName = SceneManager.GetActiveScene().name;
        string effectiveLevel = currentLevelName;

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
                    PlaythroughNumber = currentPlaythroughNumber,
                    RowType = "Question", // NEW
                    Algorithm = currentAlgorithm,
                    SceneName = sceneName,
                    Question = currentQuestion,
                    PlayerAnswer = accumulatedAnswer,
                    SentenceIndex = currentSentenceIndex
                });
                Debug.Log($"➕ Added new row with complete answer: {accumulatedAnswer}");
            }
        }
    }

    // --- NEW: LOG FEEDBACK SCORES ---
    public void LogFeedbackScores(FeedbackMessage dqn, FeedbackMessage ppo)
    {
        if (!IsLoggableScene(currentLevelName) || dqn == null || ppo == null)
        {
            return;
        }
        
        string effectiveLevel = currentLevelName;

        lock (dataLock)
        {
            var existingRow = csvData.FirstOrDefault(r => 
                r.Question == currentQuestion && 
                r.SentenceIndex == currentSentenceIndex &&
                r.LevelName == effectiveLevel);

            if (existingRow != null)
            {
                // Log DQN Performance
                existingRow.DQN_Confidence = dqn.currentPerformance.confidence.ToString("F2");
                existingRow.DQN_Clarity = dqn.currentPerformance.clarity.ToString("F2");
                existingRow.DQN_Pace = dqn.currentPerformance.pace.ToString("F2");
                existingRow.DQN_Tone = dqn.currentPerformance.tone.ToString("F2");
                existingRow.DQN_Overall = dqn.currentPerformance.overall.ToString("F2");
                
                // Log PPO Performance
                existingRow.PPO_Confidence = ppo.currentPerformance.confidence.ToString("F2");
                existingRow.PPO_Clarity = ppo.currentPerformance.clarity.ToString("F2");
                existingRow.PPO_Pace = ppo.currentPerformance.pace.ToString("F2");
                existingRow.PPO_Tone = ppo.currentPerformance.tone.ToString("F2");
                existingRow.PPO_Overall = ppo.currentPerformance.overall.ToString("F2");
                
                Debug.Log($"📈 Logged PPO/DQN scores for question: {currentQuestion}");
            }
        }
    }
    
    // --- NEW: LOG LEVEL SUMMARY ---
    public void LogLevelSummary()
    {
        if (!IsLoggableScene(currentLevelName) || FeedbackManager.Instance == null)
        {
            return;
        }

        var sessionBreakdown = FeedbackManager.Instance.GetSessionScoreBreakdown();
        var dqnBreakdown = FeedbackManager.Instance.GetDQNScoreBreakdown();
        var ppoBreakdown = FeedbackManager.Instance.GetPPOScoreBreakdown();

        string effectiveLevel = currentLevelName;
            
        lock (dataLock)
        {
            var summaryRow = new CsvRow
            {
                Timestamp = DateTime.Now.ToString("s"),
                LevelName = effectiveLevel,
                PlaythroughNumber = currentPlaythroughNumber,
                Algorithm = "", // Not applicable for summary
                SceneName = SceneManager.GetActiveScene().name,
                
                // --- THIS IS THE FIX ---
                RowType = "LevelSummary", // NEW
                Question = "", // NEW: Set question to blank
                // --- END OF FIX ---

                PlayerAnswer = "",
                SentenceIndex = -1,
                
                // Log DQN Averages
                DQN_Confidence = dqnBreakdown.avgConfidence.ToString("F2"),
                DQN_Clarity = dqnBreakdown.avgClarity.ToString("F2"),
                DQN_Pace = dqnBreakdown.avgPace.ToString("F2"),
                DQN_Tone = dqnBreakdown.avgTone.ToString("F2"),
                DQN_Overall = dqnBreakdown.avgOverall.ToString("F2"),
                
                // Log PPO Averages
                PPO_Confidence = ppoBreakdown.avgConfidence.ToString("F2"),
                PPO_Clarity = ppoBreakdown.avgClarity.ToString("F2"),
                PPO_Pace = ppoBreakdown.avgPace.ToString("F2"),
                PPO_Tone = ppoBreakdown.avgTone.ToString("F2"),
                PPO_Overall = ppoBreakdown.avgOverall.ToString("F2"),
                
                // Log Session (Player's Choice) Averages
                Session_Confidence = sessionBreakdown.avgConfidence.ToString("F2"),
                Session_Clarity = sessionBreakdown.avgClarity.ToString("F2"),
                Session_Pace = sessionBreakdown.avgPace.ToString("F2"),
                Session_Tone = sessionBreakdown.avgTone.ToString("F2"),
                Session_Overall = sessionBreakdown.avgOverall.ToString("F2")
            };
            
            csvData.Add(summaryRow);
            Debug.Log($"📊 Logged Level Summary for {effectiveLevel}, Playthrough {currentPlaythroughNumber}");
            
            // MODIFIED: Pass true to save and sort
            WriteAllDataToFile(true); 
            
            // AND NOW we update the PlayerPrefs count for this level
            string key = $"PlayCount_{currentLevelName}";
            PlayerPrefs.SetInt(key, currentPlaythroughNumber); // It was already +1
            PlayerPrefs.Save();
            Debug.Log($"Saved new play count ({currentPlaythroughNumber}) for {currentLevelName}");
        }
    }

    // --- THIS IS THE NEW SORTING LOGIC ---
    
    // Reads all existing data from the file
    private List<CsvRow> ReadExistingData()
    {
        var existingData = new List<CsvRow>();
        if (!File.Exists(fullPath))
        {
            return existingData; // Return empty list
        }

        try
        {
            var lines = File.ReadAllLines(fullPath);
            // Skip header row (index 0)
            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;
                
                var row = CsvRow.FromCsvLine(lines[i]);
                if (row != null)
                {
                    existingData.Add(row);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"DataLogger: Error reading existing CSV data: {ex.Message}");
        }
        
        return existingData;
    }

    // MODIFIED: This function now READS, SORTS, and OVERWRITES
    private void WriteAllDataToFile(bool sort = false)
    {
        if (csvData.Count == 0)
        {
            Debug.Log("No new data to write.");
            return;
        }

        try
        {
            // 1. Get all data that's already in the file
            List<CsvRow> allData = ReadExistingData();
            
            // 2. Add the new data from this session
            allData.AddRange(csvData);
            
            // 3. SORT THE DATA (This is your request!)
            if (sort)
            {
                allData = allData
                    .OrderBy(row => row.LevelName)          // First, sort by Level Name
                    .ThenBy(row => row.PlaythroughNumber)   // Then, by Playthrough Number
                    .ThenBy(row => row.RowType == "LevelSummary" ? 1 : 0) // Put summaries at the end of each play
                    .ThenBy(row => row.SentenceIndex)       // Then by the question order
                    .ToList();
            }

            // 4. Overwrite the file with the newly sorted data
            using (var writer = new StreamWriter(fullPath, false, Encoding.UTF8)) // false = OVERWRITE
            {
                // Write the header first
                writer.WriteLine(CsvHeader);
                
                // Write all sorted rows
                foreach (var row in allData)
                {
                    writer.WriteLine(row.ToCsvLine());
                }
            }
            
            Debug.Log($"💾 Successfully SAVED and SORTED {allData.Count} total rows to {fullPath}");
            
            // We have written the data, so clear the cache to prevent duplicates
            csvData.Clear();
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
            // Pass true to sort the file on quit
            WriteAllDataToFile(true);
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
            // --- THIS IS THE FIX ---
            // Was: voskSTT.VoiceProcessor.OnRecordingStop -= OnSilenceTimerComplete;
            VoskManager.Instance.GetSpeechToText().VoiceProcessor.OnRecordingStop -= OnSilenceTimerComplete;
        }
    }
}