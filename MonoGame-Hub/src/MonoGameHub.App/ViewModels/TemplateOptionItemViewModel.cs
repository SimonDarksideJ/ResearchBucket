using CommunityToolkit.Mvvm.ComponentModel;

namespace MonoGameHub.App.ViewModels;

public sealed partial class TemplateOptionItemViewModel : ObservableObject
{
    public TemplateOptionItemViewModel(string name, string templateId, string version, string? requiredWorkload)
    {
        Name = name;
        TemplateId = templateId;
        Version = version;
        RequiredWorkload = requiredWorkload;
    }

    public string Name { get; }
    public string TemplateId { get; }
    public string Version { get; }

    public string? RequiredWorkload { get; }

    public bool RequiresWorkload => !string.IsNullOrWhiteSpace(RequiredWorkload);

    [ObservableProperty]
    private bool _isRequiredWorkloadInstalled = true;

    public bool IsRequiredWorkloadMissing => RequiresWorkload && !IsRequiredWorkloadInstalled;

    public string RequiredWorkloadDisplay => RequiredWorkload ?? string.Empty;

    public void UpdateWorkloadInstalled(bool installed)
    {
        IsRequiredWorkloadInstalled = installed;
        OnPropertyChanged(nameof(IsRequiredWorkloadMissing));
    }
}
