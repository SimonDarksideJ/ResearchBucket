using System.Diagnostics;

namespace MonoGameHub.Core.Services;

public sealed class DotNetCli
{
    public async Task<int> RunAsync(
        IReadOnlyList<string> args,
        string? workingDirectory,
        IReadOnlyDictionary<string, string?>? environment,
        IProgress<string>? output,
        CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = workingDirectory ?? Environment.CurrentDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (var arg in args)
            startInfo.ArgumentList.Add(arg);

        if (environment is not null)
        {
            foreach (var (key, value) in environment)
            {
                if (value is null)
                    startInfo.Environment.Remove(key);
                else
                    startInfo.Environment[key] = value;
            }
        }

        using var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };

        var tcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

        process.OutputDataReceived += (_, e) => { if (e.Data is not null) output?.Report(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data is not null) output?.Report(e.Data); };
        process.Exited += (_, _) => tcs.TrySetResult(process.ExitCode);

        if (!process.Start())
            throw new InvalidOperationException("Failed to start dotnet process.");

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await using var _ = cancellationToken.Register(() =>
        {
            try
            {
                if (!process.HasExited)
                    process.Kill(entireProcessTree: true);
            }
            catch
            {
                // ignored
            }
        });

        return await tcs.Task.ConfigureAwait(false);
    }

    public Task<int> RunAsync(
        string commandLine,
        string? workingDirectory,
        IReadOnlyDictionary<string, string?>? environment,
        IProgress<string>? output,
        CancellationToken cancellationToken)
    {
        var args = SplitArgs(commandLine);
        return RunAsync(args, workingDirectory, environment, output, cancellationToken);
    }

    private static IReadOnlyList<string> SplitArgs(string commandLine)
    {
        // Minimal splitter: handles quoted segments.
        var result = new List<string>();
        var current = new List<char>();
        var inQuotes = false;

        for (var i = 0; i < commandLine.Length; i++)
        {
            var c = commandLine[i];

            if (c == '"')
            {
                inQuotes = !inQuotes;
                continue;
            }

            if (!inQuotes && char.IsWhiteSpace(c))
            {
                if (current.Count > 0)
                {
                    result.Add(new string(current.ToArray()));
                    current.Clear();
                }

                continue;
            }

            current.Add(c);
        }

        if (current.Count > 0)
            result.Add(new string(current.ToArray()));

        return result;
    }
}
