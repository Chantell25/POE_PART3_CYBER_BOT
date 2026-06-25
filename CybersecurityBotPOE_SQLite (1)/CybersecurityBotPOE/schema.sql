-- ============================================================
-- Cybersecurity Awareness Bot - SQLite Schema
-- ============================================================
-- This file is for reference only. The app creates the table
-- automatically at startup via DatabaseService.EnsureTableExists().
-- The database file is stored at:
--   %LOCALAPPDATA%\CybersecurityBot\cybersecuritybot.db
-- ============================================================

CREATE TABLE IF NOT EXISTS tasks (
    id            INTEGER PRIMARY KEY AUTOINCREMENT,
    title         TEXT    NOT NULL,
    description   TEXT,
    reminder_date TEXT    NULL,
    is_completed  INTEGER NOT NULL DEFAULT 0,
    created_at    TEXT    NOT NULL DEFAULT (datetime('now','localtime'))
);

-- Optional sample tasks (the app seeds these on first run if the table is empty):
INSERT INTO tasks (title, description, reminder_date, is_completed)
VALUES
    ('Enable two-factor authentication', 'Turn on 2FA for your email and banking accounts.', NULL, 0),
    ('Review privacy settings', 'Check what personal info is visible on your social media accounts.', NULL, 0);
