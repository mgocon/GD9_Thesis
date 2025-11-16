using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Ionic.Zip;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Networking;
using Vosk;

public class VoskSpeechToText : MonoBehaviour
{
    [Tooltip("Location of the model, relative to the Streaming Assets folder.")]
    public string ModelPath = "vosk-model-en-us-0.42-gigaspeech.zip";

    [Tooltip("The source of the microphone input.")]
    public VoiceProcessor VoiceProcessor;

    [Tooltip("The Max number of alternatives that will be processed.")]
    public int MaxAlternatives = 3;

    [Tooltip("How long should we record before restarting?")]
    public float MaxRecordLength = 5;

    [Tooltip("Should the recognizer start when the application is launched?")]
    public bool AutoStart = true;

    [Tooltip("The phrases that will be detected. If left empty, all words will be detected.")]
    public List<string> KeyPhrases = new List<string>();

    // Cached Vosk Model + Recognizer
    private Model _model;
    private VoskRecognizer _recognizer;
    private bool _recognizerReady;

    // Audio + threading state
    private readonly ConcurrentQueue<short[]> _threadedBufferQueue = new ConcurrentQueue<short[]>();
    private readonly ConcurrentQueue<string> _threadedResultQueue = new ConcurrentQueue<string>();
    private bool _running;

    // Status + events
    private string _decompressedModelPath;
    private string _grammar = "";
    private bool _isInitializing;
    private bool _didInit;

    public Action<string> OnStatusUpdated;
    public Action<string> OnTranscriptionResult;

    public bool RecognizerReady => _recognizerReady;
    public bool IsModelLoaded { get; private set; }
    public bool IsInitializing => _isInitializing;

    static readonly ProfilerMarker voskRecognizerCreateMarker = new ProfilerMarker("VoskRecognizer.Create");
    static readonly ProfilerMarker voskRecognizerReadMarker = new ProfilerMarker("VoskRecognizer.AcceptWaveform");

    void Start()
    {
        if (AutoStart)
        {
            StartVoskStt();
        }
    }

    public void StartVoskStt(List<string> keyPhrases = null, string modelPath = default, bool startMicrophone = false, int maxAlternatives = 3)
    {
        if (_isInitializing)
        {
            Debug.LogError("Initializing in progress!");
            return;
        }
        if (_didInit)
        {
            Debug.LogError("Vosk has already been initialized!");
            return;
        }

        if (!string.IsNullOrEmpty(modelPath))
            ModelPath = modelPath;

        if (keyPhrases != null)
            KeyPhrases = keyPhrases;

        MaxAlternatives = maxAlternatives;
        StartCoroutine(DoStartVoskStt(startMicrophone));
    }

    private IEnumerator DoStartVoskStt(bool startMicrophone)
    {
        _isInitializing = true;
        yield return WaitForMicrophoneInput();

        // Decompress if needed
        yield return Decompress();

        // Load the Vosk model async
        yield return LoadModelAsync();

        OnStatusUpdated?.Invoke("Initialized");
        if (VoiceProcessor != null)
        {
            VoiceProcessor.OnFrameCaptured += VoiceProcessorOnOnFrameCaptured;
            VoiceProcessor.OnRecordingStop += VoiceProcessorOnOnRecordingStop;
            VoiceProcessor.OnRecordingStart += VoiceProcessorOnOnRecordingStart;

            if (startMicrophone)
            {
                _running = true;
                VoiceProcessor.StartRecording();
                Task.Run(ThreadedWork);
            }
        }
        else
        {
            Debug.LogWarning("No VoiceProcessor assigned! Vosk will initialize without microphone input.");
        }

        _isInitializing = false;
        _didInit = true;
    }

    private void UpdateGrammar()
    {
        if (KeyPhrases.Count == 0)
        {
            _grammar = "";
            return;
        }

        JSONArray keywords = new JSONArray();
        foreach (string keyphrase in KeyPhrases)
            keywords.Add(new JSONString(keyphrase.ToLower()));

        keywords.Add(new JSONString("[unk]")); // unknown token
        _grammar = keywords.ToString();
    }

    private IEnumerator Decompress()
    {
        string persistentPath = Application.persistentDataPath;
        string modelFolderName = Path.GetFileNameWithoutExtension(ModelPath);
        string existingPath = Path.Combine(persistentPath, modelFolderName);

        // Already decompressed
        if (!Path.HasExtension(ModelPath) || Directory.Exists(existingPath))
        {
            OnStatusUpdated?.Invoke("Using existing decompressed model.");
            _decompressedModelPath = existingPath;
            yield break;
        }

        OnStatusUpdated?.Invoke("Decompressing model...");
        string dataPath = Path.Combine(Application.streamingAssetsPath, ModelPath);

        byte[] zipBytes = null;
        bool needsBytes = dataPath.Contains("://");
        if (needsBytes)
        {
            UnityWebRequest www = UnityWebRequest.Get(dataPath);
            www.SendWebRequest();
            while (!www.isDone) yield return null;

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Failed to read model zip: " + www.error);
                yield break;
            }
            zipBytes = www.downloadHandler.data;
        }

        Exception decompressError = null;
        Task decompressTask = Task.Run(() =>
        {
            try
            {
                if (needsBytes)
                {
                    using (var ms = new MemoryStream(zipBytes))
                    using (var zipFile = ZipFile.Read(ms))
                        zipFile.ExtractAll(persistentPath);
                }
                else
                {
                    using (var stream = File.OpenRead(dataPath))
                    using (var zipFile = ZipFile.Read(stream))
                        zipFile.ExtractAll(persistentPath);
                }
            }
            catch (Exception ex) { decompressError = ex; }
        });

        while (!decompressTask.IsCompleted)
            yield return null;

        if (decompressError != null)
        {
            Debug.LogError("Error while decompressing Vosk model: " + decompressError);
            yield break;
        }

        _decompressedModelPath = existingPath;
        OnStatusUpdated?.Invoke("Decompressing complete!");
    }

    private IEnumerator LoadModelAsync()
    {
        OnStatusUpdated?.Invoke("Loading Model from: " + _decompressedModelPath);

        Model tempModel = null;
        Exception loadError = null;

        var loadTask = Task.Run(() =>
        {
            try { tempModel = new Model(_decompressedModelPath); }
            catch (Exception ex) { loadError = ex; }
        });

        while (!loadTask.IsCompleted)
            yield return null;

        if (loadError != null)
        {
            Debug.LogError("Failed to load Vosk model: " + loadError);
            yield break;
        }

        _model = tempModel;
        IsModelLoaded = true;

        // Create the recognizer immediately after loading the model
        yield return CreateRecognizer();
    }

    private IEnumerator CreateRecognizer()
    {
        // Don't create recognizer on background thread - do it on main thread
        OnStatusUpdated?.Invoke("Creating recognizer...");
        
        try
        {
            UpdateGrammar();
            
            VoskRecognizer tempRecognizer = string.IsNullOrEmpty(_grammar)
                ? new VoskRecognizer(_model, 16000.0f)
                : new VoskRecognizer(_model, 16000.0f, _grammar);

            tempRecognizer.SetMaxAlternatives(MaxAlternatives);
            
            _recognizer = tempRecognizer;
            _recognizerReady = true;
            Debug.Log("Recognizer ready");
        }
        catch (Exception ex)
        {
            Debug.LogError("Failed to create recognizer: " + ex);
        }
        
        yield return null;
    }

    private IEnumerator WaitForMicrophoneInput()
    {
        while (Microphone.devices.Length <= 0)
            yield return null;
    }

    public void ToggleRecording()
    {
        Debug.Log("Toggle Recording");
        if (!VoiceProcessor.IsRecording)
        {
            Debug.Log("Start Recording");
            _running = true;
            VoiceProcessor.StartRecording();
            Task.Run(ThreadedWork); // run worker on background thread
        }
        else
        {
            Debug.Log("Stop Recording");
            _running = false;
            VoiceProcessor.StopRecording();
        }
    }

    // ✅ UPDATED VERSION (with CSV logging)
    void Update()
    {
        while (_threadedResultQueue.TryDequeue(out string voiceResult))
        {
            // 🔍 Debug raw JSON output from Vosk
            Debug.Log($"🔍 Raw Vosk JSON: {voiceResult}");

            // Extract recognized text
            string recognizedText = "";
            try
            {
                int textIndex = voiceResult.IndexOf("\"text\"");
                if (textIndex >= 0)
                {
                    int start = voiceResult.IndexOf(":", textIndex) + 1;
                    int end = voiceResult.IndexOf("}", start);
                    recognizedText = voiceResult.Substring(start, end - start)
                                                .Replace("\"", "")
                                                .Replace(":", "")
                                                .Trim();
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"⚠️ Failed to parse Vosk text: {ex.Message}");
            }

            // ✅ Log recognized text into CSV
            if (!string.IsNullOrEmpty(recognizedText))
            {
                Debug.Log($"🗣 Recognized: {recognizedText}");
                if (DataLogger.Instance != null)
                    DataLogger.Instance.LogAnswer(recognizedText);
            }

            // Continue to notify UI handlers
            OnTranscriptionResult?.Invoke(voiceResult);
        }
    }

    private void VoiceProcessorOnOnFrameCaptured(short[] samples)
    {
        _threadedBufferQueue.Enqueue(samples);
    }

    private void VoiceProcessorOnOnRecordingStart()
    {
        Debug.Log("Recording Started");
        if (!_running)
        {
            _running = true;
            Task.Run(ThreadedWork);
        }
    }

    private void VoiceProcessorOnOnRecordingStop()
    {
        Debug.Log("Recording Stopped");
        // Just continue recognition - no special handling needed
    }

    // Background worker (no async/await)
    private void ThreadedWork()
    {
        if (!_recognizerReady)
        {
            Debug.LogError("Recognizer not ready!");
            return;
        }

        voskRecognizerReadMarker.Begin();
        while (_running)
        {
            if (_threadedBufferQueue.TryDequeue(out short[] voiceResult))
            {
                if (_recognizer.AcceptWaveform(voiceResult, voiceResult.Length))
                {
                    var result = _recognizer.Result();
                    _threadedResultQueue.Enqueue(result);
                }
            }
            else
            {
                Thread.Sleep(10); // lightweight wait
            }
        }
        voskRecognizerReadMarker.End();
    }
}
