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

    // MODIFIED: Removed "Scene Name" column
    private const string CsvHeader = "Timestamp,Level Name,PlaythroughNumber,RowType,Algorithm Chosen,Question/Dialogue,Player Answer,Sentence Index," +
                                     "DQN_Confidence,DQN_Clarity,DQN_Pace,DQN_Tone,DQN_Overall," +
                                     "PPO_Confidence,PPO_Clarity,PPO_Pace,PPO_Tone,PPO_Overall," +
                                     "Session_Confidence,Session_Clarity,Session_Pace,Session_Tone,Session_Overall";


    private class CsvRow
    {
        // MODIFIED: All strings initialized to string.Empty for safety
        public string Timestamp = string.Empty;
        public string LevelName = string.Empty;
        public int PlaythroughNumber = 1;
        public string RowType = string.Empty;
        public string Algorithm = string.Empty;
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

        // MODIFIED: Removed SceneName from CSV output
        public string ToCsvLine()
        {
            string si = SentenceIndex >= 0 ? SentenceIndex.ToString() : "";
            
            return string.Join(",", new string[]
            {
                Escape(Timestamp),
                Escape(LevelName),
                PlaythroughNumber.ToString(),
                Escape(RowType),
                Escape(Algorithm),
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
        
        // MODIFIED: Updated to match new column count (removed SceneName)
        public static CsvRow FromCsvLine(string line)
        {
            string[] values = line.Split(',');

            string Unescape(string s)
            {
                if (s.StartsWith("\"") && s.EndsWith("\""))
                {
                    s = s.Substring(1, s.Length - 2);
                    s = s.Replace("\"\"", "\"");
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
                    Question = Unescape(values[5]),
                    PlayerAnswer = Unescape(values[6]),
                    SentenceIndex = int.TryParse(Unescape(values[7]), out int si) ? si : -1,
                    
                    DQN_Confidence = Unescape(values[8]),
                    DQN_Clarity = Unescape(values[9]),
                    DQN_Pace = Unescape(values[10]),
                    DQN_Tone = Unescape(values[11]),
                    DQN_Overall = Unescape(values[12]),
                    
                    PPO_Confidence = Unescape(values[13]),
                    PPO_Clarity = Unescape(values[14]),
                    PPO_Pace = Unescape(values[15]),
                    PPO_Tone = Unescape(values[16]),
                    PPO_Overall = Unescape(values[17]),
                    
                    Session_Confidence = Unescape(values[18]),
                    Session_Clarity = Unescape(values[19]),
                    Session_Pace = Unescape(values[20]),
                    Session_Tone = Unescape(values[21]),
                    Session_Overall = Unescape(values[22]),
                };
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"DataLogger: Failed to parse CSV line: {line}. Error: {ex.Message}");
                return null;
            }
        }

        private string Escape(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return "\"\"";
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
        if (!IsLoggableScene(currentLevelName))
        {
            accumulatedAnswer = "";
            return;
        }

        if (!string.IsNullOrEmpty(accumulatedAnswer))
        {
            SaveAccumulatedAnswer();
            accumulatedAnswer = "";
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
            WriteAllDataToFile(true); 
        }
        
        // 2. Clear the cache for the new session
        csvData.Clear();

        // 3. Now, set up the state for the NEW level
        currentLevelName = newLevelLabel;

        if (IsLoggableScene(currentLevelName))
        {
            // This is a playable level
            string key = $"PlayCount_{currentLevelName}";
            currentPlaythroughNumber = PlayerPrefs.GetInt(key, 0) + 1;
            
            Debug.Log($"📘 Scene loaded: {newSceneName} | Set to level: {currentLevelName} | This is Playthrough #{currentPlaythroughNumber}");
        }
        else
        {
            // This is the menu or manager scene
            Debug.Log($"📘 Scene loaded: {newSceneName} (ignored for level tracking)");
        }
    }

    // --- SCENE NAME TO FRIENDLY NAME ---
    private string GetLevelNameFromSceneName(string sceneName)
    {
        string lowerSceneName = sceneName.ToLowerInvariant();

        if (lowerSceneName.Contains("tutorial")) return "Tutorial Level";
        if (lowerSceneName.Contains("senior")) return "Senior Level"; 
        if (lowerSceneName.Contains("entry")) return "Entry Level";
        if (lowerSceneName.Contains("main_menu")) return "Main Menu";
        if (lowerSceneName.Contains("persistent")) return "Persistent Scene";
        
        return sceneName;
    }

    private bool IsLoggableScene(string levelName)
    {
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
                    RowType = "Question",
                    Algorithm = currentAlgorithm,
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
                    RowType = "Question",
                    Algorithm = currentAlgorithm,
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
        if (!IsLoggableScene(currentLevelName)) return;

        string timestamp = DateTime.Now.ToString("s");
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
                    RowType = "Question",
                    Algorithm = currentAlgorithm,
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
                Algorithm = "",
                RowType = "LevelSummary",
                Question = "",
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
            
            WriteAllDataToFile(true); 
            
            // AND NOW we update the PlayerPrefs count for this level
            string key = $"PlayCount_{currentLevelName}";
            PlayerPrefs.SetInt(key, currentPlaythroughNumber);
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
            return existingData;
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

    // MODIFIED: Changed sorting to use Timestamp instead of SentenceIndex
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
            
            // 3. SORT THE DATA by Timestamp instead of SentenceIndex
            if (sort)
            {
                allData = allData
                    .OrderBy(row => row.LevelName)
                    .ThenBy(row => row.PlaythroughNumber)
                    .ThenBy(row => row.RowType == "LevelSummary" ? 1 : 0)
                    .ThenBy(row => row.Timestamp)  // CHANGED: Sort by Timestamp instead of SentenceIndex
                    .ToList();
            }

            // 4. Overwrite the file with the newly sorted data
            using (var writer = new StreamWriter(fullPath, false, Encoding.UTF8))
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
            VoskManager.Instance.GetSpeechToText().VoiceProcessor.OnRecordingStop -= OnSilenceTimerComplete;
        }
    }
}