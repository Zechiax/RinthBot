using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace RinthBot.Services;

public class LoggingService
{
    // declare the fields used later in this class
    private readonly ILogger _logger;
    private readonly DiscordSocketClient _discord;

    public LoggingService(IServiceProvider services)
    {
        // get the services we need via DI, and assign the fields declared above to them
        _discord = services.GetRequiredService<DiscordSocketClient>();
        var commands = services.GetRequiredService<CommandService>();
        _logger = services.GetRequiredService<ILogger<LoggingService>>();

        // hook into these events with the methods provided below
        _discord.Ready += OnReadyAsync;
        _discord.JoinedGuild += GuildJoin;
        _discord.LeftGuild += GuildLeft;

        _discord.Log += OnLogAsync;
        commands.Log += OnLogAsync;

    }

    // this method executes on the bot being connected/ready
    private Task OnReadyAsync()
    {
        _logger.LogInformation("Connected as -> [{CurrentUser}] :)", _discord.CurrentUser.Username);
        _logger.LogInformation("We are on [{GuildsCount}] servers", _discord.Guilds.Count);
        return Task.CompletedTask;
    }

    private Task GuildJoin(SocketGuild guild)
    {
        _logger.LogInformation("Joined guild {GuildId}:{GuildName}", guild.Id, guild.Name);
        return Task.CompletedTask;
    }

    private Task GuildLeft(SocketGuild guild)
    {
        _logger.LogInformation("Left guild {GuildId}:{GuildName}", guild.Id, guild.Name);
        return Task.CompletedTask;
    }

    // this method switches out the severity level from Discord.Net's API, and logs appropriately
    private Task OnLogAsync(LogMessage msg)
    {
        var logText = $"{msg.Source}: {msg.Exception?.ToString() ?? msg.Message}";
        switch (msg.Severity.ToString())
        {
            case "Critical":
                {
                    _logger.LogCritical("{LogText}", logText);
                    break;
                }
            case "Warning":
                {
                    _logger.LogWarning("{LogText}", logText);
                    break;
                }
            case "Info":
                {
                    _logger.LogInformation("{LogText}", logText);
                    break;
                }
            case "Verbose":
                {
                    _logger.LogInformation("{LogText}", logText);
                    break;
                }
            case "Debug":
                {
                    _logger.LogDebug("{LogText}", logText);
                    break;
                }
            case "Error":
                {
                    _logger.LogError("{LogText}", logText);
                    break;
                }
        }

        return Task.CompletedTask;

    }
}