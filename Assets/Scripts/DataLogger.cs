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
        }
        else
        {
            Destroy(gameObject);
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

    // ✅ Load existing CSV data into memory (keeping this for potential future use)
    private void LoadExistingData()
    {
        lock (dataLock)
        {
            csvData.Clear();
            try
            {
                var lines = File.ReadAllLines(fullPath, Encoding.UTF8);
                for (int i = 1; i < lines.Length; i++) // Skip header
                {
                    var row = ParseCsvLine(lines[i]);
                    if (row != null)
                        csvData.Add(row);
                }
                Debug.Log($"📥 Loaded {csvData.Count} existing rows from CSV");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load existing CSV data: {ex.Message}");
            }
        }
    }

    // ✅ Parse a CSV line into a CsvRow object
    private CsvRow ParseCsvLine(string line)
    {
        try
        {
            var values = SplitCsvLine(line);
            if (values.Length < 7) return null;

            return new CsvRow
            {
                Timestamp = values[0],
                LevelName = values[1],
                Algorithm = values[2],
                SceneName = values[3],
                Question = values[4],
                PlayerAnswer = values[5],
                SentenceIndex = int.TryParse(values[6], out int idx) ? idx : -1
            };
        }
        catch
        {
            return null;
        }
    }

    // ✅ Split CSV line respecting quoted fields
    private string[] SplitCsvLine(string line)
    {
        var result = new List<string>();
        var current = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++; // skip next quote
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }
        result.Add(current.ToString());
        return result.ToArray();
    }

    // --- AUTO-DETECT SCENE CHANGE ---
    private void OnAnySceneLoaded(Scene scene, LoadSceneMode mode)
    {
        int index = scene.buildIndex;
        string levelLabel = GetLevelNameFromIndex(index);

        if (index != 0)
        {
            currentLevelName = levelLabel;
            lastNonPersistentLevel = levelLabel;
        }

        Debug.Log($"📘 Scene loaded '{scene.name}' (index {index}) → Level set as {currentLevelName}");
    }

    // --- SCENE INDEX TO FRIENDLY NAME ---
    private string GetLevelNameFromIndex(int index)
    {
        return index switch
        {
            1 => "Main Menu",
            2 => "Tutorial Level",
            3 => "Entry Level",
            4 => "Senior Level",
            0 => "Persistent Scene",
            _ => "Unknown Scene"
        };
    }

    // --- MANUAL OVERRIDE ---
    public void SetCurrentLevel(string levelName)
    {
        currentLevelName = levelName ?? "";
        if (!string.IsNullOrEmpty(levelName) && levelName != "Persistent Scene")
            lastNonPersistentLevel = levelName;

        Debug.Log($"📘 Level manually set: {currentLevelName}");
    }

    // --- ALGORITHM CHOICE ---
    public void LogAlgorithmChoice(string algorithm)
    {
        currentAlgorithm = algorithm ?? "";

        string effectiveLevel = (currentLevelName == "Persistent Scene" || string.IsNullOrEmpty(currentLevelName))
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
        currentQuestion = question ?? "";
        currentSentenceIndex = sentenceIndex;

        string timestamp = DateTime.Now.ToString("s");
        string sceneName = SceneManager.GetActiveScene().name;

        string effectiveLevel = (currentLevelName == "Persistent Scene" || string.IsNullOrEmpty(currentLevelName))
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
    }

    // --- PLAYER ANSWER LOGGING ---
    public void LogAnswer(string playerAnswer)
    {
        string timestamp = DateTime.Now.ToString("s");
        string sceneName = SceneManager.GetActiveScene().name;

        string effectiveLevel = (currentLevelName == "Persistent Scene" || string.IsNullOrEmpty(currentLevelName))
            ? lastNonPersistentLevel
            : currentLevelName;

        lock (dataLock)
        {
            // ✅ Find and update existing row
            var existingRow = csvData.FirstOrDefault(r => 
                r.Question == currentQuestion && 
                r.SentenceIndex == currentSentenceIndex &&
                r.LevelName == effectiveLevel);

            if (existingRow != null)
            {
                existingRow.PlayerAnswer = playerAnswer;
                existingRow.Timestamp = timestamp;
                Debug.Log($"🔄 Updated answer for question: {currentQuestion}");
            }
            else
            {
                // Create new row if question doesn't exist
                csvData.Add(new CsvRow
                {
                    Timestamp = timestamp,
                    LevelName = effectiveLevel,
                    Algorithm = currentAlgorithm,
                    SceneName = sceneName,
                    Question = currentQuestion,
                    PlayerAnswer = playerAnswer,
                    SentenceIndex = currentSentenceIndex
                });
                Debug.Log($"➕ Added new row with answer: {playerAnswer}");
            }

            WriteAllDataToFile();
        }

        Debug.Log($"🎙 Logged answer: {playerAnswer}");
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
}
