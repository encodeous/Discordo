using System.ComponentModel.DataAnnotations;

namespace Discordo.Data;

public class ActivityCount
{
    [Key] public string ActivityKey { get; set; }
    public ulong UserId { get; set; }
    public string ActivityName { get; set; }
    public uint Minutes { get; set; }
}