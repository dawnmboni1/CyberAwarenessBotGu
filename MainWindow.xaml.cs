using System;
using System.Collections.Generic;
using System.Media;
using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualBasic;

namespace CyberAwarenessBotGu
{
    public class Question
    {
        public string Text { get; set; }
        public string OptionA { get; set; }
        public string OptionB { get; set; }
        public string OptionC { get; set; }
        public char CorrectOption { get; set; }

        public Question(string text, string a, string b, string c, char correct)
        {
            Text = text;
            OptionA = a;
            OptionB = b;
            OptionC = c;
            CorrectOption = correct;
        }
    }

    public class TaskItem
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime? ReminderDate { get; set; }
        public bool IsCompleted { get; set; } = false;

        public override string ToString()
        {
            return $"{Title} - {(IsCompleted ? "Done" : "Pending")} - {(ReminderDate.HasValue ? ReminderDate.Value.ToShortDateString() : "No Reminder")}";
        }
    }

    public partial class MainWindow : Window
    {
        List<Question> quizQuestions = new List<Question>();
        List<TaskItem> tasks = new List<TaskItem>();
        int currentQuestionIndex = 0;
        string userInterest = "";
        List<string> activityLog = new List<string>();

        Dictionary<string, List<string>> keywordResponses = new Dictionary<string, List<string>>()
        {
            { "password", new List<string> {
                "Use strong, unique passwords for each account.",
                "Avoid using personal details in your passwords.",
                "Consider using a password manager for secure storage."
            }},
            { "phishing", new List<string> {
                "Don't click suspicious links in emails.",
                "Be cautious of urgent emails asking for login details.",
                "Verify sender addresses before trusting any email."
            }},
            { "privacy", new List<string> {
                "Limit the personal information you share online.",
                "Adjust your social media privacy settings.",
                "Avoid using public Wi-Fi for sensitive transactions."
            }},
        };

        Dictionary<string, string> specificQuestions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "what is phishing?", "Phishing is a scam where attackers try to steal sensitive information by pretending to be a trusted entity." },
            { "what is privacy?", "Privacy refers to the control you have over your personal information and how it’s shared." },
            { "how do i protect my password?", "Use a password manager, two-factor authentication, and avoid reusing passwords." },
            { "what is a scam?", "A scam is a deceptive scheme or trick used to cheat someone out of something valuable." },
            {"What are scams?", "A scam is a deceptive scheme or trick used to cheat someone out of something valuable." },
            { "how do i avoid scams?", "Avoid clicking on suspicious links, verify sources, and never share sensitive info unless sure." },
            { "cybersecurity", "Cybersecurity is the practice of protecting systems, networks, and programs from digital attacks." }

        };

        public MainWindow()
        {
            InitializeComponent();
            PlayGreeting();
            LoadQuiz();
            AddBotMessage("Welcome! Ask about cybersecurity, add a task, or type 'start quiz'.");
            CheckReminders();
        }

        private void PlayGreeting()
        {
            try
            {
                SoundPlayer player = new SoundPlayer("greeting.wav");
                player.PlaySync();
            }
            catch
            {
                AddBotMessage("Could not play greeting audio. Ensure 'greeting.wav' exists.");
            }
        }

        private void LoadQuiz()
        {
            quizQuestions.Add(new Question("What is phishing?", "A. A game", "B. A scam to steal info", "C. A type of malware", 'B'));
            quizQuestions.Add(new Question("How can you protect your password?", "A. Share it with friends", "B. Use '1234'", "C. Use a password manager", 'C'));
            quizQuestions.Add(new Question("Which is a secure practice?", "A. Clicking unknown links", "B. Verifying sender emails", "C. Ignoring updates", 'B'));
            quizQuestions.Add(new Question("What is a scam?", "A. A safe offer", "B. A trick for info or money", "C. An app", 'B'));
            quizQuestions.Add(new Question("How to avoid scams?", "A. Trust all messages", "B. Use public Wi-Fi", "C. Verify links and sources", 'C'));
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            string input = UserInputBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(input)) return;

            AddUserMessage(input);
            LogActivity($"User: {input}");
            UserInputBox.Text = "";

            string lowerInput = input.ToLower();

            if (lowerInput.Contains("interested in"))
            {
                int idx = lowerInput.IndexOf("interested in") + "interested in".Length;
                userInterest = lowerInput.Substring(idx).Trim();
                AddBotMessage($"Thanks! I'll remember you're interested in {userInterest}.");
                return;
            }
            // Match common question formats
            foreach (var key in specificQuestions.Keys)
            {
                if (lowerInput.Contains(key) || lowerInput.Contains("tell me about " + key) || lowerInput.StartsWith("what is " + key) || lowerInput.StartsWith("what are " + key))
                {
                    AddBotMessage(specificQuestions[key]);
                    return;
                }
            }

            if (lowerInput.StartsWith("add task:"))
            {
                try
                {
                    var parts = input.Substring(9).Split(';');
                    string title = parts[0].Trim();
                    string desc = parts.Length > 1 ? parts[1].Trim() : "";
                    DateTime? reminder = parts.Length > 2 ? DateTime.Parse(parts[2].Trim()) : (DateTime?)null;
                    AddTask(title, desc, reminder);
                }
                catch
                {
                    AddBotMessage("❌ Format: add task: title; description; YYYY-MM-DD (optional)");
                }
                return;
            }

            if (lowerInput == "show tasks")
            {
                ShowTasks();
                return;
            }

            if (lowerInput.StartsWith("mark done "))
            {
                if (int.TryParse(lowerInput.Substring(10).Trim(), out int idx) && idx > 0 && idx <= tasks.Count)
                {
                    tasks[idx - 1].IsCompleted = true;
                    AddBotMessage($"✅ Task {idx} marked as completed.");
                }
                else AddBotMessage("❌ Invalid task number.");
                return;
            }

            if (lowerInput.StartsWith("delete task "))
            {
                if (int.TryParse(lowerInput.Substring(12).Trim(), out int idx) && idx > 0 && idx <= tasks.Count)
                {
                    string removedTitle = tasks[idx - 1].Title;
                    tasks.RemoveAt(idx - 1);
                    AddBotMessage($"🗑️ Task '{removedTitle}' deleted.");
                }
                else AddBotMessage("❌ Invalid task number.");
                return;
            }

            if (lowerInput == "show activity log")
            {
                DisplayActivityLog();
                return;
            }

            if (lowerInput == "start quiz")
            {
                StartQuiz();
                return;
            }

            if (lowerInput.Contains("exit") || lowerInput.Contains("quit"))
            {
                AddBotMessage("Goodbye! Stay safe online.");
                LogActivity("Chatbot exited.");
                Application.Current.Shutdown();
                return;
            }

            if (specificQuestions.TryGetValue(lowerInput, out string specificResponse))
            {
                AddBotMessage(specificResponse);
                return;
            }

            if (lowerInput.Contains("remind me") && !string.IsNullOrEmpty(userInterest))
            {
                AddBotMessage($"Since you’re interested in {userInterest}, remember to double-check email addresses!");
                return;
            }

            if (lowerInput.Contains("worried") || lowerInput.Contains("scared") || lowerInput.Contains("anxious"))
            {
                string emotionalResponse = "It’s okay to feel that way. I can help you with tips on phishing, passwords, and online safety.";
                AddBotMessage(emotionalResponse);
                LogActivity("Chatbot: Responded to emotion");
                return;
            }

            // Keyword-based NLP
            foreach (var word in lowerInput.Split(' ', '.', ',', '?'))
            {
                if (keywordResponses.TryGetValue(word.ToLower(), out var responsesList))
                {
                    AddBotMessage(responsesList[new Random().Next(responsesList.Count)]);
                    return;
                }
            }

            AddBotMessage("🤔 I didn’t catch that. Try asking about phishing, passwords, or privacy.");
        }

        private void StartQuiz()
        {
            int score = 0;

            foreach (var question in quizQuestions)
            {
                string prompt = $"{question.Text}\n\n" +
                                $"{question.OptionA}\n" +
                                $"{question.OptionB}\n" +
                                $"{question.OptionC}\n\n" +
                                "Enter A, B, or C:";

                string answer = Interaction.InputBox(prompt, "Cybersecurity Quiz", "").ToUpper();

                if (string.IsNullOrEmpty(answer))
                {
                    AddBotMessage("❌ Quiz cancelled.");
                    return;
                }

                if (answer.Length == 1 && answer[0] == question.CorrectOption)
                {
                    AddBotMessage("✅ Correct!");
                    score++;
                }
                else
                {
                    AddBotMessage($"❌ Incorrect. The correct answer was {question.CorrectOption}.");
                }
            }

            string finalScore = $"🎉 You got {score} out of {quizQuestions.Count} correct!";
            AddBotMessage(finalScore);
            MessageBox.Show(finalScore, "Quiz Complete");
        }

        private void AddTask(string title, string description, DateTime? reminder)
        {
            tasks.Add(new TaskItem { Title = title, Description = description, ReminderDate = reminder });
            AddBotMessage($"📝 Task added: {title}\n📅 Reminder: {(reminder.HasValue ? reminder.Value.ToShortDateString() : "None")}");
            LogActivity($"Task added: {title}");
        }

        private void ShowTasks()
        {
            if (tasks.Count == 0)
            {
                AddBotMessage("📋 No tasks available.");
                return;
            }

            AddBotMessage("📋 Task List:");
            for (int i = 0; i < tasks.Count; i++)
            {
                var t = tasks[i];
                AddBotMessage($"{i + 1}. {t.Title} - {(t.IsCompleted ? "✅ Done" : "⏳ Pending")} - {t.ReminderDate?.ToShortDateString() ?? "No Reminder"}");
            }
        }

        private void CheckReminders()
        {
            foreach (var task in tasks)
            {
                if (!task.IsCompleted && task.ReminderDate.HasValue && task.ReminderDate.Value.Date == DateTime.Today)
                {
                    AddBotMessage($"🔔 Reminder: Task '{task.Title}' is due today!");
                }
            }
        }

        private void DisplayActivityLog()
        {
            AddBotMessage("📃 Last 5 actions:");
            for (int i = activityLog.Count - 1, shown = 0; i >= 0 && shown < 5; i--, shown++)
            {
                AddBotMessage($"• {activityLog[i]}");
            }
        }

        private void LogActivity(string activity)
        {
            string entry = $"{DateTime.Now:HH:mm:ss} - {activity}";
            activityLog.Add(entry);
        }

        private void AddUserMessage(string message)
        {
            ChatPanel.Children.Add(new TextBlock
            {
                Text = "You: " + message,
                Margin = new Thickness(5),
                Foreground = System.Windows.Media.Brushes.DarkBlue
            });
        }

        private void AddBotMessage(string message, string color = "DarkGreen")
        {
            ChatPanel.Children.Add(new TextBlock
            {
                Text = "Chatbot: " + message,
                Margin = new Thickness(5),
                TextWrapping = TextWrapping.Wrap,
                Foreground = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString(color)
            });
        }
    }
}
