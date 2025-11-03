using System.Data;

namespace Telex_Integration.Models
{
    public class TaskCommand
    {
        public CommandType Type { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public DateTime? DueDate { get; set; }
        public int? TaskId { get; set; }
        public TaskFilter Filter { get; set; } = TaskFilter.All;
        public int? ReminderMinutes { get; set; }
    }
}
