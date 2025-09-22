using UnityEngine;

public class VoskManager : MonoBehaviour
{
    public static VoskManager Instance { get; private set; }
    private VoskSpeechToText voskSpeechToText;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        voskSpeechToText = GetComponent<VoskSpeechToText>();
        if (voskSpeechToText == null)
        {
            Debug.LogError("VoskSpeechToText component missing from VoskManager!");
        }
    }

    public VoskSpeechToText GetSpeechToText()
    {
        return voskSpeechToText;
    }
}
