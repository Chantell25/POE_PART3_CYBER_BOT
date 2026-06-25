using CybersecurityBotGUI.Models;

namespace CybersecurityBotGUI.Services
{
    /// <summary>
    /// Main orchestrator. Every user message flows through here:
    /// 1. Check for exit
    /// 2. Check for memory commands ("show memory")
    /// 3. Try the NLP/Task/Log/Quiz service first (Part 3 features)
    /// 4. Fall back to the general cybersecurity response system (Part 2 features)
    /// All with sentiment-aware empathy prefixes layered on top.
    /// </summary>
    public class ChatbotService
    {
        private readonly ResponseSystem    _responseSystem;
        private readonly SentimentDetector _sentimentDetector;
        private readonly NlpService        _nlpService;
        private readonly ActivityLogService _logService;
        private readonly UserMemory        _memory;

        private string? _lastTopic;

        public ChatbotService(string userName, NlpService nlpService, ActivityLogService logService)
        {
            _responseSystem    = new ResponseSystem();
            _sentimentDetector = new SentimentDetector();
            _nlpService        = nlpService;
            _logService        = logService;
            _memory            = new UserMemory { Name = userName };
        }

        public UserMemory Memory => _memory;

        /// <summary>
        /// Processes one user message. Returns:
        ///   - the bot's reply text, or
        ///   - null if the user wants to exit, or
        ///   - "__QUIZ__" if the GUI should start the quiz.
        /// </summary>
        public string? ProcessMessage(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return "I did not catch that. Could you please rephrase?";

            string lower = input.Trim().ToLower();

            // Exit
            if (_responseSystem.IsExit(lower))
                return null;

            // Memory commands
            if (lower == "show memory" || lower == "what do you remember")
                return BuildMemoryRecap();

            if (lower == "clear memory")
            {
                _memory.FavouriteTopic = null;
                _memory.Facts.Clear();
                return "I have cleared everything I remembered. Fresh start!";
            }

            // Sentiment (used for empathy prefix on general responses only,
            // NLP/task/log responses stay factual and direct)
            Sentiment sentiment = _sentimentDetector.Detect(input);

            // ── Part 3: Try NLP / Task / Log / Quiz first ───────────────────────
            if (_nlpService.TryHandle(input, out string nlpResponse))
            {
                if (nlpResponse == "__QUIZ__")
                {
                    _logService.Log("Quiz", "Quiz started");
                    return "__QUIZ__";
                }
                // Note: TaskService/NlpService already log specific actions
                // (task added, reminder set, etc.) - no need to double-log here.
                return nlpResponse;
            }

            // ── Part 2: General cybersecurity Q&A fallback ──────────────────────
            string empathy   = _sentimentDetector.GetEmpathyPrefix(sentiment);
            string rawReply  = _responseSystem.GetResponse(input, _lastTopic, out string? newTopic);

            if (rawReply == "__NOMATCH__")
            {
                return "I am not sure I understand. Could you try rephrasing? " +
                       "You can ask about 'phishing', 'password', 'scam', 'malware', 'privacy', " +
                       "add a task, start the quiz, or type 'help'.";
            }

            _lastTopic = newTopic;
            UpdateMemory(lower, newTopic);

            string finalReply = string.IsNullOrEmpty(empathy) ? rawReply : empathy + rawReply;

            string note = BuildMemoryNote(newTopic);
            if (!string.IsNullOrEmpty(note))
                finalReply += "\n\n" + note;

            return finalReply;
        }

        /// <summary>Returns the full help text listing every supported command.</summary>
        public string GetHelpText()
        {
            return "Available commands:\n" +
                   "  password / phishing / scam / malware / privacy / 2fa / vpn / browsing\n" +
                   "  Add a task to ... (e.g. 'Add a task to enable 2FA')\n" +
                   "  Remind me to ... (e.g. 'Remind me to update my password tomorrow')\n" +
                   "  show tasks        - View your cybersecurity task list\n" +
                   "  show activity log - View recent bot actions\n" +
                   "  quiz              - Start the cybersecurity quiz\n" +
                   "  show memory       - See what I remember about you\n" +
                   "  clear memory      - Forget what I have learned about you\n" +
                   "  goodbye           - End the session";
        }

        private void UpdateMemory(string lowerInput, string? topic)
        {
            if (topic != null && _memory.FavouriteTopic == null)
            {
                if (lowerInput.Contains("interested in") || lowerInput.Contains("care about") || lowerInput.Contains("learn about"))
                {
                    _memory.FavouriteTopic   = topic;
                    _memory.Facts["interest"] = topic;
                }
            }
            _memory.LastTopic = topic;
        }

        private string BuildMemoryNote(string? currentTopic)
        {
            if (!string.IsNullOrEmpty(_memory.FavouriteTopic) && _memory.FavouriteTopic == currentTopic)
                return "As someone interested in " + _memory.FavouriteTopic + ", you might also want to review your account security settings.";
            return string.Empty;
        }

        private string BuildMemoryRecap()
        {
            string recap = "Here's what I remember about you, " + _memory.Name + ":\n";
            recap += "  Name: " + _memory.Name + "\n";
            if (!string.IsNullOrEmpty(_memory.FavouriteTopic))
                recap += "  Favourite topic: " + _memory.FavouriteTopic + "\n";
            if (!string.IsNullOrEmpty(_memory.LastTopic))
                recap += "  Last discussed topic: " + _memory.LastTopic + "\n";
            return recap.TrimEnd();
        }

        private static string Truncate(string s, int max) =>
            s.Length <= max ? s : s.Substring(0, max) + "...";
    }
}
