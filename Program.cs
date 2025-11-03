using Hangfire;
using Hangfire.SQLite;
using Microsoft.EntityFrameworkCore;
using Telex_Integration.Data;
using Telex_Integration.ITelextServices;
using Telex_Integration.TelexSevices;

namespace Telex_Integration
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();

            // Configure SQLite Database
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=tasks.db"));

            // Register Services
            builder.Services.AddScoped<ITaskService, TaskService>();
            builder.Services.AddScoped<INLPService, NLPService>();
            builder.Services.AddSingleton<ReminderService>();

            // Configure Hangfire with SQLite
            builder.Services.AddHangfire(configuration => configuration
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseSQLiteStorage(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=tasks.db"));

            builder.Services.AddHangfireServer();

            // Configure CORS for Telex.im
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowTelex", policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });

            var app = builder.Build();

            // Create database on startup
            using (var scope = app.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                dbContext.Database.EnsureCreated();
            }


            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.UseCors("AllowTelex");

            app.UseAuthorization();


            app.MapControllers();

            // Hangfire Dashboard (optional, for monitoring)
            app.UseHangfireDashboard("/hangfire");

            // Start reminder service
            var reminderService = app.Services.GetRequiredService<ReminderService>();
            RecurringJob.AddOrUpdate("check-reminders",
                () => reminderService.CheckAndSendReminders(),
                Cron.Minutely);


            var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";

            app.Run();
        }
    }
}
