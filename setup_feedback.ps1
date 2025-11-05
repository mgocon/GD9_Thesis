# Quick Setup Script for AI Feedback Integration
# Run this from the project root directory

Write-Host "🚀 AI Feedback Integration Quick Setup" -ForegroundColor Cyan
Write-Host "=======================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Check Python and dependencies
Write-Host "Step 1: Checking Python environment..." -ForegroundColor Yellow
try {
    $pythonVersion = python --version 2>&1
    Write-Host "✅ Python found: $pythonVersion" -ForegroundColor Green
} catch {
    Write-Host "❌ Python not found. Please install Python 3.7+" -ForegroundColor Red
    exit 1
}

# Step 2: Install Python dependencies
Write-Host ""
Write-Host "Step 2: Installing Python dependencies..." -ForegroundColor Yellow
$rlFunctionPath = "Assets\RL Function"
Set-Location $rlFunctionPath

Write-Host "Installing torch and onnx..." -ForegroundColor Gray
pip install torch onnx --quiet
if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Dependencies installed" -ForegroundColor Green
} else {
    Write-Host "⚠️ Warning: Some dependencies may not have installed correctly" -ForegroundColor Yellow
}

# Step 3: Export models to ONNX
Write-Host ""
Write-Host "Step 3: Exporting trained models to ONNX format..." -ForegroundColor Yellow

if (Test-Path "saved_models\dqn_best.pth") {
    Write-Host "  Found DQN model: saved_models\dqn_best.pth" -ForegroundColor Gray
} else {
    Write-Host "  ⚠️ Warning: DQN model not found at saved_models\dqn_best.pth" -ForegroundColor Yellow
}

if (Test-Path "saved_models\ppo_best.pth") {
    Write-Host "  Found PPO model: saved_models\ppo_best.pth" -ForegroundColor Gray
} else {
    Write-Host "  ⚠️ Warning: PPO model not found at saved_models\ppo_best.pth" -ForegroundColor Yellow
}

Write-Host "  Running export script..." -ForegroundColor Gray
python export_to_onnx.py

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Models exported to ONNX format" -ForegroundColor Green
} else {
    Write-Host "❌ Export failed. Check error messages above." -ForegroundColor Red
    Set-Location ..\..
    exit 1
}

# Step 4: Copy ONNX models to StreamingAssets
Write-Host ""
Write-Host "Step 4: Copying ONNX models to Unity StreamingAssets..." -ForegroundColor Yellow

$streamingAssetsPath = "..\..\StreamingAssets\MLModels"
if (-not (Test-Path $streamingAssetsPath)) {
    New-Item -ItemType Directory -Force -Path $streamingAssetsPath | Out-Null
    Write-Host "  Created directory: $streamingAssetsPath" -ForegroundColor Gray
}

if (Test-Path "onnx_models\dqn_model.onnx") {
    Copy-Item "onnx_models\dqn_model.onnx" $streamingAssetsPath -Force
    Write-Host "  ✅ Copied dqn_model.onnx" -ForegroundColor Green
} else {
    Write-Host "  ❌ dqn_model.onnx not found" -ForegroundColor Red
}

if (Test-Path "onnx_models\ppo_model.onnx") {
    Copy-Item "onnx_models\ppo_model.onnx" $streamingAssetsPath -Force
    Write-Host "  ✅ Copied ppo_model.onnx" -ForegroundColor Green
} else {
    Write-Host "  ❌ ppo_model.onnx not found" -ForegroundColor Red
}

# Return to project root
Set-Location ..\..

# Step 5: Verify Unity scripts
Write-Host ""
Write-Host "Step 5: Verifying Unity C# scripts..." -ForegroundColor Yellow

$requiredScripts = @(
    "Assets\Scripts\InterviewFeedbackData.cs",
    "Assets\Scripts\VoiceAnalyzer.cs",
    "Assets\Scripts\FeedbackManager.cs",
    "Assets\Scripts\FeedbackUI.cs"
)

$allScriptsPresent = $true
foreach ($script in $requiredScripts) {
    if (Test-Path $script) {
        Write-Host "  ✅ $script" -ForegroundColor Green
    } else {
        Write-Host "  ❌ $script (missing)" -ForegroundColor Red
        $allScriptsPresent = $false
    }
}

# Final summary
Write-Host ""
Write-Host "=======================================" -ForegroundColor Cyan
Write-Host "Setup Summary" -ForegroundColor Cyan
Write-Host "=======================================" -ForegroundColor Cyan

if ($allScriptsPresent -and (Test-Path "Assets\StreamingAssets\MLModels\dqn_model.onnx")) {
    Write-Host ""
    Write-Host "✅ Setup completed successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Next steps in Unity:" -ForegroundColor Yellow
    Write-Host "1. Open Unity Editor" -ForegroundColor White
    Write-Host "2. Install Barracuda package (Window > Package Manager > Add 'com.unity.barracuda')" -ForegroundColor White
    Write-Host "3. Create FeedbackSystem GameObject and add FeedbackManager component" -ForegroundColor White
    Write-Host "4. Create FeedbackUI panel and configure UI elements" -ForegroundColor White
    Write-Host "5. Link components in BottomBarController" -ForegroundColor White
    Write-Host ""
    Write-Host "📖 For detailed instructions, see: FEEDBACK_INTEGRATION_GUIDE.md" -ForegroundColor Cyan
    Write-Host ""
} else {
    Write-Host ""
    Write-Host "⚠️ Setup completed with warnings" -ForegroundColor Yellow
    Write-Host "   Please check the messages above and resolve any issues." -ForegroundColor Yellow
    Write-Host ""
}

Write-Host "Models location: Assets\StreamingAssets\MLModels\" -ForegroundColor Gray
Write-Host "Documentation: FEEDBACK_INTEGRATION_GUIDE.md" -ForegroundColor Gray
Write-Host ""
