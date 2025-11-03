using Telex_Integration.ITelextServices;

namespace Telex_Integration.TelexSevices
{
    public class ReminderService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ReminderService> _logger;

        public ReminderService(IServiceProvider serviceProvider, ILogger<ReminderService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task CheckAndSendReminders()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var taskService = scope.ServiceProvider.GetRequiredService<ITaskService>();

                var pendingReminders = await taskService.GetPendingRemindersAsync();

                foreach (var reminder in pendingReminders)
                {
                    try
                    {
                        // In a real implementation, you would send this back to Telex.im
                        // For now, we'll just log it and mark as sent
                        _logger.LogInformation(
                            $"Reminder: {reminder.Task?.Title} for user {reminder.UserId} in channel {reminder.ChannelId}");

                        // TODO: Implement actual Telex.im notification
                        // You could use webhooks or the Telex API to send messages back to users

                        await taskService.MarkReminderAsSentAsync(reminder.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error sending reminder {reminder.Id}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CheckAndSendReminders");
            }
        }
    }
}
