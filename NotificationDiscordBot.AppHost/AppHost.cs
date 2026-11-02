var builder = DistributedApplication.CreateBuilder(args);

var keyVault = builder.AddAzureKeyVault("notificationdiscordbot-kv");

var server = builder.AddProject<Projects.NotificationDiscordBot_Server>("notificationdiscordbot-server")
    .WithReference(keyVault);

builder.Build().Run();
