using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using CybersecurityBotGUI.Models;
using CybersecurityBotGUI.Services;

namespace CybersecurityBotGUI
{
    public partial class MainWindow : Window
    {
        // ── Services (created once at startup, independent of user name) ──────────
        private readonly DatabaseService     _db          = new DatabaseService();
        private readonly ActivityLogService  _logService  = new ActivityLogService();
        private readonly TaskService         _taskService;
        private readonly NlpService          _nlpService;
        private readonly SentimentDetector   _sentimentDetector = new SentimentDetector();
        private readonly AudioPlayer         _audioPlayer       = new AudioPlayer();

        // Created only after the user enters their name
        private ChatbotService? _chatbot;

        // ── State ─────────────────────────────────────────────────────────────────
        private bool   _isTyping = false;
        private int    _msgCount = 0;
        private string _userName = string.Empty;
        private bool   _logShowingFull = false;

        private const string AsciiArt =
            "  ██████╗██╗   ██╗██████╗ ███████╗██████╗ ███████╗███████╗ ██████╗\n" +
            " ██╔════╝╚██╗ ██╔╝██╔══██╗██╔════╝██╔══██╗██╔════╝██╔════╝██╔════╝\n" +
            " ██║      ╚████╔╝ ██████╔╝█████╗  ██████╔╝███████╗█████╗  ██║     \n" +
            " ██║       ╚██╔╝  ██╔══██╗██╔══╝  ██╔══██╗╚════██║██╔══╝  ██║     \n" +
            " ╚██████╗   ██║   ██████╔╝███████╗██║  ██║███████║███████╗╚██████╗\n" +
            "  ╚═════╝   ╚═╝   ╚═════╝ ╚══════╝╚═╝  ╚═╝╚══════╝╚══════╝ ╚═════╝\n" +
            "    ═══[ CYBERSECURITY AWARENESS BOT  |  PROG6221 POE - PART 3 ]═══";

        public MainWindow()
        {
            InitializeComponent();

            _taskService = new TaskService(_db, _logService);
            _nlpService  = new NlpService(_taskService, _logService);

            AsciiBlock.Text = AsciiArt;
            _audioPlayer.PlayGreeting();

            CheckDatabaseConnection();
            RefreshTasksList();
            RefreshLogList();

            NameInputBox.Focus();
        }

        // ════════════════════════════════════════════════════════════════════════
        //  DATABASE STARTUP CHECK
        // ════════════════════════════════════════════════════════════════════════

        private void CheckDatabaseConnection()
        {
            bool ok = _taskService.Initialise(out string err);
            if (ok)
            {
                DbStatusLabel.Text = "Database connected - tasks will be saved automatically.";
                DbStatusLabel.Foreground = new SolidColorBrush(Color.FromRgb(0x00, 0xFF, 0x88));
                DbStatusBorder.BorderBrush = DbStatusLabel.Foreground;
            }
            else
            {
                DbStatusLabel.Text = "Database NOT connected (" + err + "). " +
                    "Edit Services/DbConfig.cs with your MySQL details, then restart the app. Tasks cannot be saved until then.";
                DbStatusLabel.Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x47, 0x57));
                DbStatusBorder.BorderBrush = DbStatusLabel.Foreground;
            }
        }

        // ════════════════════════════════════════════════════════════════════════
        //  NAME DIALOG
        // ════════════════════════════════════════════════════════════════════════

        private void NameInputBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) StartChat();
        }

        private void BtnStartChat_Click(object sender, RoutedEventArgs e)
        {
            StartChat();
        }

        private void StartChat()
        {
            string name = NameInputBox.Text.Trim();

            if (string.IsNullOrEmpty(name) || name.Length < 2)
            {
                NameErrorLabel.Text       = "Please enter a valid name (at least 2 characters).";
                NameErrorLabel.Visibility = Visibility.Visible;
                NameInputBox.Focus();
                return;
            }
            if (name.Length > 30)
            {
                NameErrorLabel.Text       = "Name must be 30 characters or fewer.";
                NameErrorLabel.Visibility = Visibility.Visible;
                NameInputBox.Focus();
                return;
            }

            _userName = name;
            _chatbot  = new ChatbotService(_userName, _nlpService, _logService);
            NameDialog.Visibility = Visibility.Collapsed;
            RefreshMemorySidebar();
            ShowWelcome();
            InputBox.Focus();
        }

        // ════════════════════════════════════════════════════════════════════════
        //  WELCOME
        // ════════════════════════════════════════════════════════════════════════

        private async void ShowWelcome()
        {
            AddSystemMsg("Session started - Welcome, " + _userName + "!");
            await Task.Delay(400);
            AddBotMsg(
                "Hi " + _userName + "! I am your Cybersecurity Awareness Bot.\n\n" +
                "New in this version, I can also help you manage tasks and reminders:\n" +
                "  - \"Add a task to enable 2FA\"\n" +
                "  - \"Remind me to update my password tomorrow\"\n" +
                "  - \"Show activity log\" or \"What have you done for me?\"\n" +
                "  - \"Quiz\" to test your cybersecurity knowledge\n\n" +
                "Check the Tasks and Activity Log tabs above too. Type 'help' for the full command list.");
            SetStatus("Chat active with " + _userName);
        }

        // ════════════════════════════════════════════════════════════════════════
        //  INPUT EVENTS
        // ════════════════════════════════════════════════════════════════════════

        private void InputBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !_isTyping)
            {
                e.Handled = true;
                _ = SendAsync();
            }
        }

        private void InputBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            PlaceholderLabel.Visibility = string.IsNullOrEmpty(InputBox.Text)
                ? Visibility.Visible : Visibility.Collapsed;

            if (!string.IsNullOrWhiteSpace(InputBox.Text))
                UpdateSentiment(_sentimentDetector.Detect(InputBox.Text));
        }

        private void InputBox_GotFocus(object sender, RoutedEventArgs e)
        {
            PlaceholderLabel.Visibility = Visibility.Collapsed;
        }

        private void InputBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(InputBox.Text))
                PlaceholderLabel.Visibility = Visibility.Visible;
        }

        private async void SendBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!_isTyping) await SendAsync();
        }

        private async Task SendAsync()
        {
            if (_chatbot == null) return;

            string input = InputBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(input)) return;

            InputBox.Clear();
            PlaceholderLabel.Visibility = Visibility.Visible;
            InputBox.IsEnabled          = false;
            SendBtn.IsEnabled           = false;
            _isTyping                   = true;

            AddUserMsg(input);
            UpdateSentiment(_sentimentDetector.Detect(input));

            SetStatus("Bot is thinking...");
            await Task.Delay(300);

            string lowerInput = input.Trim().ToLower();
            string? response;

            if (lowerInput == "help")
                response = _chatbot.GetHelpText();
            else
                response = _chatbot.ProcessMessage(input);

            if (response == null)
            {
                AddBotMsg("Goodbye, " + _userName + "! Stay safe online. Remember: Think before you click!");
                AddSystemMsg("Session ended.");
                SetStatus("Session ended");
            }
            else if (response == "__QUIZ__")
            {
                AddBotMsg("Great! Let's test your cybersecurity knowledge!");
                await Task.Delay(300);
                StartQuiz();
            }
            else
            {
                AddBotMsg(response);
                RefreshMemorySidebar();
                RefreshTasksList();
                RefreshLogList();
                SetStatus("Ready - " + _msgCount + " messages");
            }

            InputBox.IsEnabled = true;
            SendBtn.IsEnabled  = true;
            _isTyping          = false;
            InputBox.Focus();
        }

        // ════════════════════════════════════════════════════════════════════════
        //  TOOLBAR BUTTONS
        // ════════════════════════════════════════════════════════════════════════

        private void BtnHelp_Click(object sender, RoutedEventArgs e)
        {
            if (_chatbot == null) return;
            AddUserMsg("help");
            AddBotMsg(_chatbot.GetHelpText());
        }

        private async void BtnMemory_Click(object sender, RoutedEventArgs e)
        {
            if (_chatbot == null) return;
            InputBox.Text = "show memory";
            await SendAsync();
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            MessagePanel.Children.Clear();
            _msgCount          = 0;
            MsgCountLabel.Text = "";
            AddSystemMsg("Chat cleared.");
        }

        private async void TopicBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_chatbot == null) return;
            if (sender is Button btn && btn.Tag is string tag)
            {
                InputBox.Text = tag;
                await SendAsync();
            }
        }

        // ════════════════════════════════════════════════════════════════════════
        //  CHAT MESSAGE BUBBLES
        // ════════════════════════════════════════════════════════════════════════

        private void AddUserMsg(string text)
        {
            _msgCount++;
            MessagePanel.Children.Add(BuildBubble("[" + _userName + "]", text, "#1A3A2A", "#00FF88", "#00FF88", true));
            MsgCountLabel.Text = _msgCount + " msgs";
            ChatScroll.ScrollToEnd();
        }

        private void AddBotMsg(string text)
        {
            _msgCount++;
            MessagePanel.Children.Add(BuildBubble("[CyberBot]", text, "#1C2333", "#00D4FF", "#E6EDF3", false));
            MsgCountLabel.Text = _msgCount + " msgs";
            ChatScroll.ScrollToEnd();
        }

        private void AddSystemMsg(string text)
        {
            MessagePanel.Children.Add(BuildBubble("[SYSTEM]", text, "#1A180F", "#FFC107", "#FFC107", false, isSystem: true));
            ChatScroll.ScrollToEnd();
        }

        private static UIElement BuildBubble(string label, string text, string bgHex, string borderHex, string textHex, bool rightAlign, bool isSystem = false)
        {
            var outer = new Grid { Margin = new Thickness(0, 3, 0, 3) };

            var border = new Border
            {
                Background      = (SolidColorBrush)new BrushConverter().ConvertFrom(bgHex)!,
                BorderBrush     = (SolidColorBrush)new BrushConverter().ConvertFrom(borderHex)!,
                BorderThickness = new Thickness(1),
                Padding         = new Thickness(10, 7, 10, 7),
                MaxWidth        = 600,
                HorizontalAlignment = isSystem ? HorizontalAlignment.Center
                                    : rightAlign ? HorizontalAlignment.Right : HorizontalAlignment.Left
            };

            var stack = new StackPanel();

            var labelRow = new Grid();
            labelRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            labelRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var labelBlock = new TextBlock
            {
                Text = label,
                Foreground = (SolidColorBrush)new BrushConverter().ConvertFrom(borderHex)!,
                FontFamily = new FontFamily("Consolas"), FontSize = 10, FontWeight = FontWeights.Bold
            };
            Grid.SetColumn(labelBlock, 0);

            var timeBlock = new TextBlock
            {
                Text = DateTime.Now.ToString("HH:mm"),
                Foreground = new SolidColorBrush(Color.FromRgb(0x8B, 0x94, 0x9E)),
                FontFamily = new FontFamily("Consolas"), FontSize = 10
            };
            Grid.SetColumn(timeBlock, 1);

            labelRow.Children.Add(labelBlock);
            labelRow.Children.Add(timeBlock);
            stack.Children.Add(labelRow);

            stack.Children.Add(new TextBlock
            {
                Text = text,
                Foreground = (SolidColorBrush)new BrushConverter().ConvertFrom(textHex)!,
                FontFamily = new FontFamily("Consolas"), FontSize = 13,
                TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 4, 0, 0), LineHeight = 19
            });

            border.Child = stack;
            outer.Children.Add(border);
            return outer;
        }

        // ════════════════════════════════════════════════════════════════════════
        //  TASKS TAB
        // ════════════════════════════════════════════════════════════════════════

        private void BtnAddTask_Click(object sender, RoutedEventArgs e)
        {
            string title = TaskTitleBox.Text.Trim();
            string desc  = TaskDescBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(title))
            {
                MessageBox.Show("Please enter a task title.", "Missing title", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DateTime? reminder = GetReminderFromCombo();

            var task = _taskService.AddTask(title, desc, reminder, out bool success, out string err);

            if (success)
            {
                TaskTitleBox.Clear();
                TaskDescBox.Clear();
                ReminderCombo.SelectedIndex = 0;

                if (_chatbot != null)
                    AddSystemMsg("Task added via form: '" + title + "'" +
                        (reminder.HasValue ? " (reminder: " + reminder.Value.ToString("dd MMM yyyy") + ")" : ""));

                RefreshTasksList();
                RefreshLogList();
            }
            else
            {
                MessageBox.Show("Could not save the task. Database error:\n" + err, "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private DateTime? GetReminderFromCombo()
        {
            if (ReminderCombo.SelectedItem is not ComboBoxItem item) return null;
            string text = item.Content?.ToString() ?? "No reminder";

            return text switch
            {
                "Tomorrow"    => DateTime.Today.AddDays(1),
                "In 3 days"   => DateTime.Today.AddDays(3),
                "In 7 days"   => DateTime.Today.AddDays(7),
                "In 14 days"  => DateTime.Today.AddDays(14),
                "In 30 days"  => DateTime.Today.AddDays(30),
                _             => null
            };
        }

        private void RefreshTasksList()
        {
            TasksListPanel.Children.Clear();
            var tasks = _taskService.GetAllTasks(out bool success, out string err);

            if (!success)
            {
                TasksListPanel.Children.Add(new TextBlock
                {
                    Text = "Could not load tasks: " + err,
                    Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x47, 0x57)),
                    FontFamily = new FontFamily("Consolas"), FontSize = 12, TextWrapping = TextWrapping.Wrap
                });
                return;
            }

            if (tasks.Count == 0)
            {
                TasksListPanel.Children.Add(new TextBlock
                {
                    Text = "No tasks yet. Add one above, or say 'Add a task to enable 2FA' in chat!",
                    Foreground = new SolidColorBrush(Color.FromRgb(0x8B, 0x94, 0x9E)),
                    FontFamily = new FontFamily("Consolas"), FontSize = 12, TextWrapping = TextWrapping.Wrap
                });
                return;
            }

            foreach (var task in tasks)
                TasksListPanel.Children.Add(BuildTaskRow(task));
        }

        private UIElement BuildTaskRow(TaskItem task)
        {
            var outer = new Border
            {
                Background      = new SolidColorBrush(Color.FromRgb(0x0D, 0x11, 0x17)),
                BorderBrush     = new SolidColorBrush(task.IsCompleted ? Color.FromRgb(0x00, 0xFF, 0x88) : Color.FromRgb(0x30, 0x36, 0x3D)),
                BorderThickness = new Thickness(1),
                Padding         = new Thickness(10, 8, 10, 8),
                Margin          = new Thickness(0, 0, 0, 8)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var textStack = new StackPanel();
            textStack.Children.Add(new TextBlock
            {
                Text = (task.IsCompleted ? "[DONE] " : "") + task.Title,
                Foreground = new SolidColorBrush(task.IsCompleted ? Color.FromRgb(0x8B, 0x94, 0x9E) : Color.FromRgb(0xE6, 0xED, 0xF3)),
                FontFamily = new FontFamily("Consolas"), FontSize = 13, FontWeight = FontWeights.Bold,
                TextDecorations = task.IsCompleted ? TextDecorations.Strikethrough : null,
                TextWrapping = TextWrapping.Wrap
            });
            if (!string.IsNullOrWhiteSpace(task.Description) && task.Description != task.Title)
            {
                textStack.Children.Add(new TextBlock
                {
                    Text = task.Description,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x8B, 0x94, 0x9E)),
                    FontFamily = new FontFamily("Consolas"), FontSize = 11,
                    TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 2, 0, 0)
                });
            }
            textStack.Children.Add(new TextBlock
            {
                Text = task.ReminderDisplay,
                Foreground = new SolidColorBrush(Color.FromRgb(0x00, 0xD4, 0xFF)),
                FontFamily = new FontFamily("Consolas"), FontSize = 11, Margin = new Thickness(0, 4, 0, 0)
            });
            Grid.SetColumn(textStack, 0);

            var btnPanel = new StackPanel { Orientation = Orientation.Horizontal };

            var completeBtn = new Button
            {
                Content = task.IsCompleted ? "Reopen" : "Complete",
                Width = 80, Height = 28, Margin = new Thickness(0, 0, 6, 0),
                Background = new SolidColorBrush(Color.FromRgb(0x16, 0x1B, 0x22)),
                Foreground = new SolidColorBrush(Color.FromRgb(0x00, 0xFF, 0x88)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x00, 0xFF, 0x88)),
                BorderThickness = new Thickness(1), FontFamily = new FontFamily("Consolas"),
                FontSize = 11, Cursor = Cursors.Hand
            };
            completeBtn.Click += (s, e) => ToggleTaskCompleted(task);

            var deleteBtn = new Button
            {
                Content = "Delete",
                Width = 70, Height = 28,
                Background = new SolidColorBrush(Color.FromRgb(0x16, 0x1B, 0x22)),
                Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x47, 0x57)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0x47, 0x57)),
                BorderThickness = new Thickness(1), FontFamily = new FontFamily("Consolas"),
                FontSize = 11, Cursor = Cursors.Hand
            };
            deleteBtn.Click += (s, e) => DeleteTaskRow(task);

            btnPanel.Children.Add(completeBtn);
            btnPanel.Children.Add(deleteBtn);
            Grid.SetColumn(btnPanel, 1);

            grid.Children.Add(textStack);
            grid.Children.Add(btnPanel);
            outer.Child = grid;
            return outer;
        }

        private void ToggleTaskCompleted(TaskItem task)
        {
            bool newState = !task.IsCompleted;
            bool ok = _taskService.SetCompleted(task.Id, task.Title, newState, out string err);
            if (!ok)
                MessageBox.Show("Could not update the task: " + err, "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);

            RefreshTasksList();
            RefreshLogList();
        }

        private void DeleteTaskRow(TaskItem task)
        {
            var result = MessageBox.Show("Delete task '" + task.Title + "'?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

            bool ok = _taskService.DeleteTask(task.Id, task.Title, out string err);
            if (!ok)
                MessageBox.Show("Could not delete the task: " + err, "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);

            RefreshTasksList();
            RefreshLogList();
        }

        // ════════════════════════════════════════════════════════════════════════
        //  ACTIVITY LOG TAB
        // ════════════════════════════════════════════════════════════════════════

        private void BtnShowMoreLog_Click(object sender, RoutedEventArgs e)
        {
            _logShowingFull = !_logShowingFull;
            BtnShowMoreLog.Content = _logShowingFull ? "Show recent only" : "Show full history";
            RefreshLogList();
        }

        private void RefreshLogList()
        {
            LogListPanel.Children.Clear();
            var entries = _logShowingFull ? _logService.GetAll() : _logService.GetRecent(8);

            if (entries.Count == 0)
            {
                LogListPanel.Children.Add(new TextBlock
                {
                    Text = "No activity yet this session.",
                    Foreground = new SolidColorBrush(Color.FromRgb(0x8B, 0x94, 0x9E)),
                    FontFamily = new FontFamily("Consolas"), FontSize = 12
                });
                return;
            }

            foreach (var entry in entries)
            {
                LogListPanel.Children.Add(new TextBlock
                {
                    Text = entry.Display,
                    Foreground = new SolidColorBrush(Color.FromRgb(0xE6, 0xED, 0xF3)),
                    FontFamily = new FontFamily("Consolas"), FontSize = 12,
                    TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 6)
                });
            }
        }

        // ════════════════════════════════════════════════════════════════════════
        //  QUIZ (carried over and adapted from Part 2)
        // ════════════════════════════════════════════════════════════════════════

        private static readonly List<(string Q, string[] Choices, int Correct, string Explanation)> QuizData = new()
        {
            ("What should you do if an email asks for your password?",
             new[] { "A) Reply with your password", "B) Delete the email", "C) Report it as phishing", "D) Ignore it" },
             2, "Reporting phishing emails helps protect everyone. Legitimate services NEVER ask for your password via email."),

            ("How long should a strong password be?",
             new[] { "A) 4 characters", "B) 6 characters", "C) 8 characters", "D) 12 or more characters" },
             3, "Passwords should be at least 12 characters long. Longer is always better for security."),

            ("True or False: Using the same strong password for all accounts is safe.",
             new[] { "A) True", "B) False" },
             1, "FALSE! If one site is breached, attackers try your password everywhere. Always use unique passwords."),

            ("What does HTTPS in a URL mean?",
             new[] { "A) The site is fast", "B) The connection is encrypted", "C) The site is free", "D) It is government-owned" },
             1, "HTTPS means the connection is encrypted. Never enter personal info on plain HTTP sites."),

            ("Which of these is social engineering?",
             new[] { "A) A software update pop-up", "B) A call from 'Microsoft' saying your PC is hacked", "C) A login page asking for your password", "D) An antivirus scan" },
             1, "Unsolicited calls claiming your PC is infected are classic social engineering / vishing attacks."),

            ("True or False: Public Wi-Fi is safe for online banking.",
             new[] { "A) True", "B) False" },
             1, "FALSE! Public Wi-Fi can be intercepted. Use a VPN or mobile data for sensitive transactions."),

            ("What is two-factor authentication (2FA)?",
             new[] { "A) Logging in twice", "B) Using two browsers", "C) A second verification step beyond your password", "D) Two email accounts" },
             2, "2FA adds a second layer of security. Even if your password is stolen, the attacker still cannot log in."),

            ("What is phishing?",
             new[] { "A) A secure login method", "B) Tricking users into revealing info via fake messages", "C) A type of encryption", "D) A firewall technique" },
             1, "Phishing uses deceptive emails or websites to steal your credentials and personal data."),

            ("How often should you update your software?",
             new[] { "A) Never", "B) Once a year", "C) As soon as updates are available", "D) Only when a friend says to" },
             2, "Updates patch security vulnerabilities. Delaying updates leaves known holes open to attackers."),

            ("True or False: A padlock icon means a website is completely trustworthy.",
             new[] { "A) True", "B) False" },
             1, "FALSE! HTTPS only means the connection is encrypted. Phishing sites also use HTTPS. Always check the domain carefully."),

            ("What is the safest way to store passwords?",
             new[] { "A) Write them in a notebook", "B) Same password for everything", "C) A reputable password manager", "D) A text file on your desktop" },
             2, "Password managers generate and store strong, unique passwords securely for every account."),
        };

        private int  _quizIndex  = 0;
        private int  _quizScore  = 0;

        private void StartQuiz()
        {
            _quizIndex = 0;
            _quizScore = 0;
            _logService.Log("Quiz", "Quiz started - " + QuizData.Count + " questions");
            AddSystemMsg("QUIZ STARTED - " + QuizData.Count + " questions");
            ShowQuestion();
        }

        private void ShowQuestion()
        {
            if (_quizIndex >= QuizData.Count) { EndQuiz(); return; }

            var (q, choices, _, _) = QuizData[_quizIndex];

            var outer = new Grid { Margin = new Thickness(0, 4, 0, 4) };
            var bubble = new Border
            {
                Background      = new SolidColorBrush(Color.FromRgb(0x0D, 0x1B, 0x2A)),
                BorderBrush     = new SolidColorBrush(Color.FromRgb(0x00, 0xD4, 0xFF)),
                BorderThickness = new Thickness(1),
                Padding         = new Thickness(12, 10, 12, 10),
                MaxWidth        = 620,
                HorizontalAlignment = HorizontalAlignment.Left
            };

            var panel = new StackPanel();
            panel.Children.Add(new TextBlock
            {
                Text = "[CyberBot]  Q" + (_quizIndex + 1) + "/" + QuizData.Count,
                Foreground = new SolidColorBrush(Color.FromRgb(0x00, 0xD4, 0xFF)),
                FontFamily = new FontFamily("Consolas"), FontSize = 10, FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 6)
            });
            panel.Children.Add(new TextBlock
            {
                Text = q,
                Foreground = new SolidColorBrush(Color.FromRgb(0xE6, 0xED, 0xF3)),
                FontFamily = new FontFamily("Consolas"), FontSize = 13, FontWeight = FontWeights.Bold,
                TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 8)
            });

            var btnPanel = new StackPanel();
            for (int i = 0; i < choices.Length; i++)
            {
                int idx = i;
                var btn = new Button
                {
                    Content = choices[i], Height = 30, Margin = new Thickness(0, 2, 0, 2),
                    Padding = new Thickness(10, 0, 10, 0),
                    Background = new SolidColorBrush(Color.FromRgb(0x16, 0x1B, 0x22)),
                    Foreground = new SolidColorBrush(Color.FromRgb(0xE6, 0xED, 0xF3)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(0x30, 0x36, 0x3D)),
                    BorderThickness = new Thickness(1), FontFamily = new FontFamily("Consolas"),
                    FontSize = 12, Cursor = Cursors.Hand,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    HorizontalContentAlignment = HorizontalAlignment.Left
                };
                btn.Click += (s, e) => HandleQuizAnswer(idx, btnPanel);
                btnPanel.Children.Add(btn);
            }

            panel.Children.Add(btnPanel);
            bubble.Child = panel;
            outer.Children.Add(bubble);
            MessagePanel.Children.Add(outer);
            ChatScroll.ScrollToEnd();
        }

        private void HandleQuizAnswer(int selected, StackPanel btnPanel)
        {
            var (_, choices, correct, explanation) = QuizData[_quizIndex];
            bool isCorrect = selected == correct;
            if (isCorrect) _quizScore++;

            for (int i = 0; i < btnPanel.Children.Count; i++)
            {
                if (btnPanel.Children[i] is Button b)
                {
                    b.IsEnabled = false;
                    if (i == correct)
                        b.Background = new SolidColorBrush(Color.FromRgb(0x1A, 0x3A, 0x2A));
                    else if (i == selected && !isCorrect)
                        b.Background = new SolidColorBrush(Color.FromRgb(0x3A, 0x1A, 0x1A));
                }
            }

            string feedback = (isCorrect ? "CORRECT! " : "Incorrect. ") + explanation;
            AddBotMsg(feedback);

            _quizIndex++;
            Task.Delay(700).ContinueWith(_ => Dispatcher.Invoke(ShowQuestion));
        }

        private void EndQuiz()
        {
            int total = QuizData.Count;
            double pct = (_quizScore / (double)total) * 100;

            string grade = pct >= 90 ? "Outstanding! You are a cybersecurity pro!" :
                           pct >= 70 ? "Great job! You know your stuff." :
                           pct >= 50 ? "Good effort! Keep learning to stay safe." :
                                       "Keep studying - cybersecurity knowledge could save you one day!";

            AddBotMsg("QUIZ COMPLETE!\n\nScore: " + _quizScore + "/" + total +
                      " (" + pct.ToString("0") + "%)\n\n" + grade +
                      "\n\nType 'quiz' to try again, or ask me anything about cybersecurity!");

            _logService.Log("Quiz", "Quiz completed - score " + _quizScore + "/" + total);
            AddSystemMsg("Quiz ended");
            RefreshLogList();
        }

        // ════════════════════════════════════════════════════════════════════════
        //  SIDEBAR & STATUS
        // ════════════════════════════════════════════════════════════════════════

        private void RefreshMemorySidebar()
        {
            if (_chatbot == null) return;
            var mem = _chatbot.Memory;
            MemNameLabel.Text  = "Name: "       + mem.Name;
            MemTopicLabel.Text = "Interest: "   + (string.IsNullOrEmpty(mem.FavouriteTopic) ? "-" : mem.FavouriteTopic);
            MemLastLabel.Text  = "Last topic: " + (string.IsNullOrEmpty(mem.LastTopic)      ? "-" : mem.LastTopic);
        }

        private void UpdateSentiment(Sentiment s)
        {
            var (label, hex) = s switch
            {
                Sentiment.Worried    => ("Worried",    "#FF4757"),
                Sentiment.Frustrated => ("Frustrated", "#FF9100"),
                Sentiment.Curious    => ("Curious",    "#00D4FF"),
                Sentiment.Positive   => ("Positive",   "#00FF88"),
                _                   => ("Neutral",    "#8B949E")
            };
            SentimentLabel.Text         = label;
            SentimentLabel.Foreground   = (SolidColorBrush)new BrushConverter().ConvertFrom(hex)!;
            SentimentBorder.BorderBrush = SentimentLabel.Foreground;
        }

        private void SetStatus(string msg) => StatusLabel.Text = msg;
    }
}
