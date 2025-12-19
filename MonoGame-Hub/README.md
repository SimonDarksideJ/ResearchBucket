# MonoGame Hub

A cross-platform desktop application for managing MonoGame projects, templates, and community content.

## Architecture

This application is built using:
- **Framework**: Avalonia UI 11.x
- **.NET**: 10.0
- **Pattern**: MVVM with dependency injection
- **Database**: SQLite for local project storage

## Project Structure

```
MonoGame-Hub/
├── MonoGameHub.Core/              # Core business logic
│   ├── Models/                    # Data models
│   ├── Services/                  # Business services
│   ├── Interfaces/                # Service contracts
│   └── Utilities/                 # Helper classes
├── MonoGameHub.Desktop/           # Avalonia UI application
│   ├── Views/                     # XAML views
│   ├── ViewModels/                # View models
│   ├── Converters/                # Value converters
│   ├── Styles/                    # Application styling
│   └── Assets/                    # Images, icons
└── MonoGameHub.Tests/             # Unit tests
```

## Building

```bash
dotnet build
```

## Running

```bash
dotnet run --project MonoGameHub.Desktop
```

## Testing

```bash
dotnet test
```

## Features

### Project Management
- Scan folders for MonoGame projects
- Detect project platforms (DesktopGL, WindowsDX, iOS, Android, etc.)
- Identify legacy MGCB vs modern Pipeline projects
- Build, run, and restore projects
- Open projects in preferred IDE

### Template Installation
- Browse available MonoGame template versions
- Install/uninstall templates
- Visual differentiation for pre-release versions

### Project Creation
- Create new MonoGame projects from templates
- Automatic dependency restoration
- Optional Git initialization
- IDE integration

### Settings
- Configure default project folder
- Manage NuGet cache location
- Select preferred IDE
- Theme customization

### Community Integration
- View MonoGame blog posts
- Browse community resources
- Search and filter by tags

## Documentation

For complete specifications, see:
- [Product Requirements Document](../MonoGame-Hub-PRD.md)
- [Framework Analysis](../Framework-Analysis.md)

## License

This project is part of the MonoGame ecosystem.
