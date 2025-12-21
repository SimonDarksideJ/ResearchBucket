using System.Text.Json;
using System.Xml.Linq;
using NuGet.Versioning;

namespace MonoGameHub.Core.Services;

public sealed class MonoGameToolingVersionSync
{
    public bool TrySyncMonoGameVersionsToDotNetTools(string projectFolder, Action<string>? log = null)
    {
        log ??= _ => { };

        if (string.IsNullOrWhiteSpace(projectFolder) || !Directory.Exists(projectFolder))
            return false;

        var toolVersion = TryGetMonoGameDotNetToolsVersion(projectFolder);
        if (string.IsNullOrWhiteSpace(toolVersion))
            return false;

        var csprojPaths = Directory
            .EnumerateFiles(projectFolder, "*.csproj", SearchOption.AllDirectories)
            .ToList();

        var anyChanges = false;

        foreach (var csprojPath in csprojPaths)
        {
            try
            {
                anyChanges |= TryUpdateProjectFile(csprojPath, toolVersion, log);
            }
            catch (Exception ex)
            {
                log($"Version sync skipped for {Path.GetFileName(csprojPath)}: {ex.Message}");
            }
        }

        return anyChanges;
    }

    private static string? TryGetMonoGameDotNetToolsVersion(string projectFolder)
    {
        var toolsPath = Path.Combine(projectFolder, ".config", "dotnet-tools.json");
        if (!File.Exists(toolsPath))
            return null;

        try
        {
            using var stream = File.OpenRead(toolsPath);
            using var doc = JsonDocument.Parse(stream);

            if (!doc.RootElement.TryGetProperty("tools", out var toolsElement) || toolsElement.ValueKind != JsonValueKind.Object)
                return null;

            // Prefer MonoGame tooling.
            if (TryGetToolVersion(toolsElement, "dotnet-mgcb", out var mgcbVersion))
                return mgcbVersion;

            if (TryGetToolVersion(toolsElement, "dotnet-mgcb-editor", out var mgcbEditorVersion))
                return mgcbEditorVersion;

            // Fallback: first tool version.
            foreach (var tool in toolsElement.EnumerateObject())
            {
                if (tool.Value.ValueKind != JsonValueKind.Object)
                    continue;

                if (tool.Value.TryGetProperty("version", out var v) && v.ValueKind == JsonValueKind.String)
                    return v.GetString();
            }
        }
        catch
        {
            // Best-effort only.
        }

        return null;
    }

    private static bool TryGetToolVersion(JsonElement toolsElement, string toolId, out string? version)
    {
        version = null;

        if (!toolsElement.TryGetProperty(toolId, out var tool) || tool.ValueKind != JsonValueKind.Object)
            return false;

        if (tool.TryGetProperty("version", out var v) && v.ValueKind == JsonValueKind.String)
        {
            version = v.GetString();
            return !string.IsNullOrWhiteSpace(version);
        }

        return false;
    }

    private static bool TryUpdateProjectFile(string csprojPath, string desiredVersion, Action<string> log)
    {
        if (string.IsNullOrWhiteSpace(csprojPath) || !File.Exists(csprojPath))
            return false;

        var doc = XDocument.Load(csprojPath, LoadOptions.PreserveWhitespace);

        var packageRefs = doc
            .Descendants()
            .Where(e => e.Name.LocalName.Equals("PackageReference", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var changed = false;

        foreach (var pr in packageRefs)
        {
            var include = pr.Attribute("Include")?.Value;
            if (string.IsNullOrWhiteSpace(include) || !IsOfficialMonoGamePackage(include))
                continue;

            var versionAttr = pr.Attribute("Version");
            var versionElement = pr.Elements().FirstOrDefault(e => e.Name.LocalName.Equals("Version", StringComparison.OrdinalIgnoreCase));

            var current = versionAttr?.Value ?? versionElement?.Value;
            if (string.IsNullOrWhiteSpace(current))
                continue;

            // Handle property-based versioning: $(MonoGameVersion)
            if (TryParseMsbuildPropertyReference(current, out var propertyName))
            {
                if (TryUpdateMsbuildProperty(doc, propertyName, desiredVersion, out var updatedFrom))
                {
                    if (!VersionsMatch(updatedFrom, desiredVersion))
                    {
                        log($"Updated {Path.GetFileName(csprojPath)}: {propertyName} {updatedFrom} -> {desiredVersion}");
                        changed = true;
                    }

                    continue;
                }

                // Fallback: convert to a literal version on this PackageReference.
                if (versionAttr is not null)
                    versionAttr.Value = desiredVersion;
                else if (versionElement is not null)
                    versionElement.Value = desiredVersion;

                log($"Updated {Path.GetFileName(csprojPath)}: {include} {current} -> {desiredVersion}");
                changed = true;
                continue;
            }

            if (VersionsMatch(current, desiredVersion))
                continue;

            if (versionAttr is not null)
                versionAttr.Value = desiredVersion;
            else if (versionElement is not null)
                versionElement.Value = desiredVersion;
            else
                continue;

            log($"Updated {Path.GetFileName(csprojPath)}: {include} {current} -> {desiredVersion}");
            changed = true;
        }

        if (!changed)
            return false;

        File.WriteAllText(csprojPath, doc.ToString(SaveOptions.DisableFormatting));
        return true;
    }

    private static bool IsOfficialMonoGamePackage(string include)
    {
        // Keep this conservative: only official MonoGame packages/tooling.
        return include.StartsWith("MonoGame.Framework", StringComparison.OrdinalIgnoreCase)
               || include.Equals("MonoGame.Framework", StringComparison.OrdinalIgnoreCase)
               || include.Equals("MonoGame.Content.Builder.Task", StringComparison.OrdinalIgnoreCase)
               || include.Equals("MonoGame.Content.Reference", StringComparison.OrdinalIgnoreCase)
               || include.StartsWith("MonoGame.Pipeline", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryParseMsbuildPropertyReference(string value, out string propertyName)
    {
        propertyName = string.Empty;

        var trimmed = value.Trim();
        if (!trimmed.StartsWith("$(", StringComparison.Ordinal) || !trimmed.EndsWith(")", StringComparison.Ordinal))
            return false;

        var name = trimmed[2..^1].Trim();
        if (string.IsNullOrWhiteSpace(name))
            return false;

        propertyName = name;
        return true;
    }

    private static bool TryUpdateMsbuildProperty(XDocument doc, string propertyName, string desiredVersion, out string previousValue)
    {
        previousValue = string.Empty;

        var propertyElement = doc
            .Descendants()
            .FirstOrDefault(e => e.Name.LocalName.Equals(propertyName, StringComparison.OrdinalIgnoreCase));

        if (propertyElement is null)
            return false;

        previousValue = propertyElement.Value;
        if (VersionsMatch(previousValue, desiredVersion))
            return true;

        propertyElement.Value = desiredVersion;
        return true;
    }

    private static bool VersionsMatch(string left, string right)
    {
        if (NuGetVersion.TryParse(left, out var lv) && NuGetVersion.TryParse(right, out var rv))
            return lv == rv;

        return string.Equals(left?.Trim(), right?.Trim(), StringComparison.OrdinalIgnoreCase);
    }
}
