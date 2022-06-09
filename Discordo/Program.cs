using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Discordo;
using Discordo.Covid;
using Discordo.Data;
using DSharpPlus;
using DSharpPlus.Entities;
using Flurl.Http;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using org.matheval;

var client = new DiscordClient(new DiscordConfiguration()
{
    Token = File.ReadAllText(Path.Combine(new FileInfo(Process.GetCurrentProcess().MainModule.FileName).Directory.FullName, "token.txt")),
    Intents = DiscordIntents.All,
    TokenType = TokenType.Bot
});

var logger = client.Logger;

void ScanUserActivities()
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
                    dbAccess.Activities.Add(new ActivityRecord()
                    {
                        ActivityName = activity.Name,
                        UserId = member.Id
                    });
                }
            }
        }
    }
    dbAccess.SaveChanges();
}

Leaderboard GenerateLeaderboard()
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

UserActivity GetUserActivity(ulong id)
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
        var samples = from x in activity
            orderby x.TimeAdded descending
            select x;
        var prevTime = DateTime.UtcNow;
        int sampleCount = 0;
        int totalSampleCount = 0;
        foreach (var cur in samples)
        {
            var newTime = cur.TimeAdded;
            if (prevTime - newTime <= TimeSpan.FromMinutes(2))
            {
                sampleCount++;
                prevTime = DateTime.MaxValue;
            }

            totalSampleCount++;
        }
        userActivity.Activities.Add(new ActivityInfo()
        {
            ActivityName = name,
            ConsecutiveTimeUse = TimeSpan.FromMinutes(sampleCount),
            TotalTimeUse = TimeSpan.FromMinutes(totalSampleCount)
        });
    }
    
    userActivity.Activities.Sort((o1, o2) =>
        -o1.TotalTimeUse.CompareTo(o2.TotalTimeUse));

    return userActivity;
}

Task.Run(async () =>
{
    while (true)
    {
        ScanUserActivities();
        await Task.Delay(TimeSpan.FromMinutes(1));
    }
});

// make sure the database is created properly

var db = new Database();
if (db.Database.GetPendingMigrations().Any())
{
    logger.LogInformation("Migrating database...");
    db.Database.Migrate();
    db.SaveChanges();
}
db.Dispose();

// CovidStat covidData = null;
//
// if (File.Exists("covid.json"))
// {
//     covidData = JsonSerializer.Deserialize<CovidStat>(File.ReadAllText("covid.json"));
// }
// else
// {
//     covidData = await "https://api.opencovid.ca/timeseries".GetJsonAsync<CovidStat>();
//     File.WriteAllText("covid.json", JsonSerializer.Serialize(covidData));
// }

var cooldown = new ConcurrentDictionary<ulong, DateTime>();

client.MessageCreated += async (_, msg) =>
{
    if (msg.Author.IsBot) return;
    var id = msg.Author.Id;
    if (cooldown.ContainsKey(id))
    {
        if (DateTime.Now - cooldown[id] <= TimeSpan.FromSeconds(5)) return;
    }
    
    cooldown[id] = DateTime.Now;

    var omsg = msg.Message;
    var text = msg.Message.Content;
    if (!text.StartsWith("oi")) return;
    text = text[2..].Trim() + "\n";

    var reader = new Scanner(text);
    
    // command
    var command = reader.next().ToLower();

    if (command == "cf")
    {
        var data = BitConverter.ToInt32(RNGCryptoServiceProvider.GetBytes(4));
        if (data % 2 == 0)
        {
            await omsg.RespondAsync("Heads");
        }
        else
        {
            await omsg.RespondAsync("Tails");
        }
    }
    else if (command == "xkcd")
    {
        var data = await "https://ec-xkcd.azurewebsites.net/api/xkcddark".GetBytesAsync();
        var lst = new Dictionary<string, Stream>();
        var memStream = new MemoryStream(data);
        lst["file1.png"] = memStream;
        await omsg.RespondAsync(new DiscordMessageBuilder().WithFiles(lst));
    }
    else if (command == "calc")
    {
        var expr = new Expression(reader.nextLine());
        try
        {
            await omsg.RespondAsync($"Result: `{expr.Eval()}`");
        }
        catch (Exception e)
        {
            await omsg.RespondAsync($"Error while evaluating expression: `{e.Message}`");
        }
    }
    // else if (command == "covid")
    // {
    //     var province = reader.next();
    //
    //     var result = from x in covidData.cases
    //         where String.Equals(x.province, province, StringComparison.CurrentCultureIgnoreCase)
    //         orderby x.GetRealDate() descending 
    //         select x;
    //
    //     var today = result.First();
    //     
    //     await omsg.RespondAsync(new DiscordEmbedBuilder()
    //         .WithTitle("Covid Stats Today")
    //         .WithDescription($"Cases Today: `{today.cases}`\nTotal Cases: `{today.cumulative_cases}`")
    //         .WithFooter($"Showing COVID stats for `{province}`")
    //         .WithColor(DiscordColor.Blurple));
    // }
    // else if (command == "update")
    // {
    //     ScanUserActivities();
    // }
    else if (command == "stats")
    {
        var aid = reader.next();
        ulong uid = id;
        if (Regex.IsMatch(aid, "<@!?(\\d+)>"))
        {
            var matches = Regex.Match(aid, "<@!?(\\d+)>");
            uid = ulong.Parse(matches.Groups[1].Value);
        }
        
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
        await omsg.RespondAsync(eb);
    }
    else if (command == "leaderboard")
    {
        var topUsers = new StringBuilder();
        var topApps = new StringBuilder();
        var lb = GenerateLeaderboard();
        
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
        await omsg.RespondAsync(eb);
    }
};

await client.ConnectAsync();

await Task.Delay(-1);