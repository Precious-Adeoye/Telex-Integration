# Telex-Integration

Task Tracker Agent for Telex.im
A smart AI-powered task management agent built with ASP.NET Core 8.0 that integrates seamlessly with Telex.im. Help your team stay organized with natural language task creation, reminders, and tracking.
🚀 Features

- Natural Language Processing: Create tasks using everyday language
- Task Management: Create, list, complete, and delete tasks
- Smart Reminders: Set reminders with flexible time formats
- Filtering Options: View pending, completed, overdue, today's, or this week's tasks
- A2A Protocol: Full integration with Telex.im's Agent-to-Agent protocol
- Background Jobs: Automated reminder system using Hangfire.
- SQLite Database: Lightweight, file-based storage

 Tech Stack

- Framework: ASP.NET Core 8.0 Web API
- Database: Entity Framework Core with SQLite
- Background Jobs: Hangfire
- Natural Language: Custom regex-based parser
- Deployment: Railway (Docker)