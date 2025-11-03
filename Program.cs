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
            builder.Services.AddOpenApi();

            // === SQLite Database (EF Core) ===
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=tasks.db";
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite(connectionString));

            // === Register Services ===
            builder.Services.AddScoped<ITaskService, TaskService>();
            builder.Services.AddScoped<INLPService, NLPService>();
            builder.Services.AddSingleton<ReminderService>();

            // === Hangfire with SQLite (FIXED) ===
            builder.Services.AddHangfire(configuration => configuration
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseStorage(new SQLiteStorage(connectionString)) // ← CRITICAL FIX
            );

            builder.Services.AddHangfireServer();

            // === CORS ===
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

            // === Ensure DB is created ===
            using (var scope = app.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                dbContext.Database.EnsureCreated();
            }

            // === Middleware ===
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.UseCors("AllowTelex");
            app.UseAuthorization();
            app.MapControllers();

            // === Hangfire Dashboard ===
            app.UseHangfireDashboard("/hangfire");

            // === Recurring Job ===
            RecurringJob.AddOrUpdate<ReminderService>(
                "check-reminders",
                service => service.CheckAndSendReminders(),
                Cron.Minutely);

            // === Run on correct port (Docker, Cloud Run, etc.) ===
            var port = Environment.GetEnvironmentVariable("PORT") ?? "8080"; // Cloud Run uses 8080
            app.Urls.Add($"http://0.0.0.0:{port}");

            app.Run();
        }
    }
}