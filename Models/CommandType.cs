namespace Telex_Integration.Models
{
    public enum CommandType
    {
         Unknown,
        CreateTask,
        ListTasks,
        CompleteTask,
        DeleteTask,
        SetReminder,
        Help
    }
}
