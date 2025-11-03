using System.Text.RegularExpressions;
using Telex_Integration.ITelextServices;
using Telex_Integration.Models;

namespace Telex_Integration.TelexSevices
{
    public class NLPService : INLPService
    {
        public TaskCommand ParseCommand(string message)
        {
            var lowerMessage = message.ToLower().Trim();
            var command = new TaskCommand();

            // Help command
            if (lowerMessage.Contains("help") || lowerMessage == "?")
            {
                command.Type = CommandType.Help;
                return command;
            }

            // Create task
            if (lowerMessage.Contains("create task") || lowerMessage.Contains("add task") || lowerMessage.Contains("new task"))
            {
                command.Type = CommandType.CreateTask;
                command.Title = ExtractTaskTitle(message);
                command.DueDate = ExtractDueDate(message);
                return command;
            }

            // List tasks
            if (lowerMessage.Contains("list") || lowerMessage.Contains("show") || lowerMessage.Contains("my tasks"))
            {
                command.Type = CommandType.ListTasks;
                command.Filter = ExtractFilter(lowerMessage);
                return command;
            }

            // Complete task
            if (lowerMessage.Contains("complete") || lowerMessage.Contains("finish") || lowerMessage.Contains("done"))
            {
                command.Type = CommandType.CompleteTask;
                command.TaskId = ExtractTaskId(message);
                return command;
            }

            // Delete task
            if (lowerMessage.Contains("delete") || lowerMessage.Contains("remove"))
            {
                command.Type = CommandType.DeleteTask;
                command.TaskId = ExtractTaskId(message);
                return command;
            }

            // Set reminder
            if (lowerMessage.Contains("remind"))
            {
                command.Type = CommandType.SetReminder;
                command.Title = ExtractReminderText(message);
                command.ReminderMinutes = ExtractReminderTime(message);
                return command;
            }

            command.Type = CommandType.Unknown;
            return command;
        }

        private string ExtractTaskTitle(string message)
        {
            // Extract text between quotes
            var match = Regex.Match(message, "\"([^\"]+)\"");
            if (match.Success)
            {
                return match.Groups[1].Value;
            }

            // Extract text after "task" keyword
            var taskMatch = Regex.Match(message, @"task\s+(.+?)(?:\s+due|\s+by|$)", RegexOptions.IgnoreCase);
            if (taskMatch.Success)
            {
                return taskMatch.Groups[1].Value.Trim();
            }

            return "Untitled Task";
        }

        private DateTime? ExtractDueDate(string message)
        {
            var lowerMessage = message.ToLower();
            var now = DateTime.Now;

            // Check for "today"
            if (lowerMessage.Contains("today"))
            {
                return now.Date.AddHours(17); // Default to 5 PM today
            }

            // Check for "tomorrow"
            if (lowerMessage.Contains("tomorrow"))
            {
                return now.Date.AddDays(1).AddHours(17);
            }

            // Check for day of week
            var daysOfWeek = new[] { "monday", "tuesday", "wednesday", "thursday", "friday", "saturday", "sunday" };
            foreach (var day in daysOfWeek)
            {
                if (lowerMessage.Contains(day))
                {
                    var targetDay = (DayOfWeek)Array.IndexOf(daysOfWeek, day);
                    var daysUntil = ((int)targetDay - (int)now.DayOfWeek + 7) % 7;
                    if (daysUntil == 0) daysUntil = 7; // Next week if same day

                    var targetDate = now.Date.AddDays(daysUntil);

                    // Try to extract time
                    var timeMatch = Regex.Match(message, @"(\d{1,2})\s*(am|pm)", RegexOptions.IgnoreCase);
                    if (timeMatch.Success)
                    {
                        var hour = int.Parse(timeMatch.Groups[1].Value);
                        if (timeMatch.Groups[2].Value.ToLower() == "pm" && hour != 12) hour += 12;
                        if (timeMatch.Groups[2].Value.ToLower() == "am" && hour == 12) hour = 0;
                        return targetDate.AddHours(hour);
                    }

                    return targetDate.AddHours(17);
                }
            }

            // Check for specific date patterns (e.g., "Nov 5", "November 5")
            var dateMatch = Regex.Match(message, @"(jan|feb|mar|apr|may|jun|jul|aug|sep|oct|nov|dec)[a-z]*\s+(\d{1,2})", RegexOptions.IgnoreCase);
            if (dateMatch.Success)
            {
                var monthStr = dateMatch.Groups[1].Value.ToLower();
                var day = int.Parse(dateMatch.Groups[2].Value);

                var months = new[] { "jan", "feb", "mar", "apr", "may", "jun", "jul", "aug", "sep", "oct", "nov", "dec" };
                var month = Array.IndexOf(months, monthStr) + 1;

                try
                {
                    var year = now.Year;
                    var targetDate = new DateTime(year, month, day);
                    if (targetDate < now.Date) targetDate = targetDate.AddYears(1);

                    return targetDate.AddHours(17);
                }
                catch
                {
                    // Invalid date
                }
            }

            return null;
        }

        private int? ExtractTaskId(string message)
        {
            var match = Regex.Match(message, @"#(\d+)");
            if (match.Success)
            {
                return int.Parse(match.Groups[1].Value);
            }

            match = Regex.Match(message, @"\b(\d+)\b");
            if (match.Success)
            {
                return int.Parse(match.Groups[1].Value);
            }

            return null;
        }

        private TaskFilter ExtractFilter(string message)
        {
            var lowerMessage = message.ToLower();

            if (lowerMessage.Contains("completed") || lowerMessage.Contains("done"))
                return TaskFilter.Completed;

            if (lowerMessage.Contains("pending") || lowerMessage.Contains("active"))
                return TaskFilter.Pending;

            if (lowerMessage.Contains("overdue"))
                return TaskFilter.Overdue;

            if (lowerMessage.Contains("today"))
                return TaskFilter.Today;

            if (lowerMessage.Contains("week") || lowerMessage.Contains("this week"))
                return TaskFilter.ThisWeek;

            return TaskFilter.All;
        }

        private string ExtractReminderText(string message)
        {
            var match = Regex.Match(message, @"about\s+(.+?)(?:\s+in|\s+at|$)", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return match.Groups[1].Value.Trim();
            }

            return "Reminder";
        }

        private int ExtractReminderTime(string message)
        {
            var match = Regex.Match(message, @"in\s+(\d+)\s*(minute|min|hour|hr)", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var amount = int.Parse(match.Groups[1].Value);
                var unit = match.Groups[2].Value.ToLower();

                if (unit.StartsWith("hour") || unit.StartsWith("hr"))
                {
                    return amount * 60;
                }

                return amount;
            }

            // Default to 30 minutes
            return 30;
        }
    }

}

