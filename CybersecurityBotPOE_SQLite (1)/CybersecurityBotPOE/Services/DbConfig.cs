using System;
using System.IO;

namespace CybersecurityBotGUI.Services
{
    /// <summary>
    /// SQLite database path configuration.
    /// The .db file is stored in the user's local AppData folder so it
    /// persists between sessions without needing admin rights.
    /// </summary>
    public static class DbConfig
    {
        /// <summary>
        /// Full path to the SQLite database file.
        /// e.g. C:\Users\Alice\AppData\Local\CybersecurityBot\cybersecuritybot.db
        /// </summary>
        public static string DatabasePath
        {
            get
            {
                string folder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "CybersecurityBot");
                Directory.CreateDirectory(folder); // safe no-op if already exists
                return Path.Combine(folder, "cybersecuritybot.db");
            }
        }

        /// <summary>Builds the SQLite connection string from the path above.</summary>
        public static string ConnectionString => $"Data Source={DatabasePath};";
    }
}
