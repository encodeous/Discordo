namespace Discordo.Data;

public class Leaderboard
{
    public IEnumerable<(string, ActivityInfo)> Top10Users { get; set; }
    public IEnumerable<(string, TimeSpan)> Top10Applications { get; set; }
}