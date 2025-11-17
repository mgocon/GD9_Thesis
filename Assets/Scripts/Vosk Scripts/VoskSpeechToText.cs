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

    private bool isVoskInitialized = false;
    private bool initializationFailed = false;

    // ✅ Add these public properties so GameManager can check
    public bool IsInitializationComplete => _didInit;
    public bool HasInitializationFailed => initializationFailed;

    private IEnumerator Start()
    {
        // First ensure model exists with validation
        yield return StartCoroutine(EnsureModelExists());

        // Only proceed if model exists and is valid
        if (!initializationFailed)
        {
            if (AutoStart)
            {
                StartVoskStt();
            }
        }
        else
        {
            Debug.LogError("⛔ Speech recognition is disabled due to model initialization failure.");
            
            // ✅ Mark initialization as complete even if it failed
            OnStatusUpdated?.Invoke("InitializationFailed");
            _isInitializing = false;
            _didInit = true;
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

        // Set the decompressed model path
        string modelFolderName = Path.GetFileNameWithoutExtension(ModelPath);
        _decompressedModelPath = Path.Combine(Application.persistentDataPath, "VoskModels", modelFolderName);

        // Load the Vosk model async
        yield return LoadModelAsync();

        // ✅ Always mark as complete, even on failure
        _isInitializing = false;
        _didInit = true;

        if (initializationFailed)
        {
            OnStatusUpdated?.Invoke("InitializationFailed");
            Debug.LogWarning("⚠️ Vosk failed to initialize. Game will continue without voice recognition.");
            yield break;
        }

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

    private IEnumerator LoadModelAsync()
    {
        if (initializationFailed)
        {
            Debug.LogError("⛔ Cannot load model - initialization failed");
            yield break;
        }

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
            Debug.LogError("❌ Failed to load Vosk model: " + loadError);
            initializationFailed = true;
            yield break;
        }

        _model = tempModel;
        IsModelLoaded = true;

        // Create the recognizer immediately after loading the model
        yield return CreateRecognizerInternal();
    }

    private IEnumerator CreateRecognizerInternal()
    {
        if (initializationFailed)
        {
            Debug.LogError("⛔ Cannot create recognizer - initialization failed");
            yield break;
        }

        OnStatusUpdated?.Invoke("Creating recognizer...");
        
        try
        {
            UpdateGrammar();
            
            VoskRecognizer tempRecognizer = string.IsNullOrEmpty(_grammar)
                ? new VoskRecognizer(_model, 16000.0f)
                : new VoskRecognizer(_model, 16000.0f, _grammar);

            tempRecognizer.SetMaxAlternatives(MaxAlternatives);
            tempRecognizer.SetWords(true);
            
            _recognizer = tempRecognizer;
            _recognizerReady = true;
            isVoskInitialized = true;
            
            Debug.Log("✅ Recognizer ready and initialized successfully");
        }
        catch (Exception ex)
        {
            Debug.LogError("❌ Failed to create recognizer: " + ex);
            initializationFailed = true;
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
            Task.Run(ThreadedWork);
        }
        else
        {
            Debug.Log("Stop Recording");
            _running = false;
            VoiceProcessor.StopRecording();
        }
    }

    void Update()
    {
        if (!isVoskInitialized || _recognizer == null)
        {
            return;
        }

        while (_threadedResultQueue.TryDequeue(out string voiceResult))
        {
            Debug.Log($"🔍 Raw Vosk JSON: {voiceResult}");

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

            if (!string.IsNullOrEmpty(recognizedText))
            {
                Debug.Log($"🗣 Recognized: {recognizedText}");
                if (DataLogger.Instance != null)
                    DataLogger.Instance.LogAnswer(recognizedText);
            }

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
    }

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
                Thread.Sleep(10);
            }
        }
        voskRecognizerReadMarker.End();
    }

    private bool IsModelValid(string modelPath)
    {
        if (!Directory.Exists(modelPath))
        {
            Debug.LogError($"❌ Model directory does not exist: {modelPath}");
            return false;
        }
        
        string[] requiredFiles = {
            "am/final.mdl",
            "conf/mfcc.conf", 
            "conf/model.conf",
            "graph/HCLG.fst",
            "graph/phones/word_boundary.int"
        };
        
        foreach (string file in requiredFiles)
        {
            string fullPath = Path.Combine(modelPath, file);
            if (!File.Exists(fullPath))
            {
                Debug.LogWarning($"⚠️ Model incomplete - missing required file: {file}");
                return false;
            }
        }
        
        string finalMdl = Path.Combine(modelPath, "am/final.mdl");
        if (new FileInfo(finalMdl).Length < 1000000)
        {
            Debug.LogWarning("⚠️ Model appears corrupted - final.mdl is too small");
            return false;
        }
        
        Debug.Log("✅ Model validation passed - all required files present");
        return true;
    }

    private bool CheckDiskSpace(string path, long requiredBytes)
    {
        try
        {
            string rootPath = Path.GetPathRoot(path);
            if (string.IsNullOrEmpty(rootPath))
            {
                rootPath = Path.GetPathRoot(Application.persistentDataPath);
            }
            
            DriveInfo drive = new DriveInfo(rootPath);
            long availableSpace = drive.AvailableFreeSpace;
            long requiredMB = requiredBytes / (1024 * 1024);
            long availableMB = availableSpace / (1024 * 1024);
            
            if (availableSpace < requiredBytes)
            {
                Debug.LogError($"❌ Insufficient disk space. Required: {requiredMB}MB, Available: {availableMB}MB");
                return false;
            }
            
            Debug.Log($"✅ Disk space check passed. Available: {availableMB}MB, Required: {requiredMB}MB");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Error checking disk space: {e.Message}");
            return false;
        }
    }

    private IEnumerator EnsureModelExists()
    {
        string modelFolderName = Path.GetFileNameWithoutExtension(ModelPath);
        string modelPath = Path.Combine(Application.persistentDataPath, "VoskModels", modelFolderName);
        string zipPath = Path.Combine(Application.streamingAssetsPath, ModelPath);
        
        Debug.Log($"Checking for model at: {modelPath}");
        
        if (Directory.Exists(modelPath))
        {
            if (IsModelValid(modelPath))
            {
                Debug.Log("✅ Model already decompressed and valid");
                _decompressedModelPath = modelPath;
                yield break;
            }
            else
            {
                Debug.LogWarning("⚠️ Existing model is incomplete or corrupted. Deleting and re-extracting...");
                try
                {
                    Directory.Delete(modelPath, true);
                }
                catch (Exception e)
                {
                    Debug.LogError($"❌ Failed to delete corrupted model: {e.Message}");
                    initializationFailed = true;
                    yield break;
                }
            }
        }
        
        bool needsWebRequest = zipPath.Contains("://");
        
        if (!needsWebRequest && !File.Exists(zipPath))
        {
            Debug.LogError($"❌ CRITICAL: Model zip file not found at: {zipPath}");
            Debug.LogError("❌ Please ensure the Vosk model is included in StreamingAssets before building.");
            initializationFailed = true;
            yield break;
        }
        
        if (!CheckDiskSpace(modelPath, 2L * 1024 * 1024 * 1024))
        {
            Debug.LogError("❌ Not enough disk space to extract Vosk model. Please free up at least 2GB.");
            initializationFailed = true;
            yield break;
        }
        
        Debug.Log("Decompressing model...");
        Debug.Log($"Decompressing model from: {zipPath}");
        
        byte[] zipBytes = null;
        
        if (needsWebRequest)
        {
            UnityWebRequest www = UnityWebRequest.Get(zipPath);
            www.SendWebRequest();
            while (!www.isDone) yield return null;

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("❌ Failed to read model zip: " + www.error);
                initializationFailed = true;
                yield break;
            }
            zipBytes = www.downloadHandler.data;
        }

        Exception decompressError = null;
        string persistentPath = Path.Combine(Application.persistentDataPath, "VoskModels");
        
        if (!Directory.Exists(persistentPath))
        {
            Directory.CreateDirectory(persistentPath);
        }

        Task decompressTask = Task.Run(() =>
        {
            try
            {
                if (needsWebRequest)
                {
                    using (var ms = new MemoryStream(zipBytes))
                    using (var zipFile = ZipFile.Read(ms))
                        zipFile.ExtractAll(persistentPath, ExtractExistingFileAction.OverwriteSilently);
                }
                else
                {
                    using (var stream = File.OpenRead(zipPath))
                    using (var zipFile = ZipFile.Read(stream))
                        zipFile.ExtractAll(persistentPath, ExtractExistingFileAction.OverwriteSilently);
                }
            }
            catch (Exception ex) 
            { 
                decompressError = ex; 
            }
        });

        while (!decompressTask.IsCompleted)
            yield return null;

        if (decompressError != null)
        {
            Debug.LogError($"❌ Error decompressing model: {decompressError.Message}");
            
            if (decompressError.Message.Contains("112") || decompressError.Message.Contains("disk"))
            {
                Debug.LogError("❌ Disk full error detected. Please free up disk space and try again.");
            }
            
            initializationFailed = true;
            
            if (Directory.Exists(modelPath))
            {
                try { Directory.Delete(modelPath, true); } catch { }
            }
            yield break;
        }

        if (!IsModelValid(modelPath))
        {
            Debug.LogError("❌ Extracted model is invalid or incomplete");
            initializationFailed = true;
            yield break;
        }

        _decompressedModelPath = modelPath;
        Debug.Log($"✅ Model decompressed and validated successfully at: {modelPath}");
    }

    private void OnAudioFilterRead(float[] data, int channels)
    {
        if (!isVoskInitialized || _recognizer == null)
        {
            return;
        }
    }
}
