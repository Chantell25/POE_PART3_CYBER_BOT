using System.Collections.Generic;
using CybersecurityBotGUI.Models;

namespace CybersecurityBotGUI.Services
{
    /// <summary>
    /// Records every significant action the bot takes during the session
    /// (Part 3 Activity Log Feature). Stored in memory as a List, as permitted by the brief.
    /// </summary>
    public class ActivityLogService
    {
        private readonly List<LogEntry> _entries = new List<LogEntry>();

        /// <summary>Adds a new entry to the log.</summary>
        public void Log(string category, string description)
        {
            _entries.Add(new LogEntry { Category = category, Description = description });
        }

        /// <summary>Returns the most recent N entries (default 10), newest first.</summary>
        public List<LogEntry> GetRecent(int count = 10)
        {
            var result = new List<LogEntry>();
            int start = _entries.Count - count;
            if (start < 0) start = 0;

            for (int i = _entries.Count - 1; i >= start; i--)
                result.Add(_entries[i]);

            return result;
        }

        /// <summary>Returns the full history, newest first (used by "Show more").</summary>
        public List<LogEntry> GetAll()
        {
            var result = new List<LogEntry>();
            for (int i = _entries.Count - 1; i >= 0; i--)
                result.Add(_entries[i]);
            return result;
        }

        public int TotalCount => _entries.Count;
    }
}
