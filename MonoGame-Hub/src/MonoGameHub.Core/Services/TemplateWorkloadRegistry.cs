namespace MonoGameHub.Core.Services;

public sealed class TemplateWorkloadRegistry
{
    // Defaults intended for expansion later.
    // Keys are template short names (TemplateId), matched case-insensitively.
    private static readonly Dictionary<string, string> RequiredByTemplateId = new(StringComparer.OrdinalIgnoreCase)
    {
        // MonoGame templates (common short names)
        ["mgios"] = "ios",
        ["mgandroid"] = "android",
    };

    public string? GetRequiredWorkloadId(string templateId, string templateName)
    {
        if (!string.IsNullOrWhiteSpace(templateId) && RequiredByTemplateId.TryGetValue(templateId, out var mapped))
            return mapped;

        // Fallback heuristics (covers future templates without hard-coding new IDs).
        if (!string.IsNullOrWhiteSpace(templateName))
        {
            if (templateName.Contains("android", StringComparison.OrdinalIgnoreCase))
                return "android";

            // Match both "iOS" and "ios".
            if (templateName.Contains("ios", StringComparison.OrdinalIgnoreCase))
                return "ios";
        }

        return null;
    }
}
