using Microsoft.EntityFrameworkCore;
using Telex_Integration.Data;
using Telex_Integration.ITelextServices;
using Telex_Integration.Models;

namespace Telex_Integration.TelexSevices
{
    public class TaskService : ITaskService
    {
        private readonly AppDbContext _context;

        public TaskService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<TaskItem> CreateTaskAsync(string title, string? description, DateTime? dueDate, string userId, string? channelId)
        {
            var task = new TaskItem
            {
                Title = title,
                Description = description,
                DueDate = dueDate,
                CreatedBy = userId,
                TelexChannelId = channelId,
                IsCompleted = false
            };

            _context.Tasks.Add(task);
            await _context.SaveChangesAsync();

            return task;
        }

        public async Task<List<TaskItem>> GetTasksAsync(string userId, TaskFilter filter, string? channelId)
        {
            var query = _context.Tasks
                .Where(t => t.CreatedBy == userId);

            if (!string.IsNullOrEmpty(channelId))
            {
                query = query.Where(t => t.TelexChannelId == channelId);
            }

            switch (filter)
            {
                case TaskFilter.Pending:
                    query = query.Where(t => !t.IsCompleted);
                    break;
                case TaskFilter.Completed:
                    query = query.Where(t => t.IsCompleted);
                    break;
                case TaskFilter.Overdue:
                    query = query.Where(t => !t.IsCompleted && t.DueDate.HasValue && t.DueDate.Value < DateTime.Now);
                    break;
                case TaskFilter.Today:
                    var today = DateTime.Now.Date;
                    query = query.Where(t => !t.IsCompleted && t.DueDate.HasValue &&
                        t.DueDate.Value.Date == today);
                    break;
                case TaskFilter.ThisWeek:
                    var weekStart = DateTime.Now.Date;
                    var weekEnd = weekStart.AddDays(7);
                    query = query.Where(t => !t.IsCompleted && t.DueDate.HasValue &&
                        t.DueDate.Value >= weekStart && t.DueDate.Value <= weekEnd);
                    break;
            }

            return await query
                .OrderBy(t => t.IsCompleted)
                .ThenBy(t => t.DueDate)
                .ThenByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<TaskItem?> GetTaskByIdAsync(int taskId)
        {
            return await _context.Tasks.FindAsync(taskId);
        }

        public async Task<bool> CompleteTaskAsync(int taskId, string userId)
        {
            var task = await _context.Tasks.FindAsync(taskId);

            if (task == null || task.CreatedBy != userId)
            {
                return false;
            }

            task.IsCompleted = true;
            task.CompletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DeleteTaskAsync(int taskId, string userId)
        {
            var task = await _context.Tasks.FindAsync(taskId);

            if (task == null || task.CreatedBy != userId)
            {
                return false;
            }

            _context.Tasks.Remove(task);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<Reminder> CreateReminderAsync(string title, int minutes, string userId, string? channelId)
        {
            // Create a task for the reminder
            var task = await CreateTaskAsync(title, "Reminder", DateTime.Now.AddMinutes(minutes), userId, channelId);

            var reminder = new Reminder
            {
                TaskId = task.Id,
                ReminderTime = DateTime.Now.AddMinutes(minutes),
                UserId = userId,
                ChannelId = channelId,
                IsSent = false
            };

            _context.Reminders.Add(reminder);
            await _context.SaveChangesAsync();

            return reminder;
        }

        public async Task<List<Reminder>> GetPendingRemindersAsync()
        {
            return await _context.Reminders
                .Include(r => r.Task)
                .Where(r => !r.IsSent && r.ReminderTime <= DateTime.Now)
                .ToListAsync();
        }

        public async Task MarkReminderAsSentAsync(int reminderId)
        {
            var reminder = await _context.Reminders.FindAsync(reminderId);
            if (reminder != null)
            {
                reminder.IsSent = true;
                await _context.SaveChangesAsync();
            }
        }
    }
}

