namespace Discordo.Data;

public class UserActivity
{
    public ulong Id { get; set; }
    public List<ActivityInfo> Activities { get; set; }
}