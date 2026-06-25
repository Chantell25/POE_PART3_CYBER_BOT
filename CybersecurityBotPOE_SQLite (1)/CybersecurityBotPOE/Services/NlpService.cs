using System;
using System.Linq;
using System.Text.RegularExpressions;
using CybersecurityBotGUI.Models;

namespace CybersecurityBotGUI.Services
{
    /// <summary>
    /// Simulates basic Natural Language Processing using keyword detection and
    /// regular expressions (Part 3 requirement). Recognises varied phrasing for
    /// adding tasks, setting reminders, viewing tasks, viewing the activity log,
    /// and starting the quiz - without requiring exact command syntax.
    /// </summary>
    public class NlpService
    {
        private readonly TaskService        _taskService;
        private readonly ActivityLogService _logService;

        // Tracks a task that was just added without a reminder, in case the
        // user's NEXT message answers "yes, remind me in X days" (per the brief's example flow).
        private TaskItem? _pendingReminderTask = null;

        public NlpService(TaskService taskService, ActivityLogService logService)
        {
            _taskService = taskService;
            _logService  = logService;
        }

        // ── Trigger phrase libraries ─────────────────────────────────────────────

        private static readonly string[] AddTaskTriggers =
        {
            "add task", "add a task", "create task", "create a task", "new task"
        };

        private static readonly string[] ReminderTriggers =
        {
            "remind me to", "remind me", "set a reminder to", "set a reminder for",
            "set reminder", "add a reminder to", "add reminder"
        };

        private static readonly string[] ShowTasksTriggers =
        {
            "show tasks", "show my tasks", "view tasks", "my tasks", "task list", "list tasks", "what are my tasks"
        };

        private static readonly string[] ShowLogTriggers =
        {
            "show activity log", "show log", "activity log", "what have you done for me",
            "what have you done", "show history", "recent actions"
        };

        private static readonly string[] QuizTriggers =
        {
            "quiz", "test me", "test my knowledge", "start the quiz", "play a game"
        };

        private static readonly string[] AffirmativeWords = { "yes", "yeah", "yep", "sure", "ok", "okay" };

        // ── Public API ───────────────────────────────────────────────────────────

        /// <summary>
        /// Attempts to interpret the input as a task/reminder/log/quiz command.
        /// Returns true and sets `response` if it recognised and handled the intent.
        /// Returns false if the input should fall through to the general chatbot response system.
        /// </summary>
        public bool TryHandle(string input, out string response)
        {
            string lower = input.Trim().ToLower();
            response = string.Empty;

            // 1. Pending reminder follow-up ("Yes, remind me in 3 days")
            if (_pendingReminderTask != null)
            {
                DateTime? date = ParseDate(lower);
                if (date.HasValue)
                {
                    var task = _pendingReminderTask;
                    _taskService.SetReminder(task.Id, task.Title, date.Value, out string err);
                    _pendingReminderTask = null;
                    response = string.IsNullOrEmpty(err)
                        ? "Got it! I will remind you about '" + task.Title + "' on " + date.Value.ToString("dd MMM yyyy") + "."
                        : "I could not save that reminder. Database error: " + err;
                    return true;
                }
                if (AffirmativeWords.Any(w => lower == w || lower.StartsWith(w + " ")) && !lower.Contains("day") && !lower.Contains("week"))
                {
                    response = "Sure! In how many days would you like the reminder? (e.g. 'in 3 days')";
                    return true;
                }
                // Any other input clears the pending state and falls through normally
                _pendingReminderTask = null;
            }

            // 2. Show activity log
            if (ShowLogTriggers.Any(t => lower.Contains(t)))
            {
                response = BuildLogSummary();
                return true;
            }

            // 3. Show tasks
            if (ShowTasksTriggers.Any(t => lower.Contains(t)))
            {
                response = BuildTaskSummary();
                return true;
            }

            // 4. Start quiz (handled by the GUI, signal with a sentinel)
            if (QuizTriggers.Any(t => lower.Contains(t)))
            {
                response = "__QUIZ__";
                return true;
            }

            // 5. Add task (explicit "add task" phrasing)
            string? triggerUsed = AddTaskTriggers.FirstOrDefault(t => lower.Contains(t));
            if (triggerUsed != null)
            {
                HandleAddTask(input, lower, triggerUsed);
                response = BuildAddTaskResponse();
                return true;
            }

            // 6. Reminder phrasing ("remind me to update my password tomorrow")
            triggerUsed = ReminderTriggers.FirstOrDefault(t => lower.Contains(t));
            if (triggerUsed != null)
            {
                HandleAddTask(input, lower, triggerUsed, isReminderPhrasing: true);
                response = BuildAddTaskResponse();
                return true;
            }

            // No NLP intent matched - let the general response system handle it
            return false;
        }

        // ── Private helpers ──────────────────────────────────────────────────────

        private TaskItem? _lastAddedTask;

        private void HandleAddTask(string originalInput, string lower, string trigger, bool isReminderPhrasing = false)
        {
            // Take everything after the trigger phrase as the raw task description
            int idx = lower.IndexOf(trigger);
            string remainder = originalInput.Substring(idx + trigger.Length).Trim();

            // Strip common leading connector words/punctuation ("-", ":", "to ")
            remainder = Regex.Replace(remainder, @"^[\s\-:]+", "");
            remainder = Regex.Replace(remainder, @"^to\s+", "", RegexOptions.IgnoreCase);

            // Extract a date phrase if present, and remove it from the title text
            DateTime? date = ParseDate(remainder.ToLower());
            string title = StripDatePhrases(remainder).TrimEnd('.', '!', '?').Trim();

            if (string.IsNullOrWhiteSpace(title))
                title = "Untitled cybersecurity task";

            // Capitalise first letter for a clean display
            if (title.Length > 0)
                title = char.ToUpper(title[0]) + title.Substring(1);

            var task = _taskService.AddTask(title, title, date, out bool success, out string err);
            _lastAddedTask = success ? task : null;

            // If no date was given, remember this task in case the next message sets the reminder
            _pendingReminderTask = (success && !date.HasValue) ? task : null;

            if (!success)
                _logService.Log("Task", "NLP failed to add task '" + title + "': " + err);
        }

        private string BuildAddTaskResponse()
        {
            if (_lastAddedTask == null)
                return "I tried to add that task but ran into a database problem. Please check your MySQL connection (see DbConfig.cs) and try again.";

            var task = _lastAddedTask;
            if (task.ReminderDate.HasValue)
                return "Reminder set for '" + task.Title + "' on " + task.ReminderDate.Value.ToString("dd MMM yyyy") + ".";

            return "Task added: '" + task.Title + "'. Would you like a reminder? (e.g. 'yes, remind me in 3 days')";
        }

        private string BuildTaskSummary()
        {
            var tasks = _taskService.GetAllTasks(out bool success, out string err);
            if (!success)
                return "I could not load your tasks. Database error: " + err;

            if (tasks.Count == 0)
                return "You do not have any cybersecurity tasks yet. Try saying 'Add a task to enable 2FA'.";

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Here are your tasks:");
            int shown = 0;
            foreach (var t in tasks)
            {
                if (shown >= 8) break;
                sb.AppendLine("  " + (t.IsCompleted ? "[x] " : "[ ] ") + t.Title + " - " + t.ReminderDisplay);
                shown++;
            }
            if (tasks.Count > 8)
                sb.AppendLine("  ...and " + (tasks.Count - 8) + " more. Check the Tasks tab to see everything.");

            return sb.ToString().TrimEnd();
        }

        private string BuildLogSummary()
        {
            var recent = _logService.GetRecent(8);
            if (recent.Count == 0)
                return "Nothing has happened yet this session. Try adding a task or starting the quiz!";

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Here's a summary of recent actions:");
            int i = 1;
            foreach (var entry in recent)
            {
                sb.AppendLine("  " + i + ". " + entry.Display);
                i++;
            }
            return sb.ToString().TrimEnd();
        }

        /// <summary>Extracts a date from natural phrasing like "tomorrow" or "in 3 days".</summary>
        public static DateTime? ParseDate(string lower)
        {
            if (lower.Contains("tomorrow"))
                return DateTime.Today.AddDays(1);

            if (lower.Contains("today"))
                return DateTime.Today;

            if (lower.Contains("next week"))
                return DateTime.Today.AddDays(7);

            Match dayMatch = Regex.Match(lower, @"in\s+(\d+)\s*day");
            if (dayMatch.Success && int.TryParse(dayMatch.Groups[1].Value, out int days))
                return DateTime.Today.AddDays(days);

            Match weekMatch = Regex.Match(lower, @"in\s+(\d+)\s*week");
            if (weekMatch.Success && int.TryParse(weekMatch.Groups[1].Value, out int weeks))
                return DateTime.Today.AddDays(weeks * 7);

            return null;
        }

        /// <summary>Removes recognised date phrases from a string so they don't pollute the task title.</summary>
        private static string StripDatePhrases(string text)
        {
            string result = text;
            result = Regex.Replace(result, @"\btomorrow\b", "", RegexOptions.IgnoreCase);
            result = Regex.Replace(result, @"\btoday\b", "", RegexOptions.IgnoreCase);
            result = Regex.Replace(result, @"\bnext week\b", "", RegexOptions.IgnoreCase);
            result = Regex.Replace(result, @"\bin\s+\d+\s*days?\b", "", RegexOptions.IgnoreCase);
            result = Regex.Replace(result, @"\bin\s+\d+\s*weeks?\b", "", RegexOptions.IgnoreCase);
            result = Regex.Replace(result, @"\s{2,}", " ");
            return result.Trim();
        }

        /// <summary>Clears any pending reminder state (used when the chat is reset).</summary>
        public void ClearPendingState()
        {
            _pendingReminderTask = null;
        }
    }
}
