using System;

namespace CybersecurityBotGUI.Models
{
    /// <summary>
    /// Represents one entry in the chatbot's in-memory activity log
    /// (Part 3 Activity Log Feature requirement).
    /// </summary>
    public class LogEntry
    {
        public DateTime Timestamp  { get; set; } = DateTime.Now;
        public string   Category   { get; set; } = string.Empty; // "Task", "Reminder", "Quiz", "NLP"
        public string   Description{ get; set; } = string.Empty;

        public string Display => "[" + Timestamp.ToString("HH:mm") + "] " + Description;
    }
}
