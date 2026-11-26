var builder = DistributedApplication.CreateBuilder(args);

var server = builder.AddProject<Projects.NotificationDiscordBot_Server>("notificationdiscordbot-server");

builder.Build().Run();
