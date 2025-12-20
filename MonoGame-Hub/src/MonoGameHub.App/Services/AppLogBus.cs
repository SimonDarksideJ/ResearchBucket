using System.Collections.Generic;

namespace MonoGameHub.App.Services;

public static class AppLogBus
{
    private static readonly object Gate = new();
    private static readonly List<string> HistoryInternal = new();

    public static event Action<string>? Line;

    public static IReadOnlyList<string> GetHistorySnapshot()
    {
        lock (Gate)
            return HistoryInternal.ToArray();
    }

    public static void Publish(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return;

        lock (Gate)
        {
            if (HistoryInternal.Count > 5000)
                HistoryInternal.RemoveAt(0);

            HistoryInternal.Add(line);
        }

        Line?.Invoke(line);
    }
}
