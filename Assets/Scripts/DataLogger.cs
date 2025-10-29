using System;
using System.IO;
using System.Text;
using System.Threading;
using UnityEngine;

public class DataLogger : MonoBehaviour
{
    public static DataLogger Instance;

    private string fileName = "InterviewResults.csv";
    private string fullPath;
    private string currentLevelName = "";
    private string currentAlgorithm = "";
    private string currentQuestion = "";
    private int currentSentenceIndex = 0;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitFile();
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

    // --- SETTERS ---
    public void SetCurrentLevel(string levelName)
    {
        currentLevelName = levelName ?? "";
        Debug.Log($"📘 Level set: {currentLevelName}");
    }

    public void LogAlgorithmChoice(string algorithm)
    {
        currentAlgorithm = algorithm ?? "";
        AppendCsvRow(DateTime.Now.ToString("s"), currentLevelName, currentAlgorithm,
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
            "<ALGORITHM_CHOSEN>", "", -1);
        Debug.Log($"🧠 Algorithm chosen: {currentAlgorithm}");
    }

    // Called when showing a new question or dialogue
    public void LogSentence(string question, int sentenceIndex)
    {
        currentQuestion = question ?? "";
        currentSentenceIndex = sentenceIndex;
        string timestamp = DateTime.Now.ToString("s");
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        AppendCsvRow(timestamp, currentLevelName, currentAlgorithm, sceneName, question, "", sentenceIndex);
        Debug.Log($"🗒 Logged question: {question}");
    }

    // Called when the player gives a spoken answer (from Vosk)
    public void LogAnswer(string playerAnswer)
    {
        string timestamp = DateTime.Now.ToString("s");
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        AppendCsvRow(timestamp, currentLevelName, currentAlgorithm, sceneName, currentQuestion, playerAnswer, currentSentenceIndex);
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
