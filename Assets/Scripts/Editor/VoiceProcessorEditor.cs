using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(VoiceProcessor))]
public class VoiceProcessorEditor : Editor
{
    private string[] microphones;
    private int selectedIndex = 0;

    void OnEnable()
    {
        microphones = Microphone.devices;
    }

    public override void OnInspectorGUI()
    {
        VoiceProcessor voiceProcessor = (VoiceProcessor)target;
        
        // Get the current selected device
        SerializedProperty selectedDeviceProp = serializedObject.FindProperty("selectedDevice");
        string currentDevice = selectedDeviceProp.stringValue;

        // Find current index
        selectedIndex = 0;
        for (int i = 0; i < microphones.Length; i++)
        {
            if (microphones[i] == currentDevice)
            {
                selectedIndex = i;
                break;
            }
        }

        // Create the popup
        EditorGUI.BeginChangeCheck();
        selectedIndex = EditorGUILayout.Popup("Microphone Device", selectedIndex, microphones);
        if (EditorGUI.EndChangeCheck())
        {
            selectedDeviceProp.stringValue = microphones[selectedIndex];
            serializedObject.ApplyModifiedProperties();
        }

        // Draw rest of inspector
        DrawDefaultInspector();
    }
}