
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text.Json;
using Discordo;
using DSharpPlus;
using DSharpPlus.Entities;
using Flurl.Http;
using org.matheval;

var client = new DiscordClient(new DiscordConfiguration()
{
    Token = File.ReadAllText(@"D:\encodeous\Discordo\Discordo\bin\Debug\net6.0\token.txt"),
    Intents = DiscordIntents.All,
    TokenType = TokenType.Bot
});

CovidStat covidData = null;

if (File.Exists("covid.json"))
{
    covidData = JsonSerializer.Deserialize<CovidStat>(File.ReadAllText("covid.json"));
}
else
{
    covidData = await "https://api.opencovid.ca/timeseries".GetJsonAsync<CovidStat>();
    File.WriteAllText("covid.json", JsonSerializer.Serialize(covidData));
}

var cooldown = new ConcurrentDictionary<ulong, DateTime>();

client.MessageCreated += async (_, msg) =>
{
    if (msg.Author.IsBot) return;
    var id = msg.Author.Id;
    if (cooldown.ContainsKey(id))
    {
        // if (DateTime.Now - cooldown[id] <= TimeSpan.FromSeconds(5)) return;
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
    else if (command == "covid")
    {
        var province = reader.next();

        var result = from x in covidData.cases
            where x.province.ToLower() == province.ToLower()
            orderby x.GetRealDate() descending 
            select x;

        var today = result.First();
        
        await omsg.RespondAsync(new DiscordEmbedBuilder()
            .WithTitle("Covid Stats Today")
            .WithDescription($"Cases Today: `{today.cases}`\nTotal Cases: `{today.cumulative_cases}`")
            .WithFooter($"Showing COVID stats for `{province}`")
            .WithColor(DiscordColor.Blurple));
    }

    Console.WriteLine(msg.Author.Username + " " + msg.Message.Content);
    // await msg.Message.RespondAsync(msg.Message.Content);
};

await client.ConnectAsync();

await Task.Delay(-1);


public class Case
{
    public string province { get; set; }
    public string date_report { get; set; }
    public DateOnly GetRealDate(){
        var spl = date_report.Split("-");
        return new DateOnly(int.Parse(spl[2]), int.Parse(spl[1]), int.Parse(spl[0]));
    }
    public int cases { get; set; }
    public int cumulative_cases { get; set; }
}

public class Mortality
{
    public string province { get; set; }
    public string date_death_report { get; set; }
    public int deaths { get; set; }
    public int cumulative_deaths { get; set; }
}

public class Recovered
{
    public string province { get; set; }
    public string date_recovered { get; set; }
    public int recovered { get; set; }
    public int cumulative_recovered { get; set; }
}

public class Testing
{
    public string province { get; set; }
    public string date_testing { get; set; }
    public int testing { get; set; }
    public int cumulative_testing { get; set; }
    public string testing_info { get; set; }
}

public class Active
{
    public string province { get; set; }
    public string date_active { get; set; }
    public int cumulative_cases { get; set; }
    public int cumulative_recovered { get; set; }
    public int cumulative_deaths { get; set; }
    public int active_cases { get; set; }
    public int active_cases_change { get; set; }
}

public class CovidStat
{
    public List<Case> cases { get; set; }
    public List<Mortality> mortality { get; set; }
    public List<Recovered> recovered { get; set; }
    public List<Testing> testing { get; set; }
    public List<Active> active { get; set; }
    public List<object> avaccine { get; set; }
    public List<object> dvaccine { get; set; }
    public List<object> cvaccine { get; set; }
    public string deprecation_warning { get; set; }
}