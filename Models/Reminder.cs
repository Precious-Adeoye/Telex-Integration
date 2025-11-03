namespace Telex_Integration.Models
{
    public class Reminder
    {
        public int Id { get; set; }
        public int TaskId { get; set; }
        public DateTime ReminderTime { get; set; }
        public bool IsSent { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string? ChannelId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public TaskItem? Task { get; set; }
    }
}
