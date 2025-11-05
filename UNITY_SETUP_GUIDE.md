# 🎮 Unity Setup Guide - AI Feedback System

## Quick Start Setup (15 minutes)

Follow these steps in order to integrate the feedback system into your Unity project.

---

## Step 1: Open Unity & Verify No Errors ✅

1. **Open Unity Editor** with your project: `E:\UnityProjects\Thesis\GD9_Thesis`
2. **Check Console** (Ctrl + Shift + C)
   - Should see: `⚠️ FeedbackManager: STUB VERSION - Install Unity Barracuda package for full ML functionality!`
   - This is normal! It means stub version is working
   - No RED errors should appear

---

## Step 2: Create Feedback System GameObject 🤖

### 2.1 Create Empty GameObject
1. In **Hierarchy**, right-click → **Create Empty**
2. Rename it to: **`FeedbackSystem`**
3. Position: (0, 0, 0) - doesn't matter, it's not visible

### 2.2 Add FeedbackManager Component
1. Select **`FeedbackSystem`** in Hierarchy
2. In **Inspector**, click **Add Component**
3. Search for: **`Feedback Manager`**
4. Click to add it

### 2.3 Add VoiceAnalyzer Component
1. With **`FeedbackSystem`** still selected
2. Click **Add Component** again
3. Search for: **`Voice Analyzer`**
4. Click to add it

### 2.4 Configure Settings
In Inspector, you should see:

**Feedback Manager:**
- ⚠️ Show Warning: ✓ (checked)
- Current Model Type: **DQN** (or PPO, your choice)
- Track Performance: ✓ (checked)
- Max History Size: **8**

**Voice Analyzer:**
- Use Simulation: ✓ (checked)
- Analysis Threshold: **0.1**

✅ **Leave FeedbackSystem as DontDestroyOnLoad** (the script handles this)

---

## Step 3: Create Feedback UI Canvas 🎨

### 3.1 Create Canvas
1. Right-click **Hierarchy** → **UI → Canvas**
2. Rename to: **`FeedbackCanvas`**
3. Select it, in Inspector:
   - **Canvas Scaler** component:
     - UI Scale Mode: **Scale With Screen Size**
     - Reference Resolution: **1920 x 1080**
     - Match: **0.5** (halfway between width and height)

### 3.2 Create Feedback Panel
1. Right-click **FeedbackCanvas** → **UI → Panel**
2. Rename to: **`FeedbackPanel`**
3. In **Inspector → Rect Transform**:
   - Anchor Presets: Click the square in top-left, hold **Alt+Shift**, click **center-middle**
   - Pos X: **0**, Pos Y: **0**
   - Width: **700**, Height: **550**
4. **Background color**: Change to dark semi-transparent (R:0, G:0, B:0, A:200)

### 3.3 Add Canvas Group (for fading)
1. With **FeedbackPanel** selected
2. Click **Add Component**
3. Search: **Canvas Group**
4. Add it (no settings to change)

### 3.4 Add FeedbackUI Script
1. With **FeedbackPanel** selected
2. Click **Add Component**
3. Search: **Feedback UI**
4. Add it (we'll configure it later)

---

## Step 4: Create UI Elements Inside Panel 📝

### 4.1 Create Title Text
1. Right-click **FeedbackPanel** → **UI → Text - TextMeshPro**
   - (If prompted to import TMP Essentials, click "Import TMP Essentials")
2. Rename to: **`FeedbackTitle`**
3. **Rect Transform**:
   - Anchor: **Top-Center**
   - Pos X: **0**, Pos Y: **-50**
   - Width: **600**, Height: **60**
4. **TextMeshProUGUI Component**:
   - Text: "Feedback Title" (placeholder)
   - Font Style: **Bold**
   - Font Size: **32**
   - Alignment: **Center** (both horizontal and vertical)
   - Color: **White** or **Yellow**

### 4.2 Create Message Text
1. Right-click **FeedbackPanel** → **UI → Text - TextMeshPro**
2. Rename to: **`FeedbackMessage`**
3. **Rect Transform**:
   - Anchor: **Center**
   - Pos X: **0**, Pos Y: **50**
   - Width: **600**, Height: **250**
4. **TextMeshProUGUI Component**:
   - Text: "Feedback message will appear here..." (placeholder)
   - Font Size: **18**
   - Alignment: **Top-Left**
   - Color: **White**
   - **Wrapping**: Enabled ✓
   - **Overflow**: Truncate

### 4.3 Create Performance Info Text
1. Right-click **FeedbackPanel** → **UI → Text - TextMeshPro**
2. Rename to: **`PerformanceText`**
3. **Rect Transform**:
   - Anchor: **Bottom-Left**
   - Pos X: **50**, Pos Y: **50**
   - Width: **300**, Height: **80**
4. **TextMeshProUGUI Component**:
   - Text: "Performance: --%" (placeholder)
   - Font Size: **14**
   - Alignment: **Top-Left**
   - Color: **Light Gray**

---

## Step 5: Create Performance Sliders 📊

For each of these 5 metrics, repeat the following:

**Metrics to create:**
1. Confidence
2. Clarity
3. Pace
4. Tone
5. Overall

### For Each Metric:

#### 5.1 Create Slider
1. Right-click **FeedbackPanel** → **UI → Slider**
2. Rename to: **`[Metric]Bar`** (e.g., "ConfidenceBar")

#### 5.2 Configure Slider Position
Use these Y positions for vertical layout:

| Metric | Y Position |
|--------|-----------|
| Confidence | -120 |
| Clarity | -160 |
| Pace | -200 |
| Tone | -240 |
| Overall | -280 |

**For each slider:**
- Anchor: **Right-Center**
- Pos X: **-200**
- Pos Y: **[see table above]**
- Width: **300**, Height: **25**

#### 5.3 Configure Slider Settings
In **Inspector → Slider Component**:
- Min Value: **0**
- Max Value: **1**
- Whole Numbers: **Unchecked** ✗
- Value: **0.5** (default)
- **Interactable: Unchecked** ✗ (important! read-only)

#### 5.4 Style the Slider
1. Expand the slider in Hierarchy:
   - Slider → Background → Change color to dark gray
   - Slider → Fill Area → Fill → Change color to:
     - **Green** for good performance
     - (FeedbackUI script will change colors dynamically)

#### 5.5 Add Label for Each Slider
1. Right-click the **Slider** → **UI → Text - TextMeshPro**
2. Rename to: **`[Metric]Label`** (e.g., "ConfidenceLabel")
3. **Rect Transform**:
   - Anchor: **Left-Center**
   - Pos X: **-320**, Pos Y: **0**
   - Width: **100**, Height: **25**
4. **TextMeshProUGUI**:
   - Text: "Confidence:" (or appropriate label)
   - Font Size: **16**
   - Alignment: **Right**
   - Color: **White**

---

## Step 6: Create Close/Continue Button 🔘

1. Right-click **FeedbackPanel** → **UI → Button - TextMeshPro**
2. Rename to: **`CloseButton`**
3. **Rect Transform**:
   - Anchor: **Bottom-Center**
   - Pos X: **0**, Pos Y: **30**
   - Width: **200**, Height: **50**
4. **Button Component**:
   - Colors: Choose nice hover/press colors
5. Expand **CloseButton** → Find **Text (TMP)** child:
   - Text: **"Continue"**
   - Font Size: **20**
   - Alignment: **Center**
   - Color: **White**

---

## Step 7: Link Everything in FeedbackUI Component 🔗

1. Select **`FeedbackPanel`** in Hierarchy
2. In **Inspector**, find **Feedback UI** component
3. **Drag and drop** each element:

**UI Elements:**
- Feedback Panel: Drag **`FeedbackPanel`** (itself)
- Feedback Title: Drag **`FeedbackTitle`**
- Feedback Message: Drag **`FeedbackMessage`**
- Performance Text: Drag **`PerformanceText`**
- Close Button: Drag **`CloseButton`**

**Performance Bars:**
- Confidence Bar: Drag **`ConfidenceBar`**
- Clarity Bar: Drag **`ClarityBar`**
- Pace Bar: Drag **`PaceBar`**
- Tone Bar: Drag **`ToneBar`**
- Overall Bar: Drag **`OverallBar`**

**Display Settings:**
- Display Duration: **5** (seconds)
- Auto Close: **Unchecked** ✗ (let player close manually)

**Colors:**
- Excellent Color: **Green** (#00FF00)
- Good Color: **Yellow** (#FFFF00)
- Needs Improvement Color: **Red** (#FF0000)

---

## Step 8: Hide Feedback Panel Initially 🙈

1. Select **`FeedbackPanel`** in Hierarchy
2. At the **top of Inspector**, find the checkbox next to the name
3. **Uncheck it** to deactivate the panel
4. The panel should now be invisible in Scene view

---

## Step 9: Link to BottomBarController 🎮

1. Find your existing **`BottomBarController`** GameObject in the scene
   - (Usually attached to your UI bottom bar)
2. Select it, in **Inspector** find **Bottom Bar Controller** component
3. Find the new fields and assign:

**AI Feedback Integration:**
- Feedback Manager: Drag **`FeedbackSystem`** from Hierarchy
- Feedback UI: Drag **`FeedbackPanel`** from Hierarchy
- Auto Generate Feedback: **✓ Check this**

---

## Step 10: Test the Setup 🧪

### 10.1 Test Feedback Display
1. **Enter Play Mode** (Ctrl + P)
2. Select **`FeedbackPanel`** in Hierarchy
3. In **Inspector → Feedback UI** component
4. Find the button: **Show Test Feedback**
5. Click it in Play Mode
6. **Verify:**
   - ✅ Panel appears with animation
   - ✅ Title shows: "Build Your Confidence"
   - ✅ Message appears
   - ✅ Performance bars show values
   - ✅ Colors change based on performance
   - ✅ "Continue" button works

### 10.2 Test Full Game Flow
1. **Exit and re-enter Play Mode**
2. Start your interview game
3. Get to a question
4. Click **"Speak"** button
5. Speak (or simulate speech)
6. Click **"Done"** button
7. **Popup boxes should appear with DQN/PPO choice**
8. Click **"DQN"** or **"PPO"**
9. **Verify:**
   - ✅ Feedback panel appears
   - ✅ Performance metrics shown
   - ✅ Appropriate feedback message
   - ✅ Console shows: `📊 Feedback Generated (STUB - DQN): [Action]`
10. Click **"Continue"**
11. **Verify:**
    - ✅ Panel fades out
    - ✅ Game advances to next question

---

## Step 11: Check Console Logs 📋

During testing, Console should show:
```
⚠️ FeedbackManager: STUB VERSION - Install Unity Barracuda package for full ML functionality!
🎤 Started recording player response
📝 Captured transcription: [your speech text]
⚠️ Using stub feedback - install Barracuda for real ML inference
📊 Feedback Generated (STUB - DQN): EncourageConfidence
   Performance: Confidence: 0.65, Clarity: 0.72, ...
```

✅ These warnings are **normal** for stub version!

---

## Step 12: Save Everything 💾

1. **File → Save** (Ctrl + S)
2. **File → Save Scene**
3. Make sure all your changes are saved

---

## Common Issues & Solutions 🔧

### "FeedbackManager not found"
- Make sure `FeedbackSystem` GameObject exists
- Check that FeedbackManager component is attached

### "Feedback panel doesn't appear"
- Verify panel is initially **deactivated** (unchecked)
- Check FeedbackUI references are all assigned
- Make sure Canvas is in **Screen Space - Overlay** mode

### "No feedback after choosing algorithm"
- Check `autoGenerateFeedback` is enabled in BottomBarController
- Verify FeedbackManager and FeedbackUI are linked
- Check Console for error messages

### "Button doesn't work"
- Make sure Button has an EventSystem in scene
- Verify CloseButton is assigned in FeedbackUI

### "Can't see UI in Game view"
- Check Canvas Render Mode is **Screen Space - Overlay**
- Verify Rect Transforms are using correct anchors
- Check Camera settings if using World Space

---

## What's Next? 🚀

### Current State (Stub Version):
✅ Feedback system working with rule-based logic
✅ UI displays properly
✅ Game flow integrated
⚠️ Not using trained DQN/PPO models yet

### To Upgrade to Full ML Version:

**When ready for real ML inference:**

1. **Install Unity Barracuda:**
   ```
   Window > Package Manager
   [+] > Add package by name
   Type: com.unity.barracuda
   Click: Add
   ```

2. **Export your models:**
   ```powershell
   cd "Assets\RL Function"
   python export_to_onnx.py
   ```

3. **Copy models to Unity:**
   ```powershell
   Copy-Item "onnx_models\*.onnx" "..\..\StreamingAssets\MLModels\"
   ```

4. **Get the full FeedbackManager:**
   - Contact me and I'll provide the full version
   - Or check the disabled file once Barracuda is installed

---

## Hierarchy Structure Reference 📋

Your final hierarchy should look like:

```
Scene
├── FeedbackSystem
│   ├── FeedbackManager (script)
│   └── VoiceAnalyzer (script)
│
├── FeedbackCanvas
│   └── FeedbackPanel
│       ├── FeedbackUI (script)
│       ├── Canvas Group (component)
│       ├── FeedbackTitle (Text)
│       ├── FeedbackMessage (Text)
│       ├── PerformanceText (Text)
│       ├── ConfidenceBar (Slider)
│       │   └── ConfidenceLabel (Text)
│       ├── ClarityBar (Slider)
│       │   └── ClarityLabel (Text)
│       ├── PaceBar (Slider)
│       │   └── PaceLabel (Text)
│       ├── ToneBar (Slider)
│       │   └── ToneLabel (Text)
│       ├── OverallBar (Slider)
│       │   └── OverallLabel (Text)
│       └── CloseButton (Button)
│           └── Text (TMP)
│
└── [Your existing game objects]
    └── BottomBarController (with updated references)
```

---

## Quick Checklist ✅

Before testing, verify:
- [ ] FeedbackSystem GameObject created with both components
- [ ] FeedbackCanvas created with proper Canvas Scaler
- [ ] FeedbackPanel created and initially deactivated
- [ ] All 5 performance sliders created and positioned
- [ ] All text elements created (Title, Message, Performance, Labels)
- [ ] CloseButton created
- [ ] FeedbackUI component has all references assigned
- [ ] BottomBarController has FeedbackManager and FeedbackUI linked
- [ ] Test button shows feedback correctly
- [ ] Full game flow works end-to-end

---

## You're Done! 🎉

You now have a working AI feedback system integrated into your job interview training game!

**Test it thoroughly, then prepare for the ML upgrade when ready!**

For questions, check:
- `FEEDBACKMANAGER_FIX.md` - Current status
- `INTEGRATION_CHECKLIST.md` - Detailed checklist
- `AI_FEEDBACK_SUMMARY.md` - System overview
