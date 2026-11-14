var builder = DistributedApplication.CreateBuilder(args);

var keyVault = builder.AddAzureKeyVault("ndbotkv");

var server = builder.AddProject<Projects.NotificationDiscordBot_Server>("notificationdiscordbot-server")
    .WithReference(keyVault);

builder.Build().Run();
