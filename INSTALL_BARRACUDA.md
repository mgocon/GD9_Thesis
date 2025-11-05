# Installing Unity Barracuda Package

## Method 1: Package Manager (Recommended)

1. **Open Unity Editor**
   - Open your project: `E:\UnityProjects\Thesis\GD9_Thesis`

2. **Open Package Manager**
   - Go to: `Window` → `Package Manager`
   - Or press `Ctrl + 9` (Windows)

3. **Add Package by Name**
   - Click the **`+`** button in the top-left corner of Package Manager
   - Select **"Add package by name..."**

4. **Enter Package Name**
   - Name: `com.unity.barracuda`
   - Click **"Add"**

5. **Wait for Installation**
   - Unity will download and install the package
   - This may take 1-2 minutes

6. **Verify Installation**
   - In Package Manager, search for "Barracuda"
   - Should show as "Installed"
   - Version should be 3.0.0 or higher

## Method 2: Package Manager via Git URL

If Method 1 doesn't work:

1. Open Package Manager (`Window` → `Package Manager`)
2. Click **`+`** → **"Add package from git URL..."**
3. Enter: `com.unity.barracuda`
4. Click **"Add"**

## Method 3: Manual manifest.json Edit

If you prefer editing the manifest directly:

1. **Close Unity Editor** (important!)

2. **Edit Package Manifest**
   - Open: `E:\UnityProjects\Thesis\GD9_Thesis\Packages\manifest.json`
   - Add this line in the "dependencies" section:
   ```json
   "com.unity.barracuda": "3.0.0"
   ```

3. **Example manifest.json:**
   ```json
   {
     "dependencies": {
       "com.unity.barracuda": "3.0.0",
       "com.unity.collab-proxy": "1.x.x",
       "com.unity.ide.visualstudio": "2.x.x",
       ...other packages...
     }
   }
   ```

4. **Reopen Unity**
   - Unity will automatically download and install the package

## Verifying Installation

After installation, verify it worked:

1. **Check Package Manager**
   - Should show "Unity Barracuda" as installed

2. **Check Scripts Compile**
   - Open `Assets/Scripts/FeedbackManager.cs`
   - Errors about `Unity.Barracuda`, `NNModel`, `IWorker`, `Tensor` should be gone

3. **Test in Console**
   - Open Unity Console (`Ctrl + Shift + C`)
   - No errors about missing Barracuda namespace

## Troubleshooting

### "Package not found"
- Make sure you're connected to the internet
- Try using the full version: `com.unity.barracuda@3.0.0`
- Check Unity version is 2020.3 or newer

### "Compilation errors"
- Wait for package to fully download and import
- Try: `Assets` → `Reimport All`
- Restart Unity Editor

### Still not working?
1. Delete `Library` folder (Unity will regenerate it)
2. Reopen project
3. Try installation again

## After Installation

Once Barracuda is installed:

1. **Run Setup Script Again**
   ```powershell
   .\setup_feedback.ps1
   ```

2. **Import ONNX Models**
   - The .onnx files should be in `Assets/StreamingAssets/MLModels/`

3. **Continue with Integration**
   - Follow `INTEGRATION_CHECKLIST.md`

## Quick Test

To test Barracuda is working:

1. Create a test script:
   ```csharp
   using Unity.Barracuda;
   using UnityEngine;
   
   public class BarracudaTest : MonoBehaviour
   {
       void Start()
       {
           Debug.Log("Barracuda is installed!");
       }
   }
   ```

2. If no errors, Barracuda is ready!

---

**Note:** You MUST install Unity Barracuda before the FeedbackManager script will work, as it requires ML inference capabilities.
