# Quick Fix: Install Unity Barracuda Package

## The Problem
The `FeedbackManager.cs` script requires Unity Barracuda package, which isn't installed yet. This causes compilation errors.

## The Solution (Choose One)

### Option A: Install Barracuda First (RECOMMENDED - 2 minutes)

**Steps:**

1. **Open Unity Editor**
   ```
   Open your project: E:\UnityProjects\Thesis\GD9_Thesis
   ```

2. **Install Barracuda Package**
   - In Unity, go to: **Window → Package Manager**
   - Click the **[+]** button (top-left)
   - Select **"Add package by name..."**
   - Type: `com.unity.barracuda`
   - Click **"Add"**
   - Wait 1-2 minutes for installation

3. **Verify Installation**
   - In Package Manager, search for "Barracuda"
   - Should show as "Installed" with version 3.0.0+

4. **Check Scripts Compile**
   - All errors in FeedbackManager.cs should disappear
   - Console should be clean

✅ **Done!** Now you can proceed with the integration.

---

### Option B: Temporarily Disable FeedbackManager (If you want to use Unity before installing Barracuda)

If you need to open Unity immediately without installing Barracuda:

1. **Rename the file temporarily:**
   ```powershell
   Rename-Item "Assets\Scripts\FeedbackManager.cs" "Assets\Scripts\FeedbackManager.cs.txt"
   ```

2. **Open Unity** - No errors now

3. **When ready to use feedback:**
   - Install Barracuda (Option A above)
   - Rename back:
   ```powershell
   Rename-Item "Assets\Scripts\FeedbackManager.cs.txt" "Assets\Scripts\FeedbackManager.cs"
   ```

---

## Install Command (Alternative Method)

If Package Manager doesn't work, edit the manifest directly:

```powershell
# Close Unity first!

# Add Barracuda to manifest
$manifestPath = "Packages\manifest.json"
$manifest = Get-Content $manifestPath -Raw | ConvertFrom-Json
$manifest.dependencies | Add-Member -Name "com.unity.barracuda" -Value "3.0.0" -MemberType NoteProperty -Force
$manifest | ConvertTo-Json -Depth 10 | Set-Content $manifestPath

# Reopen Unity - package will auto-install
```

---

## After Installation

Once Barracuda is installed:

1. **Verify no errors in Console**
2. **Continue with setup:**
   ```powershell
   .\setup_feedback.ps1
   ```
3. **Follow integration checklist:**
   - See `INTEGRATION_CHECKLIST.md`

---

## Why Barracuda is Needed

Unity Barracuda is Unity's neural network inference library. It allows you to:
- Load ONNX models in Unity
- Run ML inference on CPU/GPU
- Get real-time predictions

Without it, your trained DQN/PPO models cannot be used in Unity.

---

## Troubleshooting

**"Package not found"**
- Check internet connection
- Try full version: `com.unity.barracuda@3.0.0`
- Ensure Unity version is 2020.3+

**"Still getting errors"**
- Make sure package shows as "Installed" in Package Manager
- Try: **Assets → Reimport All**
- Restart Unity Editor

**"Can't find Package Manager"**
- Menu bar: **Window → Package Manager**
- Or press: **Ctrl + 9**

---

## Quick Summary

**Fastest path:**
1. Open Unity
2. Window → Package Manager
3. [+] → Add package by name → `com.unity.barracuda`
4. Wait for installation
5. Done! ✅

**That's it!** The FeedbackManager will work once Barracuda is installed.
