var builder = DistributedApplication.CreateBuilder(args);

var server = builder.AddDotnetProject("bot-server", "../NotificationDiscordBot.Server/NotificationDiscordBot.Server.csproj");

builder.Build().Run();
