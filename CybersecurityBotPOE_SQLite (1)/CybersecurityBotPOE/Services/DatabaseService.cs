using System;
using System.Collections.Generic;
using CybersecurityBotGUI.Models;
using Microsoft.Data.Sqlite;

namespace CybersecurityBotGUI.Services
{
    /// <summary>
    /// Handles all SQLite database operations for the Task Assistant feature.
    /// The database file is stored locally in the user's AppData folder —
    /// no external server, no install required.
    /// Every method opens its own short-lived connection (standard ADO.NET pattern)
    /// and is wrapped in try/catch so a DB problem never crashes the GUI.
    /// </summary>
    public class DatabaseService
    {
        /// <summary>
        /// Creates the `tasks` table if it does not already exist.
        /// Safe to call every time the app starts.
        /// </summary>
        public bool EnsureTableExists(out string errorMessage)
        {
            errorMessage = string.Empty;
            const string sql = @"
                CREATE TABLE IF NOT EXISTS tasks (
                    id            INTEGER PRIMARY KEY AUTOINCREMENT,
                    title         TEXT    NOT NULL,
                    description   TEXT,
                    reminder_date TEXT    NULL,
                    is_completed  INTEGER NOT NULL DEFAULT 0,
                    created_at    TEXT    NOT NULL DEFAULT (datetime('now','localtime'))
                );";

            try
            {
                using var conn = new SqliteConnection(DbConfig.ConnectionString);
                conn.Open();
                using var cmd = new SqliteCommand(sql, conn);
                cmd.ExecuteNonQuery();
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }

        /// <summary>Inserts a new task and returns its generated Id (0 on failure).</summary>
        public int AddTask(TaskItem task, out string errorMessage)
        {
            errorMessage = string.Empty;
            const string sql = @"
                INSERT INTO tasks (title, description, reminder_date, is_completed, created_at)
                VALUES (@title, @description, @reminder, @completed, @created);
                SELECT last_insert_rowid();";

            try
            {
                using var conn = new SqliteConnection(DbConfig.ConnectionString);
                conn.Open();
                using var cmd = new SqliteCommand(sql, conn);
                cmd.Parameters.AddWithValue("@title",       task.Title);
                cmd.Parameters.AddWithValue("@description", task.Description ?? string.Empty);
                cmd.Parameters.AddWithValue("@reminder",    task.ReminderDate.HasValue
                                                                ? (object)task.ReminderDate.Value.ToString("yyyy-MM-dd HH:mm:ss")
                                                                : DBNull.Value);
                cmd.Parameters.AddWithValue("@completed",   task.IsCompleted ? 1 : 0);
                cmd.Parameters.AddWithValue("@created",     task.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));

                object? result = cmd.ExecuteScalar();
                return result != null ? Convert.ToInt32(result) : 0;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return 0;
            }
        }

        /// <summary>Retrieves all tasks, most recently created first.</summary>
        public List<TaskItem> GetAllTasks(out string errorMessage)
        {
            errorMessage = string.Empty;
            var tasks = new List<TaskItem>();
            const string sql = @"
                SELECT id, title, description, reminder_date, is_completed, created_at
                FROM tasks
                ORDER BY created_at DESC;";

            try
            {
                using var conn = new SqliteConnection(DbConfig.ConnectionString);
                conn.Open();
                using var cmd    = new SqliteCommand(sql, conn);
                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    tasks.Add(new TaskItem
                    {
                        Id           = reader.GetInt32(0),
                        Title        = reader.GetString(1),
                        Description  = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                        ReminderDate = reader.IsDBNull(3)
                                           ? (DateTime?)null
                                           : DateTime.Parse(reader.GetString(3)),
                        IsCompleted  = reader.GetInt32(4) == 1,
                        CreatedAt    = DateTime.Parse(reader.GetString(5))
                    });
                }
                return tasks;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return tasks; // empty list on failure
            }
        }

        /// <summary>Marks a task as completed (or not) in the database.</summary>
        public bool SetTaskCompleted(int id, bool completed, out string errorMessage)
        {
            errorMessage = string.Empty;
            const string sql = "UPDATE tasks SET is_completed = @completed WHERE id = @id;";

            try
            {
                using var conn = new SqliteConnection(DbConfig.ConnectionString);
                conn.Open();
                using var cmd = new SqliteCommand(sql, conn);
                cmd.Parameters.AddWithValue("@completed", completed ? 1 : 0);
                cmd.Parameters.AddWithValue("@id",        id);
                cmd.ExecuteNonQuery();
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }

        /// <summary>Deletes a task from the database.</summary>
        public bool DeleteTask(int id, out string errorMessage)
        {
            errorMessage = string.Empty;
            const string sql = "DELETE FROM tasks WHERE id = @id;";

            try
            {
                using var conn = new SqliteConnection(DbConfig.ConnectionString);
                conn.Open();
                using var cmd = new SqliteCommand(sql, conn);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }

        /// <summary>Updates the reminder date for an existing task.</summary>
        public bool SetReminder(int id, DateTime? reminderDate, out string errorMessage)
        {
            errorMessage = string.Empty;
            const string sql = "UPDATE tasks SET reminder_date = @reminder WHERE id = @id;";

            try
            {
                using var conn = new SqliteConnection(DbConfig.ConnectionString);
                conn.Open();
                using var cmd = new SqliteCommand(sql, conn);
                cmd.Parameters.AddWithValue("@reminder", reminderDate.HasValue
                                                             ? (object)reminderDate.Value.ToString("yyyy-MM-dd HH:mm:ss")
                                                             : DBNull.Value);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }

        /// <summary>
        /// Quick connectivity / table-existence test used at startup.
        /// With SQLite this simply opens the file (creating it if needed).
        /// </summary>
        public bool TestConnection(out string errorMessage)
        {
            errorMessage = string.Empty;
            try
            {
                using var conn = new SqliteConnection(DbConfig.ConnectionString);
                conn.Open();
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }
    }
}
