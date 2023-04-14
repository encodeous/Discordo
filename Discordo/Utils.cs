namespace Discordo;

public static class Utils
{
    public static string GetActivityKey(ulong uid, String activity)
    {
        return uid + activity;
    }
}