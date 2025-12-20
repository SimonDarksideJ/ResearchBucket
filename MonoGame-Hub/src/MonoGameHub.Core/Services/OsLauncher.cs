using System.Diagnostics;
using System.IO;

namespace MonoGameHub.Core.Services;

public sealed class OsLauncher
{
    public void OpenUrl(string url)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = url,
            UseShellExecute = true
        });
    }

    public void OpenFolder(string folder)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = folder,
            UseShellExecute = true
        });
    }

    public void OpenInVsCode(string folder)
    {
        TryStart("code", $"\"{folder}\"");
    }

    public void OpenInRider(string folder)
    {
        // Rider provides a `rider` launcher when enabled in settings.
        TryStart("rider", $"\"{folder}\"");
    }

    private static void TryStart(string fileName, string arguments)
    {
        // On Windows, `code` / `rider` are commonly .cmd shims. Launching those via shell execute can
        // leave a visible console window behind. Prefer a hidden `cmd.exe /c ...` invocation.
        if (OperatingSystem.IsWindows())
        {
            var isDirectExecutable = Path.IsPathRooted(fileName)
                || fileName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase);

            var startInfo = isDirectExecutable
                ? new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                }
                : new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c {fileName} {arguments}",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

            Process.Start(startInfo);
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = true
        });
    }
}
