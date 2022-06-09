using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Discordo.Data;

public class ActivityRecord
{
    [Key] public long TimeKey { get; set; } = DateTime.UtcNow.ToFileTimeUtc();
    public DateTime TimeAdded => DateTime.FromFileTimeUtc(TimeKey);
    public ulong UserId { get; set; }
    public string ActivityName { get; set; }
}