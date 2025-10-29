using System;
using System.IO;
using System.Text;
using System.Threading;
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

            if (!File.Exists(fullPath))
            {
                string header = "Timestamp,Level Name,Algorithm Chosen,Scene Name,Question/Dialogue,Player Answer,Sentence Index";
                File.WriteAllText(fullPath, header + Environment.NewLine, Encoding.UTF8);
                Debug.Log($"📄 DataLogger: Created CSV at {fullPath}");
            }
            else
            {
                Debug.Log($"📄 DataLogger: Using existing CSV at {fullPath}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("DataLogger: Failed to initialize file: " + ex.Message);
        }
    }

    // --- AUTO-DETECT SCENE CHANGE ---
    private void OnAnySceneLoaded(Scene scene, LoadSceneMode mode)
    {
        int index = scene.buildIndex;
        string levelLabel = GetLevelNameFromIndex(index);

        // Don't overwrite with Persistent Scene — keep last gameplay scene
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

    // --- MANUAL OVERRIDE (GameController can call this) ---
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

        // ✅ Use last known non-persistent level if current is Persistent
        string effectiveLevel = (currentLevelName == "Persistent Scene" || string.IsNullOrEmpty(currentLevelName))
            ? lastNonPersistentLevel
            : currentLevelName;

        AppendCsvRow(DateTime.Now.ToString("s"), effectiveLevel, currentAlgorithm,
            SceneManager.GetActiveScene().name,
            "<ALGORITHM_CHOSEN>", "", -1);

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

        AppendCsvRow(timestamp, effectiveLevel, currentAlgorithm, sceneName, question, "", sentenceIndex);
        Debug.Log($"🗒 Logged question: {question}");
    }

    // --- PLAYER ANSWER LOGGING ---
    public void LogAnswer(string playerAnswer)
    {
        string timestamp = DateTime.Now.ToString("s");
        string sceneName = SceneManager.GetActiveScene().name;

        string effectiveLevel = (currentLevelName == "Persistent Scene" || string.IsNullOrEmpty(currentLevelName))
            ? lastNonPersistentLevel
            : currentLevelName;

        AppendCsvRow(timestamp, effectiveLevel, currentAlgorithm, sceneName, currentQuestion, playerAnswer, currentSentenceIndex);
        Debug.Log($"🎙 Logged answer: {playerAnswer}");
    }

    // --- SAFE FILE APPENDING ---
    private void AppendCsvRow(string timestamp, string levelName, string algorithm, string sceneName,
                              string question, string playerAnswer, int sentenceIndex)
    {
        try
        {
            string Escape(string s)
            {
                if (s == null) return "\"\"";
                s = s.Replace("\"", "\"\"");
                return $"\"{s}\"";
            }

            string si = sentenceIndex >= 0 ? sentenceIndex.ToString() : "";
            string line = string.Join(",", new string[]
            {
                Escape(timestamp),
                Escape(levelName),
                Escape(algorithm),
                Escape(sceneName),
                Escape(question),
                Escape(playerAnswer),
                Escape(si)
            });

            bool success = false;
            int retries = 0;
            while (!success && retries < 5)
            {
                try
                {
                    using (var writer = new StreamWriter(fullPath, true, Encoding.UTF8))
                    {
                        writer.WriteLine(line);
                    }
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
                Debug.LogError("❌ Could not append to CSV after multiple retries.");
        }
        catch (Exception ex)
        {
            Debug.LogError("DataLogger: Could not append row: " + ex.Message);
        }
    }

    public void SaveCSV()
    {
        if (!File.Exists(fullPath))
            InitFile();

        Debug.Log($"💾 File saved at {fullPath}");
    }

    private void OnApplicationQuit()
    {
        SaveCSV();
    }
}
