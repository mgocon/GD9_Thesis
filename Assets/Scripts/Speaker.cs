using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewSpeaker", menuName = "Data/New Speaker")]
public class Speaker : ScriptableObject
{
    public string speakerName;
    public Color textColor;
}
