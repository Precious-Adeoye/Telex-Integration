using Telex_Integration.Models;

namespace Telex_Integration.ITelextServices
{
    public interface ITaskService
    {
        Task<TaskItem> CreateTaskAsync(string title, string? description, DateTime? dueDate, string userId, string? channelId);
        Task<List<TaskItem>> GetTasksAsync(string userId, TaskFilter filter, string? channelId);
        Task<TaskItem?> GetTaskByIdAsync(int taskId);
        Task<bool> CompleteTaskAsync(int taskId, string userId);
        Task<bool> DeleteTaskAsync(int taskId, string userId);
        Task<Reminder> CreateReminderAsync(string title, int minutes, string userId, string? channelId);
        Task<List<Reminder>> GetPendingRemindersAsync();
        Task MarkReminderAsSentAsync(int reminderId);
    }
}
