using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Telex_Integration.ITelextServices;
using Telex_Integration.Models;

namespace Telex_Integration.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class A2AController : ControllerBase
    {
        private readonly ITaskService _taskService;
        private readonly INLPService _nlpService;
        private readonly ILogger<A2AController> _logger;

        public A2AController(
            ITaskService taskService,
            INLPService nlpService,
            ILogger<A2AController> logger)
        {
            _taskService = taskService;
            _nlpService = nlpService;
            _logger = logger;
        }

        [HttpPost("taskAgent")]
        public async Task<IActionResult> ProcessMessage([FromBody] A2ARequest request)
        {
            try
            {
                _logger.LogInformation($"Received message: {request.Message} from user: {request.User?.Id}");

                if (string.IsNullOrWhiteSpace(request.Message))
                {
                    return Ok(new A2AResponse
                    {
                        Text = "Please provide a message. Type 'help' to see available commands."
                    });
                }

                var userId = request.User?.Id ?? "anonymous";
                var channelId = request.ChannelId;

                var command = _nlpService.ParseCommand(request.Message);
                var response = await ExecuteCommand(command, userId, channelId);

                return Ok(new A2AResponse { Text = response });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message");
                return Ok(new A2AResponse
                {
                    Text = $"Sorry, I encountered an error: {ex.Message}. Please try again."
                });
            }
        }

        [HttpGet("health")]
        public IActionResult Health()
        {
            return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
        }

        private async Task<string> ExecuteCommand(TaskCommand command, string userId, string? channelId)
        {
            switch (command.Type)
            {
                case CommandType.CreateTask:
                    return await HandleCreateTask(command, userId, channelId);

                case CommandType.ListTasks:
                    return await HandleListTasks(command, userId, channelId);

                case CommandType.CompleteTask:
                    return await HandleCompleteTask(command, userId);

                case CommandType.DeleteTask:
                    return await HandleDeleteTask(command, userId);

                case CommandType.SetReminder:
                    return await HandleSetReminder(command, userId, channelId);

                case CommandType.Help:
                    return GetHelpMessage();

                default:
                    return "I didn't understand that command. Type 'help' to see what I can do.";
            }
        }

        private async Task<string> HandleCreateTask(TaskCommand command, string userId, string? channelId)
        {
            if (string.IsNullOrWhiteSpace(command.Title))
            {
                return "Please provide a task title. Example: create task \"Deploy to production\" due Friday";
            }

            var task = await _taskService.CreateTaskAsync(
                command.Title,
                command.Description,
                command.DueDate,
                userId,
                channelId);

            var response = new StringBuilder();
            response.AppendLine($"✅ Task created successfully!");
            response.AppendLine($"📋 Task #{task.Id}: {task.Title}");

            if (task.DueDate.HasValue)
            {
                response.AppendLine($"📅 Due: {task.DueDate.Value:MMM dd, yyyy 'at' h:mm tt}");
            }

            return response.ToString();
        }

        private async Task<string> HandleListTasks(TaskCommand command, string userId, string? channelId)
        {
            var tasks = await _taskService.GetTasksAsync(userId, command.Filter, channelId);

            if (!tasks.Any())
            {
                return command.Filter switch
                {
                    TaskFilter.Completed => "You don't have any completed tasks.",
                    TaskFilter.Pending => "You don't have any pending tasks.",
                    TaskFilter.Overdue => "You don't have any overdue tasks. Great job! 🎉",
                    TaskFilter.Today => "You don't have any tasks due today.",
                    TaskFilter.ThisWeek => "You don't have any tasks due this week.",
                    _ => "You don't have any tasks yet. Create one with: create task \"Task name\""
                };
            }

            var response = new StringBuilder();
            response.AppendLine($"📋 Your {GetFilterLabel(command.Filter)} Tasks:\n");

            foreach (var task in tasks)
            {
                var status = task.IsCompleted ? "✅" : "⏳";
                var overdue = !task.IsCompleted && task.DueDate.HasValue && task.DueDate.Value < DateTime.Now
                    ? " 🔴 OVERDUE"
                    : "";

                response.AppendLine($"{status} Task #{task.Id}: {task.Title}{overdue}");

                if (task.DueDate.HasValue)
                {
                    response.AppendLine($"   📅 Due: {task.DueDate.Value:MMM dd, h:mm tt}");
                }

                response.AppendLine();
            }

            return response.ToString();
        }

        private async Task<string> HandleCompleteTask(TaskCommand command, string userId)
        {
            if (!command.TaskId.HasValue)
            {
                return "Please specify a task ID. Example: complete task #123";
            }

            var success = await _taskService.CompleteTaskAsync(command.TaskId.Value, userId);

            if (!success)
            {
                return $"Could not find task #{command.TaskId.Value} or you don't have permission to complete it.";
            }

            return $"✅ Task #{command.TaskId.Value} marked as complete! Great job! 🎉";
        }

        private async Task<string> HandleDeleteTask(TaskCommand command, string userId)
        {
            if (!command.TaskId.HasValue)
            {
                return "Please specify a task ID. Example: delete task #123";
            }

            var success = await _taskService.DeleteTaskAsync(command.TaskId.Value, userId);

            if (!success)
            {
                return $"Could not find task #{command.TaskId.Value} or you don't have permission to delete it.";
            }

            return $"🗑️ Task #{command.TaskId.Value} has been deleted.";
        }

        private async Task<string> HandleSetReminder(TaskCommand command, string userId, string? channelId)
        {
            if (string.IsNullOrWhiteSpace(command.Title))
            {
                return "Please specify what to remind you about. Example: remind me about standup in 30 minutes";
            }

            var minutes = command.ReminderMinutes ?? 30;
            var reminder = await _taskService.CreateReminderAsync(command.Title, minutes, userId, channelId);

            return $"⏰ Reminder set! I'll remind you about \"{command.Title}\" in {minutes} minute(s).";
        }

        private string GetFilterLabel(TaskFilter filter)
        {
            return filter switch
            {
                TaskFilter.Completed => "Completed",
                TaskFilter.Pending => "Pending",
                TaskFilter.Overdue => "Overdue",
                TaskFilter.Today => "Today's",
                TaskFilter.ThisWeek => "This Week's",
                _ => "All"
            };
        }

        private string GetHelpMessage()
        {
            return @"🤖 Task Tracker Agent - Available Commands:

                📝 CREATE TASKS:
                • create task ""Task name"" due Friday
                • add task ""Deploy app"" due tomorrow 3pm
                • new task ""Review code"" due Nov 5

                📋 LIST TASKS:
                • show my tasks
                • list pending tasks
                • show completed tasks
                • show overdue tasks
                • show today's tasks

                ✅ COMPLETE TASKS:
                • complete task #123
                • finish task #456
                • done task #789

                🗑️ DELETE TASKS:
                • delete task #123
                • remove task #456

                ⏰ REMINDERS:
                • remind me about standup in 30 minutes
                • remind me about meeting in 2 hours

                Need help? Just ask! I'm here to help you stay organized. 🚀";
        }
    }
}
