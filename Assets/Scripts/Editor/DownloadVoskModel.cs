using UnityEditor;
using UnityEngine;
using System.IO;
using System.Net;

[InitializeOnLoad]
public static class DownloadVoskModel
{
    static string modelPath = "Assets/StreamingAssets/vosk-model-en-us-0.42-gigaspeech.zip";
    static string modelUrl = "https://alphacephei.com/vosk/models/vosk-model-en-us-0.42-gigaspeech.zip";

    static DownloadVoskModel()
    {
        // Run only in the editor, not in builds
        if (!File.Exists(modelPath))
        {
            bool download = EditorUtility.DisplayDialog(
                "Vosk Model Missing",
                "The Vosk speech model is missing.\nDo you want to download it now?",
                "Yes", "No"
            );

            if (download)
            {
                DownloadFile(modelUrl, modelPath);
                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog("Download Complete", "Vosk model downloaded successfully!", "OK");
            }
        }
    }

    static void DownloadFile(string url, string outputPath)
    {
        // Ensure StreamingAssets folder exists
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

        using (WebClient client = new WebClient())
        {
            client.DownloadFile(url, outputPath);
        }
    }
}
