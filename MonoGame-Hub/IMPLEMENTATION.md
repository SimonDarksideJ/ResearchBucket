# MonoGame Hub - Implementation Guide

## Phase 1: Core Services Implementation ✅

### Services Implemented

#### 1. ProjectService
**Location:** `MonoGameHub.Core/Services/ProjectService.cs`

**Features:**
- **Project Scanning**: Recursively scans folders for MonoGame projects
- **Project Analysis**: Parses `.csproj` files to extract:
  - MonoGame version (with wildcard resolution)
  - Platform targets (DesktopGL, WindowsDX, etc.)
  - Project type (Legacy MGCB vs Modern Pipeline)
  - .NET SDK version
- **Project Operations**:
  - Build: `dotnet build`
  - Run: `dotnet run`
  - Restore: `dotnet restore` (+ `dotnet tool restore` for legacy)
- **Project Management**: Save, load, and remove projects from tracking

**Detection Logic:**
```csharp
// Legacy MGCB Detection
- MonoGame.Content.Builder.Task package reference
- .mgcb file presence in Content folder

// Modern Pipeline Detection
- MonoGame.Framework without Builder.Task
- Console application with MonoGame.Pipeline reference

// Version Detection
- Exact: "3.8.1.303"
- Wildcard: "3.8.*" → resolves via NuGet
- Pre-release: "3.8.5-develop.*" → resolves latest dev version
```

#### 2. NuGetService
**Location:** `MonoGameHub.Core/Services/NuGetService.cs`

**Features:**
- **Package Version Queries**: Lists all available versions for a package
- **Wildcard Resolution**: Resolves patterns like `3.8.*` to latest matching version
- **Metadata Retrieval**: Gets package info (description, publish date, authors)
- **NuGet API Integration**: Uses NuGet.Protocol library for v3 API

**API Endpoint:** `https://api.nuget.org/v3/index.json`

#### 3. TemplateService
**Location:** `MonoGameHub.Core/Services/TemplateService.cs`

**Features:**
- **Template Discovery**: Queries NuGet for MonoGame.Templates.CSharp versions
- **Installation Management**:
  - Install: `dotnet new install MonoGame.Templates.CSharp::<version>`
  - Uninstall: `dotnet new uninstall MonoGame.Templates.CSharp`
  - Auto-uninstall before install (prevents conflicts)
- **Installed Templates**: Parses `dotnet new list` output
- **Version Classification**: Stable / Preview / Development

#### 4. SettingsService
**Location:** `MonoGameHub.Core/Services/SettingsService.cs`

**Features:**
- **Settings Persistence**: JSON file in `%APPDATA%/MonoGameHub/settings.json`
- **Environment Variables**: Updates `NUGET_PACKAGES` for cache folder
- **.NET Version Detection**: Queries `dotnet --version`
- **Default Settings**: Sensible defaults for all configuration

**Settings Stored:**
- Default project folder
- NuGet cache folder
- Theme (Light/Dark/System)
- Preferred IDE
- Language
- Update check preference

### Service Interfaces

All services implement interfaces in `MonoGameHub.Core/Interfaces/`:
- `IProjectService`
- `INuGetService`
- `ITemplateService`
- `ISettingsService`

This enables:
- Dependency injection
- Unit testing with mocks
- Future extensibility

### Testing

**Test Project:** `MonoGameHub.Tests`

**Tests Implemented:**
- `NuGetServiceTests`: Validates NuGet API integration
- `SettingsServiceTests`: Verifies settings persistence and loading

**Test Framework:** xUnit with async support

## Usage Examples

### Scanning for Projects

```csharp
var nugetService = new NuGetService();
var projectService = new ProjectService(nugetService);

var projects = await projectService.ScanFolderAsync("C:/Users/Dev/MonoGameProjects");

foreach (var project in projects)
{
    Console.WriteLine($"Found: {project.Name} - MonoGame {project.MonoGameVersion}");
    Console.WriteLine($"  Type: {project.ProjectType}");
    Console.WriteLine($"  Platforms: {string.Join(", ", project.Platforms)}");
}
```

### Installing Templates

```csharp
var nugetService = new NuGetService();
var templateService = new TemplateService(nugetService);

// Get available templates
var templates = await templateService.GetAvailableTemplatesAsync(includePrerelease: true);

// Install a specific version
var progress = new Progress<string>(msg => Console.WriteLine(msg));
await templateService.InstallTemplateAsync("3.8.1.303", progress);
```

### Managing Settings

```csharp
var settingsService = new SettingsService();

// Load settings
var settings = await settingsService.LoadSettingsAsync();

// Modify
settings.Theme = ThemeMode.Dark;
settings.PreferredIDE = IDEType.VSCode;

// Save
await settingsService.SaveSettingsAsync(settings);

// Get .NET version
var dotnetVersion = await settingsService.GetDotNetVersionAsync();
Console.WriteLine($".NET SDK: {dotnetVersion}");
```

## Next Steps

### Phase 2: UI Integration (To Be Implemented)

1. **ViewModels**:
   - Create ViewModels for each tab
   - Implement commands and data binding
   - Add progress reporting

2. **Projects Tab**:
   - Bind ProjectService to UI
   - Display project cards
   - Implement actions (open, build, run)

3. **Installs Tab**:
   - Bind TemplateService to UI
   - Show available/installed templates
   - Implement install/uninstall actions

4. **New Project Tab**:
   - Create project wizard
   - Template selection
   - Execute `dotnet new` with parameters

5. **Settings Tab**:
   - Bind SettingsService to UI
   - Folder pickers
   - IDE selection

6. **Additional Services**:
   - BlogService (for News tab)
   - ResourceService (for Resources tab)
   - IDEService (for opening projects in IDEs)

### Phase 3: Database Integration

- Replace in-memory project storage with SQLite
- Implement proper data persistence
- Add project history and favorites

### Phase 4: Community Features

- Implement blog feed fetching
- Add resource catalog integration
- Markdown rendering in UI

## Architecture Decisions

### Why These Services?

1. **Separation of Concerns**: Each service has a single responsibility
2. **Testability**: Interface-based design allows mocking
3. **Reusability**: Services can be used independently
4. **Cross-Platform**: All use .NET APIs that work on Windows/macOS/Linux

### Why NuGet.Protocol?

- Official NuGet client library
- Full v3 API support
- Version resolution built-in
- Well-maintained by Microsoft

### Why Process-Based Execution?

- `dotnet` CLI is the standard tool
- Works consistently across platforms
- No need to replicate complex logic
- Progress reporting via stdout/stderr

## Performance Considerations

### Async/Await
All I/O operations are async to prevent UI blocking

### Caching
- NuGet queries should be cached (future improvement)
- Project scan results stored in memory
- Settings loaded once per session

### Progress Reporting
All long-running operations support `IProgress<string>` for UI feedback

## Security Considerations

### File System Access
- Validates paths before operations
- Uses proper path combining
- No direct file deletion (only removes from tracking)

### Process Execution
- No shell execution (except `dotnet run` for user-initiated runs)
- Arguments are not vulnerable to injection
- Error output is captured and reported

### Network Access
- Only connects to official NuGet.org
- Uses HTTPS
- No authentication required

## Known Limitations

1. **Template Parsing**: Simplified regex-based parsing of `dotnet new list` output
2. **In-Memory Storage**: Projects not persisted between sessions (Phase 3)
3. **No IDE Detection**: IDE opening not implemented yet (Phase 2)
4. **Limited Error Handling**: Basic try-catch, could be more specific

## Build Status

✅ All projects build successfully  
✅ 0 warnings, 0 errors  
✅ .NET 10.0 compatibility maintained  
✅ Cross-platform support verified

## Documentation

- API documentation via XML comments
- Interface documentation in code
- Usage examples in this file
- PRD reference: `../MonoGame-Hub-PRD.md`
