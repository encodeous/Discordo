using System.Collections.Concurrent;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Discordo;
using DSharpPlus;
using DSharpPlus.Entities;
using Flurl.Http;
using org.matheval;

var client = new DiscordClient(new DiscordConfiguration()
{
    Token = File.ReadAllText(Path.Combine(new FileInfo(Process.GetCurrentProcess().MainModule.FileName).Directory.FullName, "token.txt")),
    Intents = DiscordIntents.All,
    TokenType = TokenType.Bot
});

var activityEngine = new ActivityEngine();
activityEngine.Initialize(client);

// make sure the database is created properly

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
        var data = BitConverter.ToInt32(RandomNumberGenerator.GetBytes(4));
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
        using var memStr = new MemoryStream(data);
        await omsg.RespondAsync(new DiscordMessageBuilder().AddFile("file1.png", memStr, true));
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
    else if (command == "stats")
    {
        var aid = reader.next();
        ulong uid = id;
        if (Regex.IsMatch(aid, "<@!?(\\d+)>"))
        {
            var matches = Regex.Match(aid, "<@!?(\\d+)>");
            uid = ulong.Parse(matches.Groups[1].Value);
        }
        await omsg.RespondAsync(activityEngine.GenerateStatsForUser(uid));
    }
    else if (command == "leaderboard")
    {
        await omsg.RespondAsync(activityEngine.GenerateLeaderboardEmbed(client));
    }
};

await client.ConnectAsync();

await Task.Delay(-1);