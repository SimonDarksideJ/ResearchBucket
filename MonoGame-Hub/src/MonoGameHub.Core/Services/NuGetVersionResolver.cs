using NuGet.Common;
using NuGet.Configuration;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace MonoGameHub.Core.Services;

public sealed class NuGetVersionResolver
{
    private static readonly ILogger Logger = NullLogger.Instance;

    private static SourceRepository CreateDefaultRepository()
        => Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");

    private static async Task<IReadOnlyList<NuGetVersion>> GetAllNuGetVersionsAsync(
        string packageId,
        CancellationToken cancellationToken)
    {
        var repository = CreateDefaultRepository();
        var resource = await repository.GetResourceAsync<PackageMetadataResource>(cancellationToken);

        // Ensure prerelease versions are included (dotnet/NuGet can float to prerelease).
        var metadata = await resource.GetMetadataAsync(
            packageId,
            includePrerelease: true,
            includeUnlisted: false,
            new SourceCacheContext(),
            Logger,
            cancellationToken);

        return metadata
            .Select(m => m.Identity.Version)
            .Where(v => v is not null)
            .Distinct()
            .ToList()!;
    }

    public async Task<IReadOnlyList<string>> GetAllVersionsAsync(string packageId, CancellationToken cancellationToken)
    {
        var versions = await GetAllNuGetVersionsAsync(packageId, cancellationToken);

        return versions
            .Select(v => v.OriginalVersion)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .ToList()!;
    }

    public async Task<string?> ResolveWildcardAsync(
        string packageId,
        string wildcardVersionSpec,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(wildcardVersionSpec))
            return null;

        var all = await GetAllNuGetVersionsAsync(packageId, cancellationToken);
        if (all.Count == 0)
            return null;

        // Use NuGet's parser where possible.
        if (VersionRange.TryParse(wildcardVersionSpec, out var range))
        {
            var satisfying = all
                .Where(v => range.Satisfies(v))
                .OrderBy(v => v)
                .ToList();

            if (satisfying.Count == 0)
                return null;

            // dotnet/NuGet floating versions (e.g., 3.8.*) prefer stable versions.
            // Prerelease versions are only preferred when the spec explicitly contains a prerelease label.
            var specHasPrereleaseLabel = wildcardVersionSpec.Contains('-', StringComparison.Ordinal);

            NuGetVersion? best;
            if (!specHasPrereleaseLabel)
            {
                best = satisfying.LastOrDefault(v => !v.IsPrerelease) ?? satisfying.Last();
            }
            else
            {
                best = range.FindBestMatch(satisfying) ?? satisfying.Last();
            }

            return (best.OriginalVersion ?? best.ToNormalizedString());
        }

        // Fallback: prefix match + highest NuGetVersion.
        var prefix = wildcardVersionSpec.Replace("*", string.Empty);
        var candidates = all
            .Where(v => (v.OriginalVersion ?? v.ToNormalizedString())
                .StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .OrderBy(v => v)
            .ToList();

        return candidates.LastOrDefault()?.OriginalVersion;
    }
}
