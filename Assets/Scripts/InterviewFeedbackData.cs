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
/// Feedback action types - focused on speech-measurable metrics only
/// </summary>
public enum FeedbackAction
{
    // Speech Rate & Pace
    ImproveSpeechPace = 0,        // Adjust speaking speed
    SlowDownPacing = 1,           // Speaking too fast
    SpeedUpPacing = 2,            // Speaking too slow
    
    // Confidence
    EncourageConfidence = 3,      // Boost vocal confidence
    ReduceNervousness = 4,        // Calm anxiety (affects pace + tone)
    
    // Tone Patterns
    ImproveVocalVariety = 5,      // More dynamic tone patterns
    OptimizeTone = 6,             // Better emotional tone
    AddEnthusiasm = 7,            // More energy in voice
    
    // Overall
    MaintainCurrentApproach = 8,  // Keep doing what you're doing
    ExcellentPerformance = 9      // Outstanding work
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
                feedback.title = "Build Your Confidence";
                feedback.message = GenerateConfidenceMessage(current);
                break;

            case FeedbackAction.ImproveSpeechPace:
                feedback.title = "Adjust Your Pace";
                feedback.message = GeneratePaceMessage(current);
                break;
                
            case FeedbackAction.SlowDownPacing:
                feedback.title = "Slow Down";
                feedback.message = GenerateSlowDownMessage(current);
                break;
                
            case FeedbackAction.SpeedUpPacing:
                feedback.title = "Pick Up the Pace";
                feedback.message = GenerateSpeedUpMessage(current);
                break;

            case FeedbackAction.OptimizeTone:
                feedback.title = "Optimize Your Tone";
                feedback.message = GenerateToneMessage(current);
                break;

            case FeedbackAction.ReduceNervousness:
                feedback.title = "Stay Calm";
                feedback.message = GenerateNervousnessMessage(current);
                break;
                
            case FeedbackAction.ImproveVocalVariety:
                feedback.title = "Vary Your Voice";
                feedback.message = GenerateVocalVarietyMessage(current);
                break;
                
            case FeedbackAction.AddEnthusiasm:
                feedback.title = "Show More Energy";
                feedback.message = GenerateEnthusiasmMessage(current);
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
    
    private static string GenerateSlowDownMessage(InterviewPerformance perf)
    {
        int pacePercent = (int)(perf.pace * 100);
        return $"Pace: {pacePercent}%. You're rushing! Slow down to improve clarity and show confidence. Take deliberate pauses between key points - this gives the interviewer time to absorb your message and shows you're thoughtful. Practice the 'breath pause' technique: breathe before answering, pause between sentences. Speaking too quickly can signal nervousness. Confident speakers control their pace.";
    }
    
    private static string GenerateSpeedUpMessage(InterviewPerformance perf)
    {
        int pacePercent = (int)(perf.pace * 100);
        return $"Pace: {pacePercent}%. Pick up the tempo! You're speaking too slowly, which can make you seem uncertain or cause the interviewer to lose engagement. Add more energy to your delivery. Practice your answers beforehand so they flow naturally. Aim for 2-3 words per second. A moderate pace shows enthusiasm and keeps attention focused on your message.";
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
        // Renamed to VocalVariety - no camera available
        return GenerateVocalVarietyMessage(perf);
    }

    private static string GenerateVocalVarietyMessage(InterviewPerformance perf)
    {
        // Focus on vocal dynamics since there's no camera
        if (perf.tone < 0.5f && perf.pace < 0.5f)
        {
            return $"Tone: {(perf.tone * 100):F0}%, Pace: {(perf.pace * 100):F0}%. Your voice needs more variation! Avoid monotone delivery - vary your pitch to emphasize key points. Slow down for important ideas, speed up slightly for background. Change your volume: louder for confident statements, softer for thoughtful moments. Think of it like telling a story, not reading a script.";
        }
        else if (perf.tone < 0.5f)
        {
            return $"Tone: {(perf.tone * 100):F0}%. Add vocal dynamics! Use pitch variation - go higher when excited or asking questions, lower for serious points. Emphasize power words: 'I ACHIEVED a 40% improvement.' Pause strategically before key points to build anticipation. Your voice is an instrument - play it expressively!";
        }
        else if (perf.pace < 0.5f)
        {
            return $"Pace: {(perf.pace * 100):F0}%. Use strategic pacing! Speed up when listing background details, slow down for main points you want them to remember. Pause after important statements - let them sink in. Vary your rhythm to keep attention. Monotonous pacing puts people to sleep; dynamic pacing keeps them engaged.";
        }
        else
        {
            return $"Overall: {(perf.overall * 100):F0}%. Enhance vocal engagement! Practice vocal emphasis: stress important words in each sentence. Use 'upspeak' (rising tone) for questions or possibilities, 'downspeak' (falling tone) for facts and conclusions. Record yourself and listen - are you interesting to hear? Would YOU stay engaged listening to this? Polish your vocal delivery like a podcast host!";
        }
    }

    private static string GenerateDetailsMessage(InterviewPerformance perf)
    {
        int clarityPercent = (int)(perf.clarity * 100);
        
        if (clarityPercent < 40)
        {
            return $"Clarity: {clarityPercent}%. Your answers are too vague. Replace generic statements with specific examples. Instead of 'I worked on a project,' say 'I led a 6-month migration project that improved performance by 40% and reduced costs by $50K annually.' Use the STAR method: Situation, Task, Action, Result.";
        }
        else if (clarityPercent < 60)
        {
            return $"Clarity: {clarityPercent}%. Add concrete numbers and outcomes! Instead of 'I improved the system,' say 'I reduced API response time from 800ms to 120ms, handling 3x more traffic.' Quantify everything: team size, timelines, percentages, dollar amounts. Make it vivid and measurable!";
        }
        else
        {
            return $"Clarity: {clarityPercent}%. You're communicating well, but go deeper! Add context about WHY your solution mattered. Who benefited? What was the business impact? What would have happened if you hadn't acted? Connect your technical work to business outcomes.";
        }
    }

    private static string GenerateConciseMessage(InterviewPerformance perf)
    {
        int pacePercent = (int)(perf.pace * 100);
        
        if (pacePercent > 80)
        {
            return $"Pace: {pacePercent}%. You're rushing and over-explaining! Slow down. Practice the 'headline first' approach: state your main point in one sentence, THEN provide 2-3 supporting details. Example: 'I increased team efficiency by 30% through automated testing.' Then elaborate. Cut filler words like 'basically,' 'actually,' 'kind of.'";
        }
        else if (pacePercent > 70)
        {
            return $"Pace: {pacePercent}%. You might be including too much detail. Apply the 2-minute rule: can you summarize any answer in under 2 minutes? If not, cut tangents. Focus on the most impressive and relevant points. Quality beats quantity. The interviewer can always ask for more details.";
        }
        else
        {
            return $"Pace: {pacePercent}%. Balance depth with brevity. After answering, pause and check the interviewer's reaction. Are they nodding (good, continue)? Looking confused (clarify)? Glancing at notes (you might be rambling)? Read the room and adjust accordingly.";
        }
    }

    private static string GenerateEnthusiasmMessage(InterviewPerformance perf)
    {
        int tonePercent = (int)(perf.tone * 100);
        
        if (tonePercent < 40)
        {
            return $"Tone: {tonePercent}%. Your delivery feels flat! Inject energy into your voice. What genuinely excites you about this role? Why do you care about this company's mission? Let that authentic enthusiasm shine through. Smile while you talk - it literally changes your vocal tone and makes you sound more engaged.";
        }
        else if (tonePercent < 60)
        {
            return $"Tone: {tonePercent}%. Show more passion for your work! When describing projects you loved, your voice should light up. Use phrases like 'What I found fascinating was...' or 'The exciting challenge here was...' Don't just list facts - share what motivated you. Passion is contagious and memorable!";
        }
        else
        {
            return $"Tone: {tonePercent}%. You're doing well, but vary your vocal energy! Be serious when discussing challenges, enthusiastic when sharing successes, thoughtful when explaining decisions. This emotional range makes you more engaging and shows you understand the weight of different situations.";
        }
    }

    private static string GenerateStructureMessage(InterviewPerformance perf)
    {
        int clarityPercent = (int)(perf.clarity * 100);
        
        if (clarityPercent < 45)
        {
            return $"Clarity: {clarityPercent}%. Your answers lack structure! Use the STAR framework for behavioral questions: Situation (context), Task (challenge), Action (what YOU did), Result (outcome with numbers). Start with: 'Let me walk you through a specific example...' This keeps you organized and focused.";
        }
        else if (clarityPercent < 60)
        {
            return $"Clarity: {clarityPercent}%. Improve organization with signposting! Use verbal markers: 'First, let me give you context...', 'The main challenge was...', 'I took three key actions...', 'The result was...'. This roadmap helps interviewers follow your thinking and shows logical thought process.";
        }
        else
        {
            return $"Clarity: {clarityPercent}%. Good structure, but make your opening stronger! Lead with the most impressive part: 'I saved the company $200K by redesigning the deployment pipeline.' THEN explain how. Hook them first, then deliver the details. 'Headline first' makes you memorable.";
        }
    }

    private static string GenerateAchievementsMessage(InterviewPerformance perf)
    {
        int confPercent = (int)(perf.confidence * 100);
        
        if (confPercent < 40)
        {
            return $"Confidence: {confPercent}%. Stop downplaying your accomplishments! Remove 'just,' 'only,' and 'simply' from your vocabulary. Replace 'I helped with...' with 'I contributed to...' or 'I drove...'. Replace 'We did...' with 'I led the team to...'. Take credit for YOUR specific impact. You earned it!";
        }
        else if (confPercent < 55)
        {
            return $"Confidence: {confPercent}%. Quantify every win with hard numbers! 'Improved performance' becomes 'Reduced load time by 60%, handling 10K more users daily'. 'Led a team' becomes 'Managed 8 engineers across 3 time zones'. 'Successful project' becomes '30% under budget, delivered 2 weeks early'. Numbers are proof!";
        }
        else
        {
            return $"Confidence: {confPercent}%. Solid presentation, but emphasize your unique value! What did YOU specifically contribute that others couldn't? Use power verbs: architected, spearheaded, pioneered, transformed, optimized. Connect your actions directly to business outcomes: revenue up, costs down, efficiency improved, customers satisfied.";
        }
    }

    private static string GenerateLeadershipMessage(InterviewPerformance perf)
    {
        int confPercent = (int)(perf.confidence * 100);
        
        if (confPercent < 50)
        {
            return $"Confidence: {confPercent}%. Highlight leadership even without a title! Talk about times you took initiative: 'I noticed the deployment process was broken, so I proposed and implemented automated testing.' Show you drive change, don't wait for permission. Initiative IS leadership.";
        }
        else
        {
            return $"Overall: {(int)(perf.overall * 100)}%. Demonstrate leadership impact! Share examples where you: influenced decisions ('I convinced the team to adopt microservices'), mentored others ('I onboarded 3 junior devs'), or resolved conflict ('I mediated between design and engineering'). Leadership is about influence, not authority.";
        }
    }

    private static string GenerateProblemSolvingMessage(InterviewPerformance perf)
    {
        int clarityPercent = (int)(perf.clarity * 100);
        
        if (clarityPercent > 70 && perf.confidence > 0.65)
        {
            return $"Clarity: {clarityPercent}%, Confidence: {(int)(perf.confidence * 100)}%. You're strong here, but go deeper into your process! Show your analytical thinking: 'I gathered metrics showing 40% of errors came from one endpoint. I considered three solutions: caching, rate-limiting, or refactoring. I chose refactoring because...' Walk through your decision-making, not just the decision.";
        }
        else
        {
            return "Showcase analytical thinking! Structure problem-solving stories: 1) Symptoms you noticed, 2) Data you gathered, 3) Root cause analysis, 4) Alternatives considered, 5) Trade-offs evaluated, 6) Solution chosen and WHY, 7) Results achieved. Show you're systematic and data-driven, not just lucky or reactive.";
        }
    }

    private static string GenerateCuriosityMessage(InterviewPerformance perf)
    {
        int overallPercent = (int)(perf.overall * 100);
        
        if (overallPercent < 70 && perf.confidence > 0.6)
        {
            return $"Overall: {overallPercent}%. Ask smarter questions! Show you've done research: 'I read about your new AI initiative - how does that affect your infrastructure roadmap?' Ask about challenges: 'What's the biggest technical debt the team is tackling?' Ask about culture: 'How does the team balance innovation vs. stability?' Thoughtful questions prove genuine interest.";
        }
        else
        {
            return "Prepare 5-7 questions categorized: technical challenges, team dynamics, success metrics, growth opportunities, company direction. Ask about the interviewer's experience: 'What's your favorite part of working here?' Avoid easily Googled questions. Interviews are two-way - show you're evaluating them too!";
        }
    }

    private static string GenerateRapportMessage(InterviewPerformance perf)
    {
        int tonePercent = (int)(perf.tone * 100);
        
        if (tonePercent < 50)
        {
            return $"Tone: {tonePercent}%. Build connection beyond technical talk! Find common ground early - reference the interviewer's background: 'I saw you worked at X, I'm curious about...' Share brief personal anecdotes that show who you are: hobbies, values, what drives you. People hire people they like working with!";
        }
        else if (tonePercent < 65)
        {
            return $"Tone: {tonePercent}%. Humanize the conversation! Use the interviewer's name occasionally. Show vulnerability: 'That's a great question, let me think for a moment' is better than fumbling. Acknowledge their insights: 'That's an interesting perspective!' Be warm, not robotic. Authenticity builds trust.";
        }
        else
        {
            return $"Tone: {tonePercent}%. You're building rapport well! Now deepen it by showing genuine curiosity about their experience. Ask follow-up questions: 'You mentioned challenge X, how did the team overcome that?' This shows you're engaged and see them as collaborators, not just gatekeepers.";
        }
    }

    private static string GenerateActiveListeningMessage(InterviewPerformance perf)
    {
        int overallPercent = (int)(perf.overall * 100);
        
        if (overallPercent < 75)
        {
            return $"Overall: {overallPercent}%. Show you're truly listening! Take brief notes (shows they said something worth writing down). Use verbal acknowledgments: 'That's a great question,' 'I appreciate you asking that.' Paraphrase to confirm: 'So you're asking about my experience with distributed systems?' This builds respect and prevents misunderstandings.";
        }
        else
        {
            return "Master active listening! Pause before answering (shows you're thinking, not just reciting). Reference earlier parts of the conversation: 'Building on what you said about scalability challenges...' Ask clarifying questions: 'To make sure I answer fully, are you asking about horizontal or vertical scaling?' This shows depth of engagement.";
        }
    }

    private static string GenerateEnergyMatchMessage(InterviewPerformance perf)
    {
        int overallPercent = (int)(perf.overall * 100);
        
        if (overallPercent < 60)
        {
            return $"Overall: {overallPercent}%. Adapt your style to match theirs! If the interviewer is formal and structured, be organized and professional. If they're casual and conversational, relax a bit but stay professional. If they speak quickly, pick up your pace slightly. If they're analytical, provide data. If they're story-focused, share anecdotes. Mirroring builds rapport!";
        }
        else
        {
            return $"Overall: {overallPercent}%. You're adapting well! Fine-tune your mirroring: match their vocabulary (do they say 'customers' or 'users'? 'team' or 'squad'?). Reflect their priorities - if they emphasize speed, highlight your efficiency. If they focus on quality, showcase your attention to detail. Subtle alignment shows cultural fit!";
        }
    }
}

