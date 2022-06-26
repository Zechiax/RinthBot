﻿using System.Text;
using Discord;
using Humanizer;
using Humanizer.Bytes;
using Modrinth.RestClient.Models;
using Modrinth.RestClient.Models.Enums;
using RinthBot.Extensions;
using StringExtensions = RinthBot.Utilities.StringExtensions;
using Version = Modrinth.RestClient.Models.Version;

namespace RinthBot.EmbedBuilders;

public static class ModrinthEmbedBuilder
{
    private static readonly Color ModrinthColor = new Color(27, 217, 106);
    private static string GetProjectUrl(Project project)
    {
        return $"https://modrinth.com/{(project.ProjectType == ProjectType.Mod ? "mod" : "modpack")}/{project.Id}";
    }

    public static EmbedBuilder GetProjectEmbed(Project project)
    {
        var embed = new EmbedBuilder()
        {
            Title = project.Title,
            Url = GetProjectUrl(project),
            Description = project.Description,
            ThumbnailUrl = project.IconUrl,
            Color = ModrinthColor,
            Fields = new List<EmbedFieldBuilder>
            {
                new() { Name = "Downloads", Value = project.Downloads.SeparateThousands(), IsInline = true },
                new() { Name = "Followers", Value = project.Followers.SeparateThousands(), IsInline = true },
                new() { Name = "Categories", Value = string.Join(", ", project.Categories), IsInline = true },
                new() { Name = "Type", Value = project.ProjectType.ToString(), IsInline = true },
                new() { Name = "Id", Value = project.Id, IsInline = true },
                new() { Name = "Created | Last updated", Value = $"{TimestampTag.FromDateTime(project.Published, TimestampTagStyles.Relative)} | {TimestampTag.FromDateTime(project.Updated, TimestampTagStyles.Relative)}"  }
            }
        };

        return embed;
    }

    public static EmbedBuilder VersionUpdateEmbed(Project project, Version version)
    {
        var sbFiles = new StringBuilder();

        foreach (var file in version.Files)
        {
            sbFiles.AppendLine($"[{file.FileName}]({file.Url}) | {ByteSize.FromBytes(file.Size).Humanize()}");
        }

        var embed = new EmbedBuilder
        {
            Author = new EmbedAuthorBuilder()
            {
                Name = $"Modrinth | {project.ProjectType.ToString()}",
                // TODO: Get icon from elsewhere
                IconUrl = "https://avatars.githubusercontent.com/u/67560307"
            },
            Footer = new EmbedFooterBuilder()
            {
                Text = "Published"
            },
            Title = $"{project.Title} | New Version Found",
            Description = $"Version **{version.VersionNumber}** has been uploaded to Modrinth" +
                          $"\n\n**Changelog**" +
                          $"\n---------------" +
                          StringExtensions.Truncate($"\n{version.Changelog}", 3000),
            Url = GetProjectUrl(project),
            ThumbnailUrl = project.IconUrl,
            ImageUrl = null,
            Fields = new List<EmbedFieldBuilder>()
            {
                new()
                {
                    Name = "Type",
                    Value = version.VersionType.ToString(),
                    IsInline = true
                },
                new()
                {
                    Name = "MC Version",
                    Value = string.Join(", ",version.GameVersions),
                    IsInline = true
                },
                new()
                {
                    Name = "Loaders",
                    Value = string.Join(", ", version.Loaders),
                    IsInline = true
                },
                new()
                {
                    Name = $"File{(version.Files.Length > 1 ? "s" : null)}",
                    Value = sbFiles.ToString(),
                },
                new()
                {
                    Name = "Links",
                    Value = string.Join(" | ", 
                        $"[Changelog]({GetProjectUrl(project)}/changelog)", 
                        $"[Version Info]({GetProjectUrl(project)}/version/{version.Id})")
                }
            },
            Timestamp = version.DatePublished,
            Color = GetColorByProjectVersionType(version.VersionType)
        };

        return embed;
    }
    
    private static Color GetColorByProjectVersionType(VersionType type)
    {
        return type switch
        {
            VersionType.Alpha => new Color(219, 49, 98),
            VersionType.Beta => new Color(247, 187, 67),
            VersionType.Release => new Color(27, 217, 106),
            _ => Color.Default
        };
    }
}