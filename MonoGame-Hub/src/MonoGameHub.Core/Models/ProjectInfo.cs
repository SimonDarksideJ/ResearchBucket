namespace MonoGameHub.Core.Models;

public sealed record ProjectInfo(
    string Name,
    string ProjectPath,
    DateTimeOffset ProjectFileLastWriteTimeUtc,
    DateTimeOffset? LastOpenedUtc,
    IReadOnlyList<string> Platforms,
    bool UsesLegacyMgcb,
    bool UsesNewPipeline,
    string? MonoGameVersionSpec,
    string? ResolvedMonoGameVersion);
