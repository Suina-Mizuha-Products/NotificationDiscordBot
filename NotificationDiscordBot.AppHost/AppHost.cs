var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.NotificationDiscordBot_Server>("notificationdiscordbot-server");

builder.Build().Run();
