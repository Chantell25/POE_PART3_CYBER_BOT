using System.Collections.Generic;
using System.Linq;

namespace CybersecurityBotGUI.Services
{
    public enum Sentiment { Neutral, Worried, Curious, Frustrated, Positive }

    /// <summary>Detects the user's emotional tone from their message (Part 2 requirement).</summary>
    public class SentimentDetector
    {
        private static readonly List<string> Worried    = new() { "worried", "scared", "afraid", "nervous", "anxious", "fear", "unsafe", "exposed", "concerned", "worrying", "scary", "terrifying" };
        private static readonly List<string> Frustrated = new() { "frustrated", "frustrating", "annoyed", "annoying", "confused", "confusing", "lost", "don't understand", "complicated", "difficult", "hard", "angry", "upset" };
        private static readonly List<string> Curious    = new() { "curious", "wondering", "interested", "want to know", "how does", "why does", "what is", "explain", "learn", "understand", "tell me" };
        private static readonly List<string> Positive   = new() { "great", "awesome", "love", "happy", "excited", "good", "perfect", "excellent", "thanks", "thank you", "helpful", "nice" };

        public Sentiment Detect(string input)
        {
            string lower = input.ToLower();
            if (Worried.Any(k    => lower.Contains(k))) return Sentiment.Worried;
            if (Frustrated.Any(k => lower.Contains(k))) return Sentiment.Frustrated;
            if (Curious.Any(k    => lower.Contains(k))) return Sentiment.Curious;
            if (Positive.Any(k   => lower.Contains(k))) return Sentiment.Positive;
            return Sentiment.Neutral;
        }

        public string GetEmpathyPrefix(Sentiment s) => s switch
        {
            Sentiment.Worried    => "It's completely understandable to feel that way. Let me help reassure you. ",
            Sentiment.Frustrated => "I understand this can feel confusing. Let me explain more clearly. ",
            Sentiment.Curious    => "Great question! I love helping people learn about cybersecurity. ",
            Sentiment.Positive   => "Glad to hear that! Keep that enthusiasm for staying safe online. ",
            _                   => string.Empty
        };
    }
}
