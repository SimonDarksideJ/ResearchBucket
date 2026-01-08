using NuGet.Common;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using MonoGameHub.Core.Interfaces;

namespace MonoGameHub.Core.Services;

/// <summary>
/// Service for interacting with NuGet
/// </summary>
public class NuGetService : INuGetService
{
    private readonly SourceRepository _repository;
    private readonly ILogger _logger;

    public NuGetService()
    {
        var packageSource = new NuGet.Configuration.PackageSource("https://api.nuget.org/v3/index.json");
        _repository = Repository.Factory.GetCoreV3(packageSource);
        _logger = NullLogger.Instance;
    }

    public async Task<List<string>> GetPackageVersionsAsync(string packageId, bool includePrerelease = false)
    {
        try
        {
            var metadataResource = await _repository.GetResourceAsync<PackageMetadataResource>();
            var metadata = await metadataResource.GetMetadataAsync(
                packageId,
                includePrerelease,
                includeUnlisted: false,
                new SourceCacheContext(),
                _logger,
                CancellationToken.None);

            return metadata
                .Select(m => m.Identity.Version.ToString())
                .OrderByDescending(v => v)
                .ToList();
        }
        catch
        {
            return new List<string>();
        }
    }

    public async Task<string?> ResolveWildcardVersionAsync(string packageId, string pattern)
    {
        try
        {
            var versions = await GetPackageVersionsAsync(packageId, pattern.Contains("develop") || pattern.Contains("preview"));
            
            // Parse the pattern (e.g., "3.8.*" or "3.8.5-develop.*")
            var versionRange = VersionRange.Parse(pattern);
            
            // Find the best match
            var nugetVersions = versions
                .Select(v => NuGetVersion.Parse(v))
                .Where(v => versionRange.Satisfies(v))
                .OrderByDescending(v => v)
                .FirstOrDefault();

            return nugetVersions?.ToString();
        }
        catch
        {
            return null;
        }
    }

    public async Task<PackageMetadata?> GetPackageMetadataAsync(string packageId, string version)
    {
        try
        {
            var metadataResource = await _repository.GetResourceAsync<PackageMetadataResource>();
            var metadata = await metadataResource.GetMetadataAsync(
                packageId,
                includePrerelease: true,
                includeUnlisted: false,
                new SourceCacheContext(),
                _logger,
                CancellationToken.None);

            var packageMetadata = metadata.FirstOrDefault(m => m.Identity.Version.ToString() == version);
            if (packageMetadata == null)
                return null;

            return new PackageMetadata
            {
                PackageId = packageId,
                Version = version,
                Description = packageMetadata.Description,
                Published = packageMetadata.Published?.UtcDateTime,
                Authors = packageMetadata.Authors
            };
        }
        catch
        {
            return null;
        }
    }
}
