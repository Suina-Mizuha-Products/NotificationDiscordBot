using Discord.WebSocket;
using NotificationDiscordBot.Server.Interfaces.Services.Twitch;
using NotificationDiscordBot.Server.Services.Discord;
using NotificationDiscordBot.Server.Services.Twitch;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddSingleton<DiscordSocketClient>();
builder.Services.AddHostedService<DiscordBotService>();
builder.Services.AddScoped<ITwitchVerifySignatureService, TwitchVerifySignatureService>();

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
