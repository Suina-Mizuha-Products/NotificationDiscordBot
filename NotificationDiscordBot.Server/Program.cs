using Discord;
using Discord.WebSocket;
using NotificationDiscordBot.Server.Interfaces.Services;
using NotificationDiscordBot.Server.Interfaces.Services.Discord;
using NotificationDiscordBot.Server.Interfaces.Services.Twitch;
using NotificationDiscordBot.Server.Services;
using NotificationDiscordBot.Server.Services.Discord;
using NotificationDiscordBot.Server.Services.Twitch;
using IDiscordClient = NotificationDiscordBot.Server.Interfaces.Services.Discord.IDiscordClient;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddHttpClient();

builder.Services.AddHostedService<QueueHostedService>();
builder.Services.AddSingleton<IBackgroundTaskQueue, DefaultBackgroundQueueService>(_ =>
{
    if (!int.TryParse(builder.Configuration["BackgroundTaskQueueCapacity"], out var capacity))
    {
        capacity = 100;
    }
    return new DefaultBackgroundQueueService(capacity);
});

builder.Services.AddSingleton<DiscordSocketClient>(service =>
{
    var config = new DiscordSocketConfig
    {
        MessageCacheSize = 100,
        LogLevel = LogSeverity.Info,
        GatewayIntents = GatewayIntents.Guilds
    };
    return new DiscordSocketClient(config);
});
builder.Services.AddSingleton<IDiscordClient>(sp =>
{
    var socketClient = sp.GetRequiredService<DiscordSocketClient>();
    return new DiscordClientWrapper(socketClient);
});
builder.Services.AddHostedService<DiscordBotService>();
builder.Services.AddScoped<ITwitchVerifySignatureService, TwitchVerifySignatureService>();
builder.Services.AddScoped<ITwitchApiInvocationService, TwitchApiInvocationService>();
builder.Services.AddScoped<ITwitchEventProcessingService, TwitchEventProcessingService>();
builder.Services.AddScoped<ISendNotificationService, SendNotificationService>();

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
