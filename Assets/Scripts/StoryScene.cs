using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewStoryScene", menuName = "Data/New Story Scene")]
[System.Serializable]
public class StoryScene : ScriptableObject
{
    public List<Sentence> sentences;
    [Tooltip("If true, sentences will be played in a random order when this scene is played.")]
    public bool randomizeSentences = false;
    [Tooltip("Number of sentences to use when playing this scene. 0 = use all available sentences.")]
    public int sentencesToUse = 0;
    public Sprite background;
    public StoryScene nextScene;

    [System.Serializable]
    public struct Sentence
    {
        public String text;
        public Speaker speaker;
        [Tooltip("Mark this as true if this sentence is a question that requires an algorithm choice (PPO/DQN)")]
        public bool isQuestion;
    }
}
