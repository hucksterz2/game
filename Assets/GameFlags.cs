using System.Collections.Generic;

public static class GameFlags
{
    private static Dictionary<string, bool> flags = new Dictionary<string, bool>();

    public static void Set(string key, bool value)
    {
        if (string.IsNullOrEmpty(key)) return;
        flags[key] = value;
    }

    public static bool Get(string key)
    {
        if (string.IsNullOrEmpty(key)) return false;
        return flags.TryGetValue(key, out bool v) && v;
    }

    public static void Clear() => flags.Clear();
}