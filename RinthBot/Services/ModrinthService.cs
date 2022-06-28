﻿using System.ComponentModel;
using System.Timers;
using Discord.WebSocket;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Modrinth.RestClient;
using Modrinth.RestClient.Models;
using Timer = System.Timers.Timer;
using Version = Modrinth.RestClient.Models.Version;
using RinthBot.EmbedBuilders;
using RinthBot.Interfaces;

namespace RinthBot.Services;

public class ModrinthService // : IModrinthService
{
    private readonly BackgroundWorker _updateWorker;
    private readonly IModrinthApi _api;
    private readonly ILogger _logger;
    private readonly IMemoryCache _cache;
    private readonly MemoryCacheEntryOptions _cacheEntryOptions;
    private readonly DataService _dataService;
    private readonly DiscordSocketClient _client;
    
    public ModrinthService(IServiceProvider serviceProvider)
    {
        _api = ModrinthApi.NewClient();
        _logger = serviceProvider.GetRequiredService<ILogger<ModrinthService>>();
        _cache = serviceProvider.GetRequiredService<IMemoryCache>();
        _dataService = serviceProvider.GetRequiredService<DataService>();
        _client = serviceProvider.GetRequiredService<DiscordSocketClient>();
        
        _cacheEntryOptions = new MemoryCacheEntryOptions()
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
        };
        
        _updateWorker = new BackgroundWorker();
        _updateWorker.DoWork += CheckUpdate;
        
        var checkTimer = new Timer(MinutesToMilliseconds(120));
        checkTimer.Elapsed += checkTimer_Elapsed;
        checkTimer.Start();
        
        _logger.LogInformation("Modrinth service initialized");

    }
    private double MinutesToMilliseconds(int minutes)
    {
        return TimeSpan.FromMinutes(minutes).TotalMilliseconds;
    }

    async void CheckUpdate(object? sender, DoWorkEventArgs e)
    {
        _logger.LogInformation("Running update check");
        var projects = _dataService.GetAllProjects();

        foreach (var project in projects)
        {
            _logger.LogInformation("Checking project ID {Id}", project.Id);
            // Check if this project has even any update, if not, no need to go through all the guilds
            // TODO: What if Modrinth is down? or is rate limiting
            // This could be null
            var currentProject = await GetProject(project.Id);

            // This also could be null if rate limiting
            var newVersions = await CheckProjectForUpdates(project.Id);
            if (newVersions.Length == 0)
            {
                _logger.LogInformation("No new versions");
                continue;
            }
            
            _logger.LogInformation("{Count} new versions", newVersions.Length);

            var guilds = _dataService.GetAllGuildsSubscribedTo(project);
            foreach (var guild in guilds)
            {
                // We can have custom channel for this project
                var projectInfo = _dataService.GetProjectInfo(guild, currentProject);
                
                SocketTextChannel channel;
                
                // Is not custom
                if (projectInfo.CustomUpdateChannel == null)
                {
                    channel = _client.GetGuild(guild.Id).GetTextChannel((ulong)guild.UpdateChannel);
                    _logger.LogInformation("Sending update to guild {Id} and channel {Channel}", guild.Id, guild.UpdateChannel == null ? "NOT SET" : guild.UpdateChannel);
                }
                // Custom
                else
                {
                    channel = _client.GetGuild(guild.Id).GetTextChannel(projectInfo.CustomUpdateChannel);
                    _logger.LogInformation("Sending update to guild {Id} and channel {Channel}", guild.Id, channel == null ? "NOT SET" : channel.Id);
                }

                
                if (channel == null)
                {
                    _logger.LogInformation("Guild {Id} has not yet set a default update channel or custom channel for this project", guild.Id);
                    continue;
                }
                
                for (var i = newVersions.Length - 1; i >= 0; i--)
                {
                    var version = newVersions[i];
                    
                    var embed = ModrinthEmbedBuilder.VersionUpdateEmbed(currentProject, version);
                    try
                    {
                        await channel.SendMessageAsync(embed: embed.Build());
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("Error while sending message to guild {Guild}: {Exception}", guild.Id, ex.Message);
                    }


                }
            }
        }
        _logger.LogInformation("Update check ended");
    }

    /// <summary>
    /// Force check for updates, used for debugging
    /// </summary>
    /// <returns>False if worker is busy</returns>
    public bool ForceUpdate()
    {
        if (_updateWorker.IsBusy)
        {
            return false;
        }
        
        _updateWorker.RunWorkerAsync();

        return true;
    }
    

    private void checkTimer_Elapsed(object? sender, ElapsedEventArgs e)
    {
        if (!_updateWorker.IsBusy) _updateWorker.RunWorkerAsync();
    }

    /// <summary>
    /// Checks for update and updates the data in the database
    /// </summary>
    /// <param name="projectId"></param>
    /// <returns>List of new versions</returns>
    private async Task<Version[]> CheckProjectForUpdates(string projectId)
    {
        //BUG: Catch exceptions
        //TODO: Think of rate limiting
        var versions = await GetVersionListAsync(projectId);

        var project = _dataService.UpdateProjectVersionAndReturnOldOne(projectId, versions[0].Id);
        _logger.LogInformation("Last check version is {LastVersion}", project.LastCheckVersion);

        List<Version> newVersions = new();
        if (project == null)
            return newVersions.ToArray();
        
        foreach (var version in versions)
        {
            if (version.Id == project.LastCheckVersion)
            {
                return newVersions.ToArray();
            }
            
            newVersions.Add(version);
        }

        return newVersions.ToArray();
    }

    /// <summary>
    /// Gets Modrinth Project, uses caching
    /// </summary>
    /// <param name="slugOrId"></param>
    /// <returns></returns>
    public async Task<Project?> GetProject(string slugOrId)
    {
        if (_cache.TryGetValue(slugOrId, out object? value) && value is Project project)
        {
            _logger.LogDebug("{Id} retrieved from cache", slugOrId);
            return project;
        }

        _logger.LogDebug("{Id} not in cache", slugOrId);
        Project? p;
        try
        {
            p = await _api.GetProject(slugOrId);
        }
        catch (Exception)
        {
            return null;
        }
        
        _cache.Set(slugOrId, p, _cacheEntryOptions);

        return p;
    }

    public async Task<Version[]?> GetVersionListAsync(string slugOrId)
    {
        try
        {
            var searchResponse = await _api.GetProjectVersionList(slugOrId);
            return searchResponse;
        }
        catch (Exception e)
        {
            _logger.LogInformation("{ExceptionMessage}", e.Message);
        }

        return null;
    }

    public async Task<SearchResponse?> SearchProjects(string query)
    {
        try
        {
            var searchResponse = await _api.SearchProjects(query);
            return searchResponse;
        }
        catch (Exception e)
        {
            _logger.LogInformation("{ExceptionMessage}", e.Message);
        }

        return null;
    }

    public async Task<Project[]?> GetMultipleProjects(IEnumerable<string> projectIds)
    {
        try
        {
            var searchResponse = await _api.GetMultipleProjects(projectIds);
            return searchResponse;
        }
        catch (Exception e)
        {
            _logger.LogInformation("{ExceptionMessage}", e.Message);
        }

        return null;
    }

    public async Task<Version?> GetProjectsLatestVersion(string projectId)
    {
        var versions = await GetVersionListAsync(projectId);

        if (versions == null)
            return null;
        
        // Get last version ID
        var lastVersion = versions.OrderByDescending(x => x.DatePublished).First();

        return lastVersion;
    }
    public async Task<Version?> GetProjectsLatestVersion(Project project)
    {
        return await GetProjectsLatestVersion(project.Id);
    }
}