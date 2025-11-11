using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Represents the performance metrics extracted from player's voice/response
/// </summary>
[Serializable]
public class InterviewPerformance
{
    [Range(0f, 1f)] public float confidence = 0.5f;      // Voice confidence level
    [Range(0f, 1f)] public float clarity = 0.5f;         // Speech clarity
    [Range(0f, 1f)] public float pace = 0.5f;            // Speaking pace appropriateness
    [Range(0f, 1f)] public float tone = 0.5f;            // Emotional tone quality
    [Range(0f, 1f)] public float overall = 0.5f;         // Overall performance

    public float[] ToArray()
    {
        return new float[] { confidence, clarity, pace, tone, overall };
    }

    public void FromArray(float[] values)
    {
        if (values.Length >= 5)
        {
            confidence = Mathf.Clamp01(values[0]);
            clarity = Mathf.Clamp01(values[1]);
            pace = Mathf.Clamp01(values[2]);
            tone = Mathf.Clamp01(values[3]);
            overall = Mathf.Clamp01(values[4]);
        }
    }

    public override string ToString()
    {
        return $"Confidence: {confidence:F2}, Clarity: {clarity:F2}, Pace: {pace:F2}, Tone: {tone:F2}, Overall: {overall:F2}";
    }

    // Calculate improvement from another performance
    public InterviewPerformance GetImprovement(InterviewPerformance previous)
    {
        return new InterviewPerformance
        {
            confidence = this.confidence - previous.confidence,
            clarity = this.clarity - previous.clarity,
            pace = this.pace - previous.pace,
            tone = this.tone - previous.tone,
            overall = this.overall - previous.overall
        };
    }
}

/// <summary>
/// Feedback action types that match the trained models
/// </summary>
public enum FeedbackAction
{
    // Core Communication
    EncourageConfidence = 0,      // Boost confidence
    ImproveSpeechPace = 1,        // Adjust speaking speed
    EnhanceClarity = 2,           // Improve articulation
    OptimizeTone = 3,             // Better emotional tone
    ReduceNervousness = 4,        // Calm anxiety
    
    // Advanced Communication
    ImproveBodyLanguage = 5,      // Posture, gestures, presence
    AddMoreDetails = 6,           // Provide specific examples
    BeMoreConcise = 7,            // Reduce rambling
    ShowMoreEnthusiasm = 8,       // Energy and passion
    StructureAnswersBetter = 9,   // Use STAR method
    
    // Professional Skills
    HighlightAchievements = 10,   // Emphasize accomplishments
    DemonstrateLeadership = 11,   // Show leadership qualities
    ShowProblemSolving = 12,      // Display analytical skills
    ExpressCuriosity = 13,        // Ask thoughtful questions
    
    // Interpersonal
    BuildRapport = 14,            // Connect with interviewer
    ListenMoreActively = 15,      // Better engagement
    MatchInterviewerEnergy = 16,  // Mirror communication style
    
    // Positive Feedback
    MaintainCurrentApproach = 17, // Keep doing what you're doing
    ExcellentPerformance = 18     // Outstanding work
}

/// <summary>
/// Feedback message with details for UI display
/// </summary>
[Serializable]
public class FeedbackMessage
{
    public FeedbackAction action;
    public string title;
    public string message;
    public float confidence;  // Model confidence in this feedback (0-1)
    public InterviewPerformance currentPerformance;
    public InterviewPerformance expectedImprovement;

    // Configuration for message generation
    public static bool UseGenericFeedback = true;  // Toggle between generic and interview-specific feedback

    public static FeedbackMessage Create(FeedbackAction action, float confidence, 
        InterviewPerformance current, InterviewPerformance improvement)
    {
        var feedback = new FeedbackMessage
        {
            action = action,
            confidence = confidence,
            currentPerformance = current,
            expectedImprovement = improvement
        };

        // Generate dynamic, context-aware messages
        switch (action)
        {
            case FeedbackAction.EncourageConfidence:
                feedback.title = UseGenericFeedback ? "Improve Confidence" : "Build Your Confidence";
                feedback.message = GenerateConfidenceMessage(current);
                break;

            case FeedbackAction.ImproveSpeechPace:
                feedback.title = UseGenericFeedback ? "Adjust Pace" : "Adjust Your Pace";
                feedback.message = GeneratePaceMessage(current);
                break;
                
            case FeedbackAction.SlowDownPacing:
                feedback.title = "Slow Down";
                feedback.message = GenerateSlowDownMessage(current);
                break;
                
            case FeedbackAction.SpeedUpPacing:
                feedback.title = "Speed Up";
                feedback.message = GenerateSpeedUpMessage(current);

            case FeedbackAction.EnhanceClarity:
                feedback.title = "Improve Clarity";
                feedback.message = GenerateClarityMessage(current);
                break;

            case FeedbackAction.OptimizeTone:
                feedback.title = "Optimize Your Tone";
                feedback.message = GenerateToneMessage(current);
                break;

            case FeedbackAction.ReduceNervousness:
                feedback.title = "Stay Calm";
                feedback.message = GenerateNervousnessMessage(current);
                break;
                
            case FeedbackAction.ImproveBodyLanguage:
                feedback.title = "Enhance Your Presence";
                feedback.message = GenerateBodyLanguageMessage(current);
                break;
                
            case FeedbackAction.AddMoreDetails:
                feedback.title = "Provide Specific Examples";
                feedback.message = GenerateDetailsMessage(current);
                break;
                
            case FeedbackAction.BeMoreConcise:
                feedback.title = "Focus Your Message";
                feedback.message = GenerateConciseMessage(current);
                break;
                
            case FeedbackAction.ShowMoreEnthusiasm:
                feedback.title = "Show Your Passion";
                feedback.message = GenerateEnthusiasmMessage(current);
                break;
                
            case FeedbackAction.StructureAnswersBetter:
                feedback.title = "Organize Your Thoughts";
                feedback.message = GenerateStructureMessage(current);
                break;
                
            case FeedbackAction.HighlightAchievements:
                feedback.title = "Emphasize Your Wins";
                feedback.message = GenerateAchievementsMessage(current);
                break;
                
            case FeedbackAction.DemonstrateLeadership:
                feedback.title = "Show Leadership";
                feedback.message = GenerateLeadershipMessage(current);
                break;
                
            case FeedbackAction.ShowProblemSolving:
                feedback.title = "Display Analytical Thinking";
                feedback.message = GenerateProblemSolvingMessage(current);
                break;
                
            case FeedbackAction.ExpressCuriosity:
                feedback.title = "Ask Thoughtful Questions";
                feedback.message = GenerateCuriosityMessage(current);
                break;
                
            case FeedbackAction.BuildRapport:
                feedback.title = "Connect Personally";
                feedback.message = GenerateRapportMessage(current);
                break;
                
            case FeedbackAction.ListenMoreActively:
                feedback.title = "Engage More Fully";
                feedback.message = GenerateActiveListeningMessage(current);
                break;
                
            case FeedbackAction.MatchInterviewerEnergy:
                feedback.title = "Mirror Communication Style";
                feedback.message = GenerateEnergyMatchMessage(current);
                break;

            case FeedbackAction.MaintainCurrentApproach:
                feedback.title = "Keep It Up!";
                feedback.message = GeneratePositiveMessage(current);
                break;
                
            case FeedbackAction.ExcellentPerformance:
                feedback.title = "Outstanding Work!";
                feedback.message = GenerateExcellentMessage(current);
                break;
        }

        return feedback;
    }

    private static string GenerateConfidenceMessage(InterviewPerformance perf)
    {
        float confScore = perf.confidence;
        
        if (confScore < 0.3f)
            return $"Your confidence is at {(confScore * 100):F0}%. Remember, you have valuable skills and experience. Take a moment to center yourself, speak louder, and make eye contact. The interviewer wants you to succeed!";
        else if (confScore < 0.5f)
            return $"You're at {(confScore * 100):F0}% confidence. You're on the right track! Try to eliminate hesitation words like 'um' and 'uh'. Stand or sit up straight, and project your voice with conviction.";
        else if (confScore < 0.7f)
            return $"Good confidence level at {(confScore * 100):F0}%. To reach the next level, emphasize your achievements more boldly. Use phrases like 'I successfully...' and 'I'm proud that...' to own your accomplishments.";
        else
            return $"Strong confidence at {(confScore * 100):F0}%! Just be careful not to come across as overconfident. Balance assertiveness with humility and active listening.";
    }

    private static string GeneratePaceMessage(InterviewPerformance perf)
    {
        float pace = perf.pace;
        
        if (pace < 0.35f)
            return $"Your speaking pace is quite slow ({(pace * 100):F0}%). While thoughtfulness is good, try to pick up the tempo slightly. Practice your answers beforehand to speak more fluidly and maintain the interviewer's engagement.";
        else if (pace < 0.5f)
            return $"You're speaking a bit slowly ({(pace * 100):F0}%). This shows you're thinking carefully, but you might lose momentum. Try adding more energy to your delivery while staying clear and composed.";
        else if (pace < 0.7f)
            return $"Your pace is in a good range ({(pace * 100):F0}%). You're balancing thoughtfulness with engagement well. Just ensure you're pausing occasionally to let your points land.";
        else if (pace < 0.85f)
            return $"You're speaking quite quickly ({(pace * 100):F0}%). While enthusiasm is great, slow down a touch to ensure clarity. Take deliberate pauses between key points to let the interviewer absorb your message.";
        else
            return $"Your pace is very fast ({(pace * 100):F0}%). Take a breath! Speaking too quickly can make you seem nervous and reduces comprehension. Slow down, pause between sentences, and emphasize important words.";
    }

    private static string GenerateClarityMessage(InterviewPerformance perf)
    {
        float clarity = perf.clarity;
        
        if (clarity < 0.4f)
            return $"Your clarity needs improvement ({(clarity * 100):F0}%). Focus on enunciating each word clearly. Avoid mumbling, speak up, and structure your thoughts before answering. Use the STAR method: Situation, Task, Action, Result.";
        else if (clarity < 0.6f)
            return $"Your clarity is moderate ({(clarity * 100):F0}%). You're communicating, but some ideas could be clearer. Try organizing your thoughts with signposts like 'First...', 'Additionally...', 'In conclusion...'. Be more specific with examples.";
        else if (clarity < 0.8f)
            return $"Good clarity at {(clarity * 100):F0}%. Your ideas are coming through well! To excel further, use concrete numbers and specific outcomes when describing your experience. Avoid vague terms like 'a lot' or 'pretty good'.";
        else
            return $"Excellent clarity ({(clarity * 100):F0}%)! Your communication is crisp and well-structured. Keep using specific examples and maintaining this logical flow. This is a major strength!";
    }

    private static string GenerateToneMessage(InterviewPerformance perf)
    {
        float tone = perf.tone;
        
        if (tone < 0.4f)
            return $"Your tone could use more warmth ({(tone * 100):F0}%). Smile while you speak - it affects your voice! Show genuine enthusiasm about the role and company. Vary your pitch to emphasize key points and avoid monotone delivery.";
        else if (tone < 0.6f)
            return $"Your tone is okay ({(tone * 100):F0}%), but inject more energy and positivity. Think about what excites you about this opportunity and let that enthusiasm shine through. Match the interviewer's energy level.";
        else if (tone < 0.8f)
            return $"Nice tone at {(tone * 100):F0}%! You're conveying appropriate emotion and engagement. To perfect it, ensure your tone matches your message - serious when discussing challenges, enthusiastic when sharing successes.";
        else
            return $"Excellent tone ({(tone * 100):F0}%)! Your warmth and professionalism come through perfectly. You're creating rapport and showing genuine interest. This is making a great impression!";
    }

    private static string GenerateNervousnessMessage(InterviewPerformance perf)
    {
        string[] nervousTips = new string[]
        {
            $"Overall performance: {(perf.overall * 100):F0}%. It's okay to be nervous - it shows you care! Try the 4-7-8 breathing technique before answering: breathe in for 4 counts, hold for 7, exhale for 8. This calms your nervous system.",
            $"You're at {(perf.overall * 100):F0}% performance. Remember, the interviewer is human too! They want you to succeed. Take a sip of water if you need a moment to collect your thoughts. Pausing is better than rambling.",
            $"Current level: {(perf.overall * 100):F0}%. Channel that nervous energy into enthusiasm! Sit with both feet on the floor, keep your hands visible and still. Power poses before the interview can boost confidence.",
            $"You're doing {(perf.overall * 100):F0}% overall. Everyone gets nervous in interviews. Use positive self-talk: 'I am prepared. I am qualified. I belong here.' Focus on the conversation, not your anxiety.",
            $"At {(perf.overall * 100):F0}% performance. Ground yourself! Notice 5 things you can see, 4 you can touch, 3 you can hear. This mindfulness technique pulls you out of anxiety and into the present moment."
        };
        
        int index = UnityEngine.Random.Range(0, nervousTips.Length);
        return nervousTips[index];
    }

    private static string GeneratePositiveMessage(InterviewPerformance perf)
    {
        if (perf.overall >= 0.9f)
            return $"Outstanding performance ({(perf.overall * 100):F0}%)! You're hitting all the marks - confident, clear, well-paced, and engaging. This is exactly the level of communication that impresses interviewers. Keep this energy!";
        else if (perf.overall >= 0.8f)
            return $"Strong performance at {(perf.overall * 100):F0}%! You're demonstrating excellent communication skills. Your answers are well-structured and delivered with confidence. You're making a positive impression!";
        else if (perf.overall >= 0.7f)
            return $"Good work ({(perf.overall * 100):F0}%)! You're doing well across multiple dimensions. Your approach is solid - just maintain this consistency and you'll continue to excel. Trust your preparation!";
        else
            return $"You're at {(perf.overall * 100):F0}% and performing solidly. Keep up this balanced approach. Every interview is practice for the next one. Stay focused and keep refining your delivery!";
    }

    private static string GenerateExcellentMessage(InterviewPerformance perf)
    {
        return $"Exceptional work! ({(perf.overall * 100):F0}%) You're demonstrating mastery across all dimensions. Your confidence is strong, communication is crystal clear, pacing is perfect, and your tone shows genuine enthusiasm. This is the gold standard of interview performance. Interviewers remember candidates like you!";
    }

    private static string GenerateBodyLanguageMessage(InterviewPerformance perf)
    {
        string[] tips = new string[]
        {
            "Body language speaks volumes! Sit up straight with shoulders back. Keep your hands visible and use natural gestures to emphasize points. Maintain good eye contact - look at the camera, not the screen. Smile genuinely to convey warmth and confidence.",
            "Your non-verbal communication matters just as much as your words. Avoid crossing your arms (looks defensive) or fidgeting. Lean in slightly when listening to show engagement. Nod occasionally to show you're following along. Power poses before the interview boost confidence!",
            "Project presence through posture! Plant both feet on the ground. Keep your chin parallel to the floor. Use open hand gestures instead of pointing. Mirror the interviewer's body language subtly to build rapport. Your physical confidence will translate to vocal confidence."
        };
        return tips[UnityEngine.Random.Range(0, tips.Length)];
    }

    private static string GenerateDetailsMessage(InterviewPerformance perf)
    {
        return $"Clarity: {(perf.clarity * 100):F0}%. Your answers need more concrete examples. Instead of saying 'I improved the system,' say 'I reduced load times by 40% by implementing Redis caching, which saved the company 20 hours per week.' Use numbers, timeframes, and measurable outcomes. The STAR method helps: Situation, Task, Action, Result. Make it vivid and specific!";
    }

    private static string GenerateConciseMessage(InterviewPerformance perf)
    {
        return $"Pace: {(perf.pace * 100):F0}%. You might be over-explaining. Aim for the 'headline first' approach - state your main point in one sentence, then provide 2-3 supporting details. Practice the 2-minute rule: can you summarize any answer in under 2 minutes? Cut filler words and tangents. Quality over quantity!";
    }

    private static string GenerateEnthusiasmMessage(InterviewPerformance perf)
    {
        return $"Tone: {(perf.tone * 100):F0}%. Show more passion! Your technical skills are evident, but interviewers also want to see excitement. Why does this role excite you? What genuinely interests you about this company? Let that enthusiasm come through in your voice. Smile while you talk - it changes your vocal tone and energy level. Passion is contagious!";
    }

    private static string GenerateStructureMessage(InterviewPerformance perf)
    {
        return $"Clarity: {(perf.clarity * 100):F0}%. Organize your thoughts using frameworks. For behavioral questions, use STAR (Situation, Task, Action, Result). For technical problems, use Problem-Solution-Impact. Start strong: 'The short answer is...' then elaborate. Use transitions: 'First...', 'Additionally...', 'In conclusion...'. Signpost your thinking!";
    }

    private static string GenerateAchievementsMessage(InterviewPerformance perf)
    {
        return $"Confidence: {(perf.confidence * 100):F0}%. Don't be humble - this is your time to shine! Quantify your wins: 'I increased sales by 30%', 'I led a team of 8', 'I reduced costs by $50K'. Use strong action verbs: achieved, spearheaded, optimized, transformed. Own your accomplishments without apologizing. Replace 'I just...' with 'I successfully...'";
    }

    private static string GenerateLeadershipMessage(InterviewPerformance perf)
    {
        return "Demonstrate leadership qualities even if you haven't had a formal leadership role! Talk about times you took initiative, mentored others, drove consensus, or influenced decisions. Leadership shows through phrases like 'I proposed...', 'I coordinated...', 'I guided the team...'. Show you can inspire and influence, not just execute.";
    }

    private static string GenerateProblemSolvingMessage(InterviewPerformance perf)
    {
        return "Showcase your analytical thinking! Walk through your thought process: 'I identified the root cause by...', 'I considered three alternatives...', 'I chose this approach because...'. Show trade-off analysis. Mention data you gathered, stakeholders you consulted, risks you evaluated. Interviewers want to see HOW you think, not just WHAT you decided.";
    }

    private static string GenerateCuriosityMessage(InterviewPerformance perf)
    {
        return "Interviews are two-way conversations! Prepare 3-5 thoughtful questions that show you've researched the company: 'I noticed you recently launched X, how is that affecting the team's priorities?' Ask about challenges, team dynamics, success metrics, growth opportunities. Avoid questions easily answered by Google. Curiosity shows genuine interest and initiative!";
    }

    private static string GenerateRapportMessage(InterviewPerformance perf)
    {
        return $"Tone: {(perf.tone * 100):F0}%. Build personal connection! Find common ground early - reference something from their LinkedIn or the company's recent news. Use the interviewer's name occasionally. Share brief personal anecdotes that humanize you. Be authentic, not robotic. People hire people they like and can imagine working with. Show personality!";
    }

    private static string GenerateActiveListeningMessage(InterviewPerformance perf)
    {
        return "Show you're truly engaged! Don't just wait for your turn to talk. Take brief notes, nod, and use verbal acknowledgments ('That's a great question', 'I appreciate you asking that'). Paraphrase their question to confirm understanding: 'If I'm hearing you correctly, you're asking about...' Ask clarifying questions. This shows respect and thoughtfulness.";
    }

    private static string GenerateEnergyMatchMessage(InterviewPerformance perf)
    {
        return $"Overall: {(perf.overall * 100):F0}%. Match the interviewer's communication style! If they're formal and structured, be organized and professional. If they're casual and conversational, relax a bit. Mirror their pace - if they speak quickly, pick up your tempo. If they're analytical, provide data. If they're people-focused, share stories. Adaptation builds rapport!";
    }
}

