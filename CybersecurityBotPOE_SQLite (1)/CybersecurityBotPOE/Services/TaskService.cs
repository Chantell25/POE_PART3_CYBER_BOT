using System;
using System.Collections.Generic;
using CybersecurityBotGUI.Models;

namespace CybersecurityBotGUI.Services
{
    /// <summary>
    /// Business-logic layer for cybersecurity tasks. Wraps DatabaseService
    /// and automatically records every action to the ActivityLogService (Part 3).
    /// </summary>
    public class TaskService
    {
        private readonly DatabaseService     _db;
        private readonly ActivityLogService  _log;

        public TaskService(DatabaseService db, ActivityLogService log)
        {
            _db  = db;
            _log = log;
        }

        /// <summary>
        /// Adds a new task. Returns the created TaskItem (Id will be 0 if the DB write failed),
        /// and outputs an error message if something went wrong.
        /// </summary>
        public TaskItem AddTask(string title, string description, DateTime? reminder, out bool success, out string errorMessage)
        {
            var task = new TaskItem
            {
                Title        = title,
                Description  = string.IsNullOrWhiteSpace(description) ? title : description,
                ReminderDate = reminder,
                IsCompleted  = false,
                CreatedAt    = DateTime.Now
            };

            int newId = _db.AddTask(task, out errorMessage);
            success    = newId > 0;
            task.Id    = newId;

            if (success)
            {
                _log.Log("Task", "Task added: '" + title + "'" +
                    (reminder.HasValue ? " (Reminder set for " + reminder.Value.ToString("dd MMM yyyy") + ")" : " (no reminder set)"));
            }
            else
            {
                _log.Log("Task", "Failed to add task '" + title + "': " + errorMessage);
            }

            return task;
        }

        /// <summary>Retrieves all tasks from the database.</summary>
        public List<TaskItem> GetAllTasks(out bool success, out string errorMessage)
        {
            var tasks = _db.GetAllTasks(out errorMessage);
            success = string.IsNullOrEmpty(errorMessage);
            return tasks;
        }

        /// <summary>Marks a task complete/incomplete and logs the action.</summary>
        public bool SetCompleted(int id, string title, bool completed, out string errorMessage)
        {
            bool ok = _db.SetTaskCompleted(id, completed, out errorMessage);
            if (ok)
                _log.Log("Task", (completed ? "Task completed: '" : "Task reopened: '") + title + "'");
            return ok;
        }

        /// <summary>Deletes a task and logs the action.</summary>
        public bool DeleteTask(int id, string title, out string errorMessage)
        {
            bool ok = _db.DeleteTask(id, out errorMessage);
            if (ok)
                _log.Log("Task", "Task deleted: '" + title + "'");
            return ok;
        }

        /// <summary>Sets/updates a reminder date for an existing task and logs the action.</summary>
        public bool SetReminder(int id, string title, DateTime reminder, out string errorMessage)
        {
            bool ok = _db.SetReminder(id, reminder, out errorMessage);
            if (ok)
                _log.Log("Reminder", "Reminder set for '" + title + "' on " + reminder.ToString("dd MMM yyyy"));
            return ok;
        }

        /// <summary>Checks the database connection and creates the table if needed.</summary>
        public bool Initialise(out string errorMessage)
        {
            return _db.EnsureTableExists(out errorMessage);
        }
    }
}
