using System.Text;
using Discordo.Data;
using DSharpPlus;
using DSharpPlus.Entities;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Discordo;

public class ActivityEngine
{
    
    public Leaderboard GenerateLeaderboard(DiscordClient client)
    {
        List<(string, ActivityInfo)> topUsers = new();
        Dictionary<string, TimeSpan> applications = new();
        foreach (var guild in client.Guilds.Values)
        {
            foreach (var member in guild.Members.Values)
            {
                var userActivity = GetUserActivity(member.Id);
                if (userActivity.Activities.Count == 0) continue;
                topUsers.Add((member.Username, userActivity.Activities[0]));
                foreach (var activity in userActivity.Activities)
                {
                    if(!applications.ContainsKey(activity.ActivityName))
                        applications[activity.ActivityName] = TimeSpan.Zero;
                    applications[activity.ActivityName] += activity.TotalTimeUse;
                }
            }
        }
        topUsers.Sort((o1, o2) =>
            -o1.Item2.TotalTimeUse.CompareTo(o2.Item2.TotalTimeUse));
        var lb = new Leaderboard();
        lb.Top10Users = topUsers.Take(15);
        lb.Top10Applications = (from x in applications
            orderby x.Value descending
            select (x.Key, x.Value)).Take(15);
        return lb;
    }
    
    public UserActivity GetUserActivity(ulong id)
    {
        var dbAccess = new Database();
        var userEntries = (from x in dbAccess.Activities
            where x.UserId == id
            select x).ToList();
        var userActivities = from x in userEntries
            group x by x.ActivityName
            into activityGroup
            select activityGroup;
        var userActivity = new UserActivity();
        userActivity.Activities = new List<ActivityInfo>();
        userActivity.Id = id;
        foreach (var activity in userActivities)
        {
            string name = activity.Key;
            userActivity.Activities.Add(new ActivityInfo()
            {
                ActivityName = name,
                TotalTimeUse = TimeSpan.FromMinutes(activity.First().Minutes)
            });
        }
    
        userActivity.Activities.Sort((o1, o2) =>
            -o1.TotalTimeUse.CompareTo(o2.TotalTimeUse));

        return userActivity;
    }

    public DiscordEmbed GenerateLeaderboardEmbed(DiscordClient client)
    {
        var topUsers = new StringBuilder();
        var topApps = new StringBuilder();
        var lb = GenerateLeaderboard(client);
        
        {
            topUsers.AppendLine("```");
            var title = $"{"Time Used".PadRight(15)} | {"User".PadRight(15)} | {"Activity".PadRight(15)}";
            topUsers.AppendLine(title);
            topUsers.AppendLine($"{string.Concat(Enumerable.Repeat('-', title.Length))}");
            foreach (var activity in lb.Top10Users)
            {
                topUsers.AppendLine(
                    $"{TimeSpanHumanizeExtensions.Humanize(activity.Item2.TotalTimeUse).PadRight(15)} | {activity.Item1.Truncate(15).PadRight(15)} | {activity.Item2.ActivityName.Truncate(15).PadRight(15)}");
            }
            topUsers.AppendLine("```");
        }
        
        {
            topApps.AppendLine("```");
            var title = $"{"Time Used".PadRight(15)} | {"Activity".PadRight(35)}";
            topApps.AppendLine(title);
            topApps.AppendLine($"{string.Concat(Enumerable.Repeat('-', title.Length))}");
            foreach (var activity in lb.Top10Applications)
            {
                topApps.AppendLine(
                    $"{TimeSpanHumanizeExtensions.Humanize(activity.Item2).PadRight(15)} | {activity.Item1.Truncate(35).PadRight(35)}");
            }
            topApps.AppendLine("```");
        }

        var eb = new DiscordEmbedBuilder()
            .WithTitle($"Showing global activity leaderboard")
            .AddField("Top 15 Activities (Cumulative)", topApps.ToString())
            .AddField("Top 15 Users", topUsers.ToString())
            .Build();
        return eb;
    }

    public DiscordEmbed GenerateStatsForUser(ulong uid)
    {
        var res = GetUserActivity(uid);

        var sb = new StringBuilder();
        sb.AppendLine("```");
        var title = $"{"Activity Name".PadRight(35)} | {"Time Used".PadRight(15)}";
        sb.AppendLine(title);
        sb.AppendLine($"{string.Concat(Enumerable.Repeat('-', title.Length))}");
        foreach (var activity in res.Activities)
        {
            sb.AppendLine(
                $"{activity.ActivityName.Truncate(35).PadRight(35)} | {activity.TotalTimeUse.Humanize().PadRight(15)}");
        }
        sb.AppendLine("```");

        var eb = new DiscordEmbedBuilder()
            .WithTitle($"Showing Activity for `{uid}`")
            .WithDescription(sb.ToString()).Build();

        return eb;
    }

    public void Initialize(DiscordClient client)
    {
        var logger = client.Logger;
        var db = new Database();
        if (db.Database.GetPendingMigrations().Any())
        {
            logger.LogInformation("Migrating database...");
            db.Database.Migrate();
            db.SaveChanges();
        }
        db.Dispose();
        Task.Run(async () =>
        {
            while (true)
            {
                ScanUserActivities(client);
                await Task.Delay(TimeSpan.FromMinutes(1));
            }
        });
    }
    
    private void ScanUserActivities(DiscordClient client)
    {
        var dbAccess = new Database();
        foreach (var guild in client.Guilds.Values)
        {
            foreach (var member in guild.Members.Values)
            {
                if (member.Presence != null && member.Presence.Activities.Any() && !member.IsBot)
                {
                    foreach (var activity in member.Presence.Activities)
                    {
                        // the user has an activity
                        if(activity.Name == "Custom Status") continue;
                        var curActivity = dbAccess.Activities.Find(Utils.GetActivityKey(member.Id, activity.Name));
                        if (curActivity != null)
                        {
                            curActivity.Minutes++;
                            dbAccess.Activities.Update(curActivity);
                        }
                        else
                        {
                            dbAccess.Activities.Add(new ActivityCount()
                            {
                                ActivityName = activity.Name,
                                UserId = member.Id,
                                Minutes = 1,
                                ActivityKey = Utils.GetActivityKey(member.Id, activity.Name)
                            });
                        }
                    }
                }
            }
        }
        dbAccess.SaveChanges();
    }
}