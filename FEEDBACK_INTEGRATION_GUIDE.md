# AI Feedback Integration Guide for Job Interview Training Game

This guide explains how to integrate your trained DQN and PPO models into the Unity game to provide real-time feedback to players.

## 📋 Overview

The integration consists of several components:
1. **Python Export Script** - Converts PyTorch models to ONNX format
2. **Unity ML Components** - Inference engine and feedback system
3. **UI Components** - Display feedback to players
4. **Integration with Game Flow** - Connects with existing interview mechanics

## 🔧 Step 1: Export Models to ONNX

### 1.1 Install Required Python Packages

```powershell
cd "e:\UnityProjects\Thesis\GD9_Thesis\Assets\RL Function"
pip install torch onnx
```

### 1.2 Run the Export Script

```powershell
python export_to_onnx.py
```

This will create:
- `onnx_models/dqn_model.onnx`
- `onnx_models/ppo_model.onnx`

### 1.3 Copy ONNX Models to Unity

```powershell
# Create StreamingAssets folder if it doesn't exist
New-Item -ItemType Directory -Force -Path "e:\UnityProjects\Thesis\GD9_Thesis\Assets\StreamingAssets\MLModels"

# Copy the ONNX models
Copy-Item "onnx_models\*.onnx" "e:\UnityProjects\Thesis\GD9_Thesis\Assets\StreamingAssets\MLModels\"
```

## 🎮 Step 2: Unity Setup

### 2.1 Install Unity Barracuda (ML Inference Engine)

1. Open Unity
2. Go to **Window > Package Manager**
3. Click **+** (top-left) > **Add package by name**
4. Enter: `com.unity.barracuda`
5. Click **Add**

### 2.2 Import ONNX Models in Unity

1. In Unity Project window, navigate to `StreamingAssets/MLModels`
2. Select each `.onnx` file
3. In Inspector, set **Model Type** to `Barracuda`

### 2.3 Create Feedback System GameObject

1. In Scene Hierarchy, create new Empty GameObject: `FeedbackSystem`
2. Add component: `FeedbackManager` (the script we created)
3. Add component: `VoiceAnalyzer` (the script we created)

### 2.4 Create Feedback UI

#### Create UI Canvas:
1. Right-click Hierarchy > **UI > Canvas**
2. Rename to `FeedbackCanvas`
3. Set Canvas Scaler to **Scale With Screen Size**

#### Create Feedback Panel:
1. Right-click FeedbackCanvas > **UI > Panel**
2. Rename to `FeedbackPanel`
3. Set anchors to center-middle
4. Set Width: 600, Height: 400

#### Add UI Elements to FeedbackPanel:

**Title Text:**
- Right-click FeedbackPanel > **UI > Text - TextMeshPro**
- Rename to `FeedbackTitle`
- Position at top, font size: 24, bold

**Message Text:**
- Right-click FeedbackPanel > **UI > Text - TextMeshPro**
- Rename to `FeedbackMessage`
- Position in center, font size: 16, word wrap enabled

**Performance Bars:**
Create 5 sliders (one for each metric):
- Right-click FeedbackPanel > **UI > Slider**
- Create for: Confidence, Clarity, Pace, Tone, Overall
- Set Min Value: 0, Max Value: 1
- Position vertically on right side

**Close Button:**
- Right-click FeedbackPanel > **UI > Button - TextMeshPro**
- Rename to `CloseButton`
- Position at bottom

### 2.5 Configure FeedbackManager

1. Select `FeedbackSystem` GameObject
2. In Inspector, find `FeedbackManager` component
3. Assign references:
   - **DQN Model**: Drag `dqn_model.onnx` from StreamingAssets
   - **PPO Model**: Drag `ppo_model.onnx` from StreamingAssets
   - **Current Model Type**: Select DQN or PPO

### 2.6 Configure FeedbackUI

1. Select `FeedbackPanel` GameObject
2. Add component: `FeedbackUI` (the script we created)
3. Assign references:
   - **Feedback Panel**: Drag FeedbackPanel itself
   - **Feedback Title**: Drag FeedbackTitle text
   - **Feedback Message**: Drag FeedbackMessage text
   - **Performance Bars**: Drag each slider (Confidence, Clarity, Pace, Tone, Overall)
   - **Close Button**: Drag CloseButton

### 2.7 Link to BottomBarController

1. Find your existing `BottomBarController` GameObject in the scene
2. In Inspector, find `BottomBarController` component
3. Assign new references:
   - **Feedback Manager**: Drag `FeedbackSystem`
   - **Feedback UI**: Drag `FeedbackPanel` (with FeedbackUI component)
   - **Auto Generate Feedback**: Check this box

## 🎯 Step 3: How It Works

### Game Flow with Feedback:

1. **Question Displayed**: Player sees interview question
2. **Player Speaks**: Clicks "Speak" button, Vosk records response
3. **Player Clicks "Done"**: Recording stops, popup boxes appear
4. **Player Chooses Algorithm**: 
   - Clicks "DQN" or "PPO" button
   - System analyzes voice/text
   - Selected model generates feedback
   - Feedback UI displays results
5. **Next Question**: Game advances automatically

### Feedback Generation Process:

```
Player Response → Voice Analyzer → Performance Metrics
                                          ↓
                    Speech Features (25D) + Performance (5D) = Observation (30D)
                                          ↓
                              DQN or PPO Model Inference
                                          ↓
                              Feedback Action (0-5)
                                          ↓
                              FeedbackUI Display
```

### Feedback Actions:

- **0**: Encourage Confidence - Boost confidence
- **1**: Improve Speech Pace - Adjust speaking speed  
- **2**: Enhance Clarity - Improve articulation
- **3**: Optimize Tone - Better emotional tone
- **4**: Reduce Nervousness - Calm anxiety
- **5**: Maintain Current Approach - No change needed

## 🔍 Step 4: Testing

### Test Feedback System:

1. **Play Mode Test**:
   - Enter Play mode in Unity
   - Select `FeedbackPanel` in Hierarchy
   - In Inspector, click `Show Test Feedback` button
   - Verify feedback displays correctly

2. **Full Game Test**:
   - Play the interview game
   - Answer a question with voice
   - Click "Done"
   - Choose DQN or PPO
   - Verify feedback appears

### Debug Information:

Check Console for logs:
- ✅ `DQN model loaded successfully`
- ✅ `PPO model loaded successfully`
- 📊 `Feedback Generated (DQN): EncourageConfidence`
- 🎤 `Started recording player response`

## 📊 Step 5: Data Collection

The system automatically logs:
- Algorithm chosen (DQN/PPO)
- Feedback action taken
- Performance metrics
- Model confidence
- Player transcription

Access logs in: `Application.persistentDataPath/InterviewResults.csv`

## ⚙️ Advanced Configuration

### Voice Analyzer Settings:

```csharp
[SerializeField] private bool useSimulation = true;  // Switch to false for real audio analysis
```

Currently uses simulated analysis based on text. To implement real audio analysis:
1. Extract MFCC features from audio
2. Calculate pitch variation
3. Analyze speaking rate
4. Update `RealAnalysis()` method in `VoiceAnalyzer.cs`

### Feedback Display Settings:

```csharp
[SerializeField] private float displayDuration = 5f;    // How long to show feedback
[SerializeField] private bool autoClose = false;        // Auto-hide or manual close
```

### Performance Tuning:

- **Model Selection**: DQN typically provides more stable feedback, PPO is more adaptive
- **Feedback Frequency**: Adjust when feedback is generated (every question vs. every N questions)
- **UI Timing**: Customize fade in/out durations

## 🐛 Troubleshooting

### Models Not Loading:
- Verify `.onnx` files are in `StreamingAssets/MLModels`
- Check Unity Console for error messages
- Ensure Barracuda package is installed

### No Feedback Displayed:
- Verify `FeedbackManager` and `FeedbackUI` are assigned in `BottomBarController`
- Check `autoGenerateFeedback` is enabled
- Look for warning messages in Console

### Wrong Feedback Actions:
- Verify correct model (DQN/PPO) is selected
- Check model file versions match training
- Review performance metrics in debug logs

### VoiceAnalyzer Issues:
- Ensure `VoskSpeechToText` is working
- Check `VoskDialogText` for transcription
- Verify response duration is calculated correctly

## 📝 Next Steps

1. **Enhance Voice Analysis**: Implement real audio feature extraction
2. **Tune Feedback Messages**: Customize messages for your specific interview context
3. **Add Performance Tracking**: Show progress over multiple sessions
4. **Implement Learning**: Adapt feedback based on player improvement
5. **A/B Testing**: Compare DQN vs PPO effectiveness with real users

## 📚 Files Created

### Python Scripts:
- `Assets/RL Function/export_to_onnx.py` - Model export utility

### C# Scripts:
- `Assets/Scripts/InterviewFeedbackData.cs` - Data structures
- `Assets/Scripts/VoiceAnalyzer.cs` - Performance analysis
- `Assets/Scripts/FeedbackManager.cs` - ML inference & feedback generation
- `Assets/Scripts/FeedbackUI.cs` - UI display
- `Assets/Scripts/BottomBarController.cs` - Updated with integration

### Models:
- `Assets/StreamingAssets/MLModels/dqn_model.onnx` - DQN inference model
- `Assets/StreamingAssets/MLModels/ppo_model.onnx` - PPO inference model

## 🎓 Research Notes

This implementation provides:
- **Real-time feedback** during interview practice
- **Comparison** between DQN and PPO approaches
- **Performance metrics** for analysis
- **User study capability** (A/B testing)
- **Data collection** for further research

Perfect for your thesis on "Adaptive Game Dynamics: Comparing Deep Q-Network and Proximal Policy Optimization for Real-Time Feedback in a Job Interview Training Simulation"!

## 🆘 Support

If you encounter issues:
1. Check Unity Console for detailed error messages
2. Verify all components are properly assigned
3. Test models individually before full integration
4. Review Python training logs to ensure models trained correctly

Good luck with your thesis! 🚀
