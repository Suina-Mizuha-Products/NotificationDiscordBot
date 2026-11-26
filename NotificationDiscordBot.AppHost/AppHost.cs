var builder = DistributedApplication.CreateBuilder(args);

builder.AddDockerComposeEnvironment("env");

var server = builder.AddDotnetProject("bot-server", "../NotificationDiscordBot.Server/NotificationDiscordBot.Server.csproj")
    .PublishAsDockerComposeService((resource, service) =>
    {
        service.Name = "bot-server";
    });

builder.Build().Run();
