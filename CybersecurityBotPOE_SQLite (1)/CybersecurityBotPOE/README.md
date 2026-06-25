# Cybersecurity Awareness Bot — Part 3 / POE (PROG6221)

A WPF chatbot that educates South African citizens about cybersecurity, with a
**Task Assistant (SQLite-backed)**, a **cybersecurity quiz**, an **NLP-style command
recogniser**, and an **Activity Log** — built on top of the Part 1 and Part 2 features.

---

## ⚠️ Before you do anything else — read this

I built and tested this entirely on a Linux sandbox, which **cannot run WPF or connect
to a real SQLite (local file)**. Here's exactly what that means for you:

| Tested by me | NOT tested by me — please check on your machine |
|---|---|
| All chatbot logic (keyword recognition, sentiment, NLP phrase parsing, date parsing, task title extraction) — 18/18 automated tests pass | The actual XAML window opening and rendering in Visual Studio |
| MySQL query syntax and the `Microsoft.Data.Sqlite` API calls (verified against official docs) | A real connection to your SQLite (local file) |
| Every `x:Name` and event handler in the XAML matches the code-behind exactly | Whether Microsoft.Data.Sqlite restores cleanly in your environment |

**First thing to do:** open the project in Visual Studio, let it restore NuGet packages, and hit **Build**. If anything red shows up in the Output window, send me the exact error text (like you did last time) and I will fix it immediately — that loop is fast and reliable, even though I can't pre-empt every possible Visual-Studio-only error from here.

---

## ✅ What's included (Part 1 + 2 + 3, all in one project)

| Feature | Where |
|---|---|
| Voice greeting, ASCII art, console-style theming | Carried into the WPF GUI |
| Keyword recognition, random tips, sentiment detection, memory/recall | Chat tab |
| **Task Assistant with SQLite storage** (add/view/complete/delete, optional reminders) | Tasks tab |
| **Cybersecurity quiz** (11 questions, immediate feedback, final score) | Triggered via chat or sidebar button |
| **NLP simulation** (flexible phrasing: "remind me to...", "add a task to...") | `NlpService.cs` |
| **Activity Log** (last 8 actions, "show full history" toggle) | Activity Log tab |

---

## 🔧 MySQL Setup (required for the Tasks tab to work)

1. Install MySQL — easiest option is **XAMPP** (https://www.apachefriends.org) or **MySQL Workbench**.
2. Start your SQLite (local file) (in XAMPP, click "Start" next to MySQL in the control panel).
3. Run `schema.sql` (included in this project) once, either:
   - In **phpMyAdmin** (XAMPP): Import tab → choose `schema.sql` → Go
   - In **MySQL Workbench**: open `schema.sql` → click the lightning bolt to execute
   - Or via terminal: `mysql -u root -p < schema.sql`
4. Open **`Services/DbConfig.cs`** and confirm these match your setup (defaults match a standard XAMPP install):

```csharp
public static string Server   = "localhost";
public static string Port     = "3306";
public static string Database = "cybersecuritybot";
public static string UserId   = "root";
public static string Password = "";   // set this if your MySQL root user has a password
```

5. Run the app. The **Tasks tab** shows a green banner if the connection works, or a red banner with the exact error if it doesn't — the app **never crashes** because of a DB problem, it just disables saving until you fix the connection.

---

## 🚀 How to Run

```bash
git clone <your-repo-url>
cd CybersecurityBotPOE
dotnet restore
dotnet run
```

Or open `CybersecurityBotGUI.csproj` in Visual Studio 2022, hit Restore, then F5.

**Requirements:** Windows 10/11, .NET 8 SDK, SQLite (local file).

---

## 🎮 Things to try

| Type this | What happens |
|---|---|
| `Add a task to enable 2FA` | Creates a task, asks if you want a reminder |
| `Yes, remind me in 3 days` | Sets the reminder on the task you just added |
| `Remind me to update my password tomorrow` | Adds the task **and** sets the reminder in one go |
| `show tasks` / `show activity log` | Summarises tasks / recent bot actions in chat |
| `quiz` | Starts the 11-question cybersecurity quiz |
| `I'm worried about scams` | Sentiment-aware empathetic response + a scam tip |
| `show memory` | Shows what the bot remembers about you |

You can also use the **Tasks tab** (form-based add/complete/delete) and the
**Activity Log tab** (with a "Show full history" button) directly with the mouse —
everything you do there is logged and reflected in chat too.

---

## ⚠️ One thing you must replace yourself

**`greeting.wav`** in this project is a placeholder beep tone, not a real voice
recording. The brief requires **your own recorded voice** (no AI voices). Record a
short greeting, export it as WAV, and replace the file before submitting.

---

## 📁 Project Structure

```
CybersecurityBotPOE/
├── .github/workflows/dotnet-wpf.yml   ← CI (Windows runner)
├── Models/
│   ├── TaskItem.cs        ← maps to the `tasks` MySQL table
│   ├── LogEntry.cs        ← one activity log record
│   └── UserMemory.cs
├── Services/
│   ├── DbConfig.cs        ← EDIT THIS with your MySQL details
│   ├── DatabaseService.cs ← all MySQL CRUD (try/catch on every method)
│   ├── TaskService.cs     ← business logic + auto-logging
│   ├── NlpService.cs      ← flexible phrase recognition + date parsing
│   ├── ActivityLogService.cs
│   ├── ChatbotService.cs  ← orchestrator
│   ├── ResponseSystem.cs  ← Part 2 cybersecurity Q&A engine
│   ├── SentimentDetector.cs
│   └── AudioPlayer.cs
├── App.xaml / App.xaml.cs
├── MainWindow.xaml         ← Chat / Tasks / Activity Log tabs
├── MainWindow.xaml.cs
├── schema.sql              ← run this in MySQL first
├── greeting.wav            ← REPLACE with your own voice recording
├── CybersecurityBotGUI.csproj
└── README.md
```

---

## 📚 References

Pieterse, H. 2021. The Cyber Threat Landscape in South Africa: A 10-Year Review.
*The African Journal of Information and Communication*, 28(28).
doi: https://doi.org/10.23962/10539/32213
