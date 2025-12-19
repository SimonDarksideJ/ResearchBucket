# MonoGame Hub - Product Requirements Document

## Executive Summary

The MonoGame Hub is a cross-platform desktop application designed to streamline the MonoGame development experience, providing developers with a unified interface for project management, template installation, and community engagement. Inspired by Unity Hub's successful approach, the MonoGame Hub will serve as the central command center for all MonoGame development activities.

### Vision Statement
To create a modern, intuitive, and powerful hub that empowers MonoGame developers to efficiently manage their projects, stay connected with the community, and maintain their development environment across Windows, macOS, and Linux platforms.

---

## Technical Architecture

> **Note**: For a comprehensive analysis of framework options, see [Framework-Analysis.md](Framework-Analysis.md), which compares 8 modern cross-platform frameworks including Avalonia UI, .NET MAUI, Uno Platform, Electron, Tauri, Flutter, Qt, and Neutralinojs. Avalonia UI scored 93/100 and was selected for its perfect .NET integration and true cross-platform support including Linux.

### Framework Selection: Avalonia UI

**Primary Framework:** Avalonia UI (https://avaloniaui.net/)

**Rationale:**
- **True Cross-Platform Support**: Single codebase runs on Windows, macOS, and Linux
- **Native .NET Integration**: Perfect integration with .NET ecosystem
- **Modern UI Capabilities**: XAML-based with MVVM architecture
- **Performance**: Hardware-accelerated rendering
- **Styling & Theming**: Flexible styling system supporting modern dark themes
- **Active Development**: Well-maintained with strong community support
- **Designer Support**: Visual Studio and Rider integration

**Technology Stack:**
- **UI Framework**: Avalonia UI 11.x (with .NET 10 support)
- **Architecture Pattern**: MVVM (Model-View-ViewModel)
- **Dependency Injection**: Microsoft.Extensions.DependencyInjection
- **.NET Version**: .NET 10.0 (current latest)
- **Package Management**: NuGet
- **Data Storage**: SQLite for local project database
- **HTTP Client**: System.Net.Http for API calls
- **Markdown Rendering**: Markdown.Avalonia for blog/resource display
- **Process Management**: System.Diagnostics.Process for CLI operations

### Architecture Patterns

**MVVM Architecture:**
```
┌─────────────────────────────────────────┐
│           Presentation Layer            │
│  (Views - XAML, Code-behind minimal)   │
└──────────────────┬──────────────────────┘
                   │ Data Binding
┌──────────────────▼──────────────────────┐
│          ViewModel Layer                │
│  (Business Logic, Commands, Observables)│
└──────────────────┬──────────────────────┘
                   │ Service Calls
┌──────────────────▼──────────────────────┐
│           Service Layer                 │
│  (Project, Template, NuGet, Web APIs)  │
└──────────────────┬──────────────────────┘
                   │
┌──────────────────▼──────────────────────┐
│            Data Layer                   │
│  (File System, SQLite, Registry)       │
└─────────────────────────────────────────┘
```

**Project Structure:**
```
MonoGameHub/
├── MonoGameHub.Core/              # Core business logic
│   ├── Models/                    # Data models
│   ├── Services/                  # Business services
│   │   ├── ProjectService.cs
│   │   ├── TemplateService.cs
│   │   ├── NuGetService.cs
│   │   ├── BlogService.cs
│   │   └── ResourceService.cs
│   ├── Interfaces/                # Service contracts
│   └── Utilities/                 # Helper classes
├── MonoGameHub.Desktop/           # Avalonia UI application
│   ├── Views/                     # XAML views
│   │   ├── MainWindow.xaml
│   │   ├── ProjectsView.xaml
│   │   ├── InstallsView.xaml
│   │   ├── CreateProjectView.xaml
│   │   ├── SettingsView.xaml
│   │   ├── BlogView.xaml
│   │   └── ResourcesView.xaml
│   ├── ViewModels/                # View models
│   ├── Converters/                # Value converters
│   ├── Styles/                    # Application styling
│   └── Assets/                    # Images, icons
├── MonoGameHub.Tests/             # Unit tests
└── docs/                          # Documentation
```

---

## Core Features & Requirements

### 1. Project Management

#### 1.1 Project Discovery & Listing
**Requirements:**
- Scan configured project folder(s) recursively
- Detect .csproj files and identify MonoGame projects
- Extract project metadata:
  - Project name
  - Platform targets (DesktopGL, WindowsDX, iOS, Android, etc.)
  - MonoGame version
  - Template type (legacy MGCB vs new Pipeline)
  - Last modified date
  - .NET SDK version

**Detection Logic:**
```csharp
// Legacy MGCB Detection
- Check for "MonoGame.Framework.Content.Pipeline" package reference
- Check for ".mgcb" file in Content folder
- Check for "MonoGame.Content.Builder.Task" package

// New Pipeline Detection
- Check for console application with "MonoGame.Pipeline" reference
- Check for "MonoGame.Framework" >= 3.8.2 with ContentReference items

// Platform Detection
- Parse .csproj <TargetFramework> and <OutputType>
- Identify platform-specific project files (iOS.csproj, Android.csproj, etc.)
```

**Version Detection:**
```csharp
// Parse PackageReference versions
- Exact version: "3.8.1.303"
- Wildcard: "3.8.*" (query NuGet for latest matching)
- Pre-release: "3.8.5-develop.*" (query NuGet for latest pre-release)
```

#### 1.2 Project Actions
- **Open in IDE**: Launch project in user's preferred IDE (VS, VS Code, Rider)
- **Open in File Explorer**: Navigate to project folder
- **Build Project**: Execute `dotnet build`
- **Run Project**: Execute `dotnet run`
- **Restore Dependencies**: Execute `dotnet restore` and `dotnet tool restore`
- **Remove from List**: Remove project from tracked list (doesn't delete files)
- **Show Details**: Display detailed project information panel

### 2. Template Management (Installs Tab)

#### 2.1 Template Discovery
**Requirements:**
- Query NuGet API for MonoGame template packages
- List available versions:
  - Stable releases (e.g., 3.8.1.303)
  - Preview releases (e.g., 3.8.2-preview1)
  - Development releases (e.g., 3.8.5-develop.*)
- Display currently installed templates (via `dotnet new list`)

**NuGet API Integration:**
```csharp
// Query NuGet.org API
- Package: "MonoGame.Templates.CSharp"
- Endpoint: https://api.nuget.org/v3/index.json
- Filter by version stability (stable, preview, dev)
```

#### 2.2 Template Installation/Uninstallation
**Requirements:**
- Install template: `dotnet new install MonoGame.Templates.CSharp::<version>`
- Uninstall existing before installing new: `dotnet new uninstall MonoGame.Templates.CSharp`
- Progress indication during installation
- Error handling and rollback

**Visual Indicators:**
- **Stable Releases**: Default dark background, white text
- **Pre-release/Development**: MonoGame Orange (#E73C00) background, white text
- **Installed**: Green checkmark badge
- **Not Installed**: Gray "Install" button

### 3. Project Creation

#### 3.1 New Project Wizard
**Requirements:**
- **Project Name**: User input field with validation
- **Location**: Default or custom folder selection
- **Template Selection**: Dropdown with available templates
  - MonoGame Cross-Platform Desktop Application (DesktopGL)
  - MonoGame Windows Desktop Application (WindowsDX)
  - MonoGame iOS Application
  - MonoGame Android Application
  - MonoGame Shared Library Project
- **MonoGame Version**: Select from installed template versions
- **Advanced Options** (collapsible):
  - .NET SDK version
  - Include Git repository initialization
  - Create solution file

#### 3.2 Project Creation Flow
```
1. Validate inputs
2. Create project folder
3. Execute: dotnet new <template> -n <ProjectName> -o <Location>
4. Execute: dotnet restore
5. Execute: dotnet tool restore (if legacy MGCB)
6. Add project to database
7. (Optional) Initialize Git repo
8. (Optional) Open in IDE
```

### 4. Settings Management

#### 4.1 General Settings
- **Default Project Folder**: Path selector
- **Default NuGet Cache Folder**: Path selector (updates `NUGET_PACKAGES` env var)
- **Theme**: Light / Dark / System
- **Language**: English (extensible for future localization)

#### 4.2 Development Environment
- **Detected .NET SDK Version**: Display current version
- **Install .NET Links**:
  - .NET 9 download link (platform-specific)
  - .NET 10 download link (platform-specific)
  - .NET 11 download link (platform-specific)
- **Preferred IDE**: Dropdown selection
  - Visual Studio (Windows only)
  - Visual Studio Code
  - JetBrains Rider
  - System Default

#### 4.3 IDE Integration
**Detection Logic:**
```csharp
// Visual Studio (Windows)
- Check registry: HKLM\SOFTWARE\Microsoft\VisualStudio\
- Check common paths: C:\Program Files\Microsoft Visual Studio\

// VS Code
- Check PATH for 'code' command
- Check common install locations

// Rider
- Check PATH for 'rider' command
- Check common install locations
```

### 5. Community Integration

#### 5.1 Blog Feed
**Requirements:**
- Fetch blog posts from https://monogame.net/blog/
- Display post list with:
  - Title
  - Published date
  - Author
  - Excerpt/summary
  - Featured image
- **Filter by Tags**: Dynamic tag list extracted from posts
- **Search**: Full-text search across titles and content
- **View Post**: Render markdown content within the hub
- **Open in Browser**: External link to full post

**Implementation:**
```csharp
// RSS/Atom feed or web scraping
- Parse blog posts (RSS preferred)
- Cache locally with TTL (Time To Live)
- Background refresh every 6 hours
- Markdown rendering with Markdown.Avalonia
```

#### 5.2 Resources Directory
**Requirements:**
- Fetch resources from https://monogame.net/resources/
- Display resource cards with:
  - Title
  - Description
  - Category/Type
  - Tags
  - External link
- **Filter by Tags**: Tag-based filtering
- **Filter by Category**: Asset packs, tools, tutorials, etc.
- **Search**: Search across titles and descriptions
- **Open Resource**: Navigate to external resource link

---

## UI/UX Design Approach

### Design Philosophy
**Inspired by Unity Hub, but uniquely MonoGame:**
- Clean, modern interface with focus on content
- MonoGame brand identity (orange accent color #E73C00)
- Dark theme as default (light theme optional)
- Intuitive navigation with clear visual hierarchy
- Responsive design adapting to window sizes

### Color Palette

**Primary Colors:**
- **MonoGame Orange**: #E73C00 (brand accent, CTAs, pre-release tags)
- **Dark Background**: #1E1E1E (main background)
- **Secondary Background**: #2D2D30 (cards, panels)
- **Border/Divider**: #3E3E42

**Semantic Colors:**
- **Success**: #4EC9B0 (installed, successful operations)
- **Warning**: #CCA700 (pre-release, warnings)
- **Error**: #F48771 (errors, critical issues)
- **Info**: #569CD6 (information, hints)

**Text Colors:**
- **Primary Text**: #FFFFFF
- **Secondary Text**: #CCCCCC
- **Disabled Text**: #808080

### Typography
- **Primary Font**: Segoe UI (Windows), San Francisco (macOS), Roboto (Linux)
- **Monospace Font**: Consolas, Monaco, or Source Code Pro (for versions, paths)
- **Font Sizes**:
  - H1: 28px (page titles)
  - H2: 22px (section headers)
  - H3: 18px (subsection headers)
  - Body: 14px (standard text)
  - Caption: 12px (metadata, hints)

---

## Screen Specifications

### Main Window Layout

```
┌────────────────────────────────────────────────────────┐
│  ┌──────┐  MonoGame Hub                    ⚙️  [_][□][X]│
│  │ LOGO │                                                │
│  └──────┘                                                │
├────────────────────────────────────────────────────────┤
│ 📂 Projects │ 📦 Installs │ ➕ New │ ⚙️ Settings │ 📰 News │ 📚 Resources │
├────────────────────────────────────────────────────────┤
│                                                          │
│                   [Content Area]                         │
│                                                          │
│                                                          │
│                                                          │
├────────────────────────────────────────────────────────┤
│  Status: Ready               MonoGame v3.8.1 | .NET 10.0 │
└────────────────────────────────────────────────────────┘
```

### 1. Projects Tab

**Layout:**
```
┌────────────────────────────────────────────────────────┐
│  🔍 Search projects...              📁 Add Folder  ⟳   │
├────────────────────────────────────────────────────────┤
│  ┌──────────────────────────────────────────────────┐  │
│  │ 🎮 My Awesome Game                         v3.8.1│  │
│  │ 📍 C:\Projects\MyAwesomeGame                      │  │
│  │ 🖥️  DesktopGL | WindowsDX     ⏰ Modified 2d ago  │  │
│  │ [Open] [Build] [Run] [⋮ More]                    │  │
│  └──────────────────────────────────────────────────┘  │
│  ┌──────────────────────────────────────────────────┐  │
│  │ 🎮 Platformer Demo                  v3.8.5-dev.* │  │
│  │ 📍 C:\Projects\PlatformerDemo                     │  │
│  │ 🖥️  DesktopGL | iOS | Android  ⏰ Modified 1w ago│  │
│  │ [Open] [Build] [Run] [⋮ More]                    │  │
│  └──────────────────────────────────────────────────┘  │
│                                                          │
└────────────────────────────────────────────────────────┘
```

**Project Card Details:**
- **Icon**: Project type icon (game controller, library, etc.)
- **Title**: Project name (bold, large)
- **Path**: Full project path (secondary text, smaller)
- **Platforms**: Chips/badges for each detected platform
- **Version**: MonoGame version (aligned right, colored based on type)
- **Modified Date**: Relative time (e.g., "2 days ago")
- **Actions**: Primary buttons (Open, Build, Run) + overflow menu

**Overflow Menu Actions:**
- Open in File Explorer
- Restore Dependencies
- Clean Build
- Show Details
- Remove from List

### 2. Installs Tab

**Layout:**
```
┌────────────────────────────────────────────────────────┐
│  MonoGame Template Versions              ⟳ Refresh     │
├────────────────────────────────────────────────────────┤
│  ☑ Show Pre-releases    ☑ Show Development Builds     │
├────────────────────────────────────────────────────────┤
│  ┌──────────────────────────────────────────────────┐  │
│  │ ✅ MonoGame Templates v3.8.1.303       [Installed]│  │
│  │ Stable Release | Released: Jan 15, 2024           │  │
│  │ [Uninstall] [View Documentation]                  │  │
│  └──────────────────────────────────────────────────┘  │
│  ┌──────────────────────────────────────────────────┐  │
│  │ 🟠 MonoGame Templates v3.8.5-develop.*  [Install]│  │
│  │ Development Build | Released: Dec 10, 2024        │  │
│  │ [Install] [View Documentation]                    │  │
│  └──────────────────────────────────────────────────┘  │
│  ┌──────────────────────────────────────────────────┐  │
│  │ MonoGame Templates v3.8.2-preview1      [Install]│  │
│  │ Preview Release | Released: Oct 5, 2024           │  │
│  │ [Install] [View Documentation]                    │  │
│  └──────────────────────────────────────────────────┘  │
│                                                          │
└────────────────────────────────────────────────────────┘
```

**Version Card Details:**
- **Status Icon**: Checkmark (installed) or blank
- **Badge**: Orange background for pre-release/dev
- **Version Number**: Large, prominent
- **Release Type**: Stable / Preview / Development
- **Release Date**: Date published
- **Actions**: Install/Uninstall + documentation link

### 3. New Project Tab

**Layout:**
```
┌────────────────────────────────────────────────────────┐
│                  Create New Project                     │
├────────────────────────────────────────────────────────┤
│                                                          │
│  Project Name *                                         │
│  ┌────────────────────────────────────────────────┐    │
│  │ MyNewGame                                       │    │
│  └────────────────────────────────────────────────┘    │
│                                                          │
│  Location *                                             │
│  ┌──────────────────────────────────────────┐ [Browse] │
│  │ C:\Users\Dev\MonoGameProjects            │          │
│  └──────────────────────────────────────────┘          │
│                                                          │
│  Template *                                             │
│  ┌────────────────────────────────────────────────┐    │
│  │ MonoGame Cross-Platform Desktop (DesktopGL) ▼  │    │
│  └────────────────────────────────────────────────┘    │
│                                                          │
│  MonoGame Version *                                     │
│  ┌────────────────────────────────────────────────┐    │
│  │ 3.8.1.303 (Stable)                           ▼ │    │
│  └────────────────────────────────────────────────┘    │
│                                                          │
│  ▼ Advanced Options                                     │
│                                                          │
│  ☑ Initialize Git repository                           │
│  ☑ Open project after creation                         │
│                                                          │
│                                   [Cancel]  [Create]    │
│                                                          │
└────────────────────────────────────────────────────────┘
```

**Form Validation:**
- Project name: Required, alphanumeric + spaces, valid folder name
- Location: Required, must be valid writable path
- Template: Required, must select from available templates
- Version: Required, only show installed template versions

**Creation Progress Dialog:**
```
┌──────────────────────────────────────┐
│  Creating Project...                 │
│                                      │
│  ✓ Creating folder structure         │
│  ⏳ Running dotnet new...            │
│  ⬜ Restoring dependencies           │
│  ⬜ Initializing Git                 │
│                                      │
│  [████████░░░░░░░░] 60%              │
└──────────────────────────────────────┘
```

### 4. Settings Tab

**Layout:**
```
┌────────────────────────────────────────────────────────┐
│  ⚙️ Settings                                            │
├────────────────────────────────────────────────────────┤
│  ┌─ General ────────────────────────────────────────┐  │
│  │                                                   │  │
│  │  Default Project Folder                          │  │
│  │  ┌────────────────────────────────┐ [Browse]    │  │
│  │  │ C:\Users\Dev\MonoGameProjects  │             │  │
│  │  └────────────────────────────────┘             │  │
│  │                                                   │  │
│  │  NuGet Cache Folder                              │  │
│  │  ┌────────────────────────────────┐ [Browse]    │  │
│  │  │ C:\Users\Dev\.nuget\packages   │             │  │
│  │  └────────────────────────────────┘             │  │
│  │                                                   │  │
│  │  Theme                                           │  │
│  │  ○ Light  ● Dark  ○ System                      │  │
│  │                                                   │  │
│  └───────────────────────────────────────────────────┘  │
│  ┌─ Development Environment ─────────────────────────┐  │
│  │                                                   │  │
│  │  Current .NET SDK: 10.0.101                      │  │
│  │  [Download .NET 9] [Download .NET 10]           │  │
│  │                                                   │  │
│  │  Preferred IDE                                   │  │
│  │  ┌──────────────────────────────┐               │  │
│  │  │ Visual Studio Code         ▼ │               │  │
│  │  └──────────────────────────────┘               │  │
│  │                                                   │  │
│  └───────────────────────────────────────────────────┘  │
│                                                          │
│                           [Reset] [Save Changes]        │
└────────────────────────────────────────────────────────┘
```

### 5. News Tab (Blog Integration)

**Layout:**
```
┌────────────────────────────────────────────────────────┐
│  📰 MonoGame News                         ⟳ Refresh    │
├────────────────────────────────────────────────────────┤
│  🔍 Search articles...                                  │
│  🏷️  [All] [Releases] [Tutorials] [Community] [Events] │
├────────────────────────────────────────────────────────┤
│  ┌──────────────────────────────────────────────────┐  │
│  │ [Featured Image]                                  │  │
│  │ MonoGame 3.8.2 Released!                         │  │
│  │ 📅 December 1, 2024 | ✍️ MonoGame Team            │  │
│  │ We're excited to announce MonoGame 3.8.2...      │  │
│  │ [Read More] [Open in Browser]                    │  │
│  └──────────────────────────────────────────────────┘  │
│  ┌──────────────────────────────────────────────────┐  │
│  │ Getting Started with MonoGame Pipeline           │  │
│  │ 📅 November 15, 2024 | ✍️ John Developer          │  │
│  │ Learn how to use the new MonoGame Pipeline...    │  │
│  │ [Read More] [Open in Browser]                    │  │
│  └──────────────────────────────────────────────────┘  │
│                                                          │
└────────────────────────────────────────────────────────┘
```

**Article View (Modal/Panel):**
```
┌────────────────────────────────────────────────────────┐
│  ← Back to News                      🔗 Open in Browser │
├────────────────────────────────────────────────────────┤
│                                                          │
│  # MonoGame 3.8.2 Released!                             │
│  📅 December 1, 2024 | ✍️ MonoGame Team                  │
│  🏷️  Releases, Announcements                            │
│                                                          │
│  [Article content rendered with Markdown.Avalonia]      │
│                                                          │
│                                                          │
└────────────────────────────────────────────────────────┘
```

### 6. Resources Tab

**Layout:**
```
┌────────────────────────────────────────────────────────┐
│  📚 MonoGame Resources                    ⟳ Refresh    │
├────────────────────────────────────────────────────────┤
│  🔍 Search resources...                                 │
│  📂 [All] [Tools] [Assets] [Tutorials] [Extensions]    │
│  🏷️  Filter by tags...                                 │
├────────────────────────────────────────────────────────┤
│  ┌────────────┐ ┌────────────┐ ┌────────────┐         │
│  │ [Icon]     │ │ [Icon]     │ │ [Icon]     │         │
│  │ Content    │ │ MonoGame   │ │ Pipeline   │         │
│  │ Pipeline   │ │ Extended   │ │ Importer   │         │
│  │ Extension  │ │ Toolkit    │ │ Templates  │         │
│  │ [Visit]    │ │ [Visit]    │ │ [Visit]    │         │
│  └────────────┘ └────────────┘ └────────────┘         │
│  ┌────────────┐ ┌────────────┐ ┌────────────┐         │
│  │ [Icon]     │ │ [Icon]     │ │ [Icon]     │         │
│  │ Sprite     │ │ Particle   │ │ Tiled Map  │         │
│  │ Batch      │ │ Effects    │ │ Integration│         │
│  │ Tutorial   │ │ Library    │ │            │         │
│  │ [Visit]    │ │ [Visit]    │ │ [Visit]    │         │
│  └────────────┘ └────────────┘ └────────────┘         │
│                                                          │
└────────────────────────────────────────────────────────┘
```

**Resource Card (Grid Layout):**
- Icon/Thumbnail
- Title
- Brief description
- Tags (chips)
- Category badge
- "Visit" button to open external link

---

## Data Models

### Project Model
```csharp
public class MonoGameProject
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Path { get; set; }
    public List<string> Platforms { get; set; }
    public string MonoGameVersion { get; set; }
    public VersionType VersionType { get; set; } // Stable, Preview, Development
    public ProjectType ProjectType { get; set; } // Legacy, Modern
    public string DotNetVersion { get; set; }
    public DateTime LastModified { get; set; }
    public DateTime LastScanned { get; set; }
    public bool IsFavorite { get; set; }
}

public enum VersionType
{
    Stable,
    Preview,
    Development
}

public enum ProjectType
{
    LegacyMGCB,
    ModernPipeline
}
```

### Template Model
```csharp
public class MonoGameTemplate
{
    public string PackageId { get; set; } // "MonoGame.Templates.CSharp"
    public string Version { get; set; }
    public string DisplayName { get; set; }
    public VersionType Type { get; set; }
    public DateTime PublishedDate { get; set; }
    public bool IsInstalled { get; set; }
    public string Description { get; set; }
    public string DocumentationUrl { get; set; }
    public List<string> AvailableTemplates { get; set; }
}
```

### Blog Post Model
```csharp
public class BlogPost
{
    public string Id { get; set; }
    public string Title { get; set; }
    public string Slug { get; set; }
    public string Author { get; set; }
    public DateTime PublishedDate { get; set; }
    public string Excerpt { get; set; }
    public string Content { get; set; } // Markdown
    public string FeaturedImageUrl { get; set; }
    public List<string> Tags { get; set; }
    public string Url { get; set; }
}
```

### Resource Model
```csharp
public class Resource
{
    public string Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string Category { get; set; }
    public List<string> Tags { get; set; }
    public string Url { get; set; }
    public string IconUrl { get; set; }
    public ResourceType Type { get; set; }
}

public enum ResourceType
{
    Tool,
    AssetPack,
    Tutorial,
    Extension,
    Library
}
```

### Settings Model
```csharp
public class AppSettings
{
    public string DefaultProjectFolder { get; set; }
    public string NuGetCacheFolder { get; set; }
    public ThemeMode Theme { get; set; }
    public IDEType PreferredIDE { get; set; }
    public string Language { get; set; }
    public bool CheckForUpdatesOnStartup { get; set; }
}

public enum ThemeMode
{
    Light,
    Dark,
    System
}

public enum IDEType
{
    VisualStudio,
    VSCode,
    Rider,
    SystemDefault
}
```

---

## Service Specifications

### 1. ProjectService
**Responsibilities:**
- Scan folders for MonoGame projects
- Parse .csproj files to extract metadata
- Detect project type (legacy/modern)
- Detect platforms
- Resolve MonoGame version (including wildcards)
- CRUD operations for project database
- Execute project actions (build, run, restore)

**Key Methods:**
```csharp
Task<List<MonoGameProject>> ScanFolderAsync(string path);
Task<MonoGameProject> AnalyzeProjectAsync(string projectPath);
Task<string> ResolveVersionAsync(string versionPattern);
Task BuildProjectAsync(MonoGameProject project, IProgress<string> progress);
Task RunProjectAsync(MonoGameProject project);
Task RestoreProjectAsync(MonoGameProject project, IProgress<string> progress);
```

### 2. TemplateService
**Responsibilities:**
- Query NuGet API for MonoGame templates
- List installed templates via `dotnet new list`
- Install templates with version-specific commands
- Uninstall templates
- Track installation status

**Key Methods:**
```csharp
Task<List<MonoGameTemplate>> GetAvailableTemplatesAsync(bool includePrerelease);
Task<List<MonoGameTemplate>> GetInstalledTemplatesAsync();
Task InstallTemplateAsync(string version, IProgress<string> progress);
Task UninstallTemplateAsync(string version);
Task<bool> IsTemplateInstalledAsync(string version);
```

### 3. NuGetService
**Responsibilities:**
- Query NuGet API for package information
- Resolve wildcard versions to specific versions
- Get package metadata (publish date, description)
- Check local NuGet cache

**Key Methods:**
```csharp
Task<List<string>> GetPackageVersionsAsync(string packageId, bool includePrerelease);
Task<string> ResolveWildcardVersionAsync(string packageId, string pattern);
Task<PackageMetadata> GetPackageMetadataAsync(string packageId, string version);
```

### 4. BlogService
**Responsibilities:**
- Fetch blog posts from MonoGame website
- Parse RSS/Atom feed or scrape HTML
- Cache posts locally
- Filter and search posts
- Render markdown content

**Key Methods:**
```csharp
Task<List<BlogPost>> FetchBlogPostsAsync(bool forceRefresh = false);
Task<BlogPost> GetBlogPostDetailsAsync(string postId);
List<BlogPost> FilterByTag(List<BlogPost> posts, string tag);
List<BlogPost> SearchPosts(List<BlogPost> posts, string query);
```

### 5. ResourceService
**Responsibilities:**
- Fetch resources from MonoGame website
- Parse resource data
- Cache resources locally
- Filter by category and tags
- Search resources

**Key Methods:**
```csharp
Task<List<Resource>> FetchResourcesAsync(bool forceRefresh = false);
List<Resource> FilterByCategory(List<Resource> resources, string category);
List<Resource> FilterByTags(List<Resource> resources, List<string> tags);
List<Resource> SearchResources(List<Resource> resources, string query);
```

### 6. IDEService
**Responsibilities:**
- Detect installed IDEs on the system
- Open projects in specified IDE
- Platform-specific IDE detection and launching

**Key Methods:**
```csharp
Task<List<IDEInfo>> DetectInstalledIDEsAsync();
Task<bool> IsIDEInstalledAsync(IDEType ide);
Task OpenProjectInIDEAsync(string projectPath, IDEType ide);
```

### 7. SettingsService
**Responsibilities:**
- Load and save application settings
- Update environment variables (NuGet cache)
- Validate settings
- Provide defaults

**Key Methods:**
```csharp
Task<AppSettings> LoadSettingsAsync();
Task SaveSettingsAsync(AppSettings settings);
Task UpdateNuGetCacheFolderAsync(string path);
Task<string> GetDotNetVersionAsync();
```

---

## Implementation Phases

### Phase 1: Foundation (Weeks 1-2)
**Goal:** Set up project structure and core infrastructure

**Deliverables:**
- [x] Create Avalonia UI solution structure
- [x] Set up MVVM architecture with DI
- [x] Implement basic navigation between tabs
- [x] Design and implement theme/styling system
- [x] Create data models
- [x] Set up SQLite database for project storage
- [x] Implement SettingsService
- [x] Create basic Settings UI

### Phase 2: Project Management (Weeks 3-4)
**Goal:** Implement core project discovery and management features

**Deliverables:**
- [ ] Implement ProjectService with project scanning
- [ ] Create project detection logic (legacy vs modern)
- [ ] Build Projects tab UI with project cards
- [ ] Implement project actions (open, build, run)
- [ ] Add project filtering and search
- [ ] Create project details panel

### Phase 3: Template Management (Weeks 5-6)
**Goal:** Enable template installation and management

**Deliverables:**
- [ ] Implement TemplateService
- [ ] Implement NuGetService
- [ ] Create Installs tab UI
- [ ] Add template listing with version detection
- [ ] Implement template installation/uninstallation
- [ ] Add progress indicators for long operations

### Phase 4: Project Creation (Week 7)
**Goal:** Build new project creation wizard

**Deliverables:**
- [ ] Create New Project UI with form validation
- [ ] Implement project creation workflow
- [ ] Add template selection and version picker
- [ ] Implement advanced options (Git, open after creation)
- [ ] Add creation progress dialog
- [ ] Integrate with IDEService

### Phase 5: IDE Integration (Week 8)
**Goal:** Detect and integrate with development environments

**Deliverables:**
- [ ] Implement IDEService
- [ ] Add IDE detection for VS, VS Code, Rider
- [ ] Platform-specific launch logic
- [ ] Add IDE preference in Settings
- [ ] Test IDE launching on all platforms

### Phase 6: Community Integration (Weeks 9-10)
**Goal:** Integrate blog and resources

**Deliverables:**
- [ ] Implement BlogService
- [ ] Create News tab UI with blog posts
- [ ] Add tag filtering and search
- [ ] Implement markdown rendering
- [ ] Implement ResourceService
- [ ] Create Resources tab UI
- [ ] Add resource filtering and search

### Phase 7: Polish & Testing (Weeks 11-12)
**Goal:** Refinement, testing, and performance optimization

**Deliverables:**
- [ ] Comprehensive testing (unit, integration, UI)
- [ ] Performance optimization
- [ ] Error handling improvements
- [ ] Accessibility improvements
- [ ] User documentation
- [ ] Build installers for Windows, macOS, Linux

### Phase 8: Beta Release (Week 13)
**Goal:** Deploy beta version for community testing

**Deliverables:**
- [ ] Beta release packages
- [ ] Release notes
- [ ] Bug tracking system setup
- [ ] Gather community feedback
- [ ] Iterate based on feedback

---

## Testing Strategy

### Unit Tests
**Coverage:**
- Service layer logic
- Version parsing and resolution
- Project detection algorithms
- NuGet API interactions
- File system operations (mocked)

**Framework:** xUnit

### Integration Tests
**Coverage:**
- End-to-end project creation
- Template installation workflows
- Project scanning and analysis
- IDE launching
- Settings persistence

### UI Tests
**Coverage:**
- Navigation between tabs
- Form validation
- User interactions
- Theme switching
- Responsive layout

**Framework:** Avalonia UI Testing

### Platform Testing
**Platforms:**
- Windows 10/11 (x64)
- macOS 12+ (Apple Silicon and Intel)
- Ubuntu 20.04+ / Fedora 36+

**Manual Testing Checklist:**
- [ ] Project scanning on each platform
- [ ] Template installation
- [ ] Project creation
- [ ] IDE launching
- [ ] Settings persistence
- [ ] Theme rendering
- [ ] Performance benchmarks

---

## Security & Privacy Considerations

### Data Storage
- Local SQLite database for project metadata
- No telemetry or usage tracking
- Settings stored in user's local app data folder
- No cloud synchronization

### External Dependencies
- NuGet API (read-only)
- MonoGame website (read-only)
- No authentication required
- No personal data transmitted

### Process Execution
- Execute `dotnet` CLI commands with proper escaping
- Validate all user inputs before file operations
- Restrict file operations to user-specified folders
- No elevated privileges required

---

## Performance Targets

### Startup Time
- **Target:** < 2 seconds on modern hardware
- **Strategy:** Lazy loading of tabs, background cache warming

### Project Scanning
- **Target:** < 5 seconds for 100 projects
- **Strategy:** Parallel scanning, caching, incremental updates

### Template Installation
- **Target:** Progress feedback within 1 second
- **Strategy:** Async operations with real-time progress

### UI Responsiveness
- **Target:** 60 FPS for animations, < 100ms for interactions
- **Strategy:** Background threads for I/O, async/await patterns

---

## Accessibility

### WCAG 2.1 Compliance
- **Level:** AA minimum, AAA target
- **Keyboard Navigation:** Full keyboard support for all features
- **Screen Readers:** ARIA labels and roles for all UI elements
- **Color Contrast:** 4.5:1 minimum for text
- **Focus Indicators:** Clear visual focus states

### Features
- Tab navigation
- Keyboard shortcuts for common actions
- High contrast mode support
- Scalable UI (respect system DPI settings)
- Alternative text for all images/icons

---

## Localization

### Phase 1 (Initial Release)
- English only

### Future Phases
- Resource string externalization
- Support for additional languages:
  - Spanish
  - French
  - German
  - Japanese
  - Chinese (Simplified)
  - Portuguese

---

## Maintenance & Updates

### Update Mechanism
- Check for updates on startup (opt-in)
- GitHub Releases for version distribution
- Semantic versioning (MAJOR.MINOR.PATCH)
- In-app update notifications

### Telemetry (Optional & Opt-In)
- **Crash reports:** Anonymous crash dumps
- **Usage statistics:** Feature usage frequency (anonymous)
- **Performance metrics:** Startup time, operation durations
- **Fully transparent:** User consent required, can be disabled

---

## Future Enhancements

### Potential Features (Post-Launch)
1. **MonoGame Marketplace Integration**
   - Browse and install community assets
   - Integrate with NuGet packages
   - Asset previews

2. **Project Templates**
   - User-defined project templates
   - Template sharing with community
   - Template marketplace

3. **Integrated Package Manager**
   - Browse and install NuGet packages
   - Visual package dependency viewer
   - Version conflict resolution

4. **Build Configuration Manager**
   - Multi-platform build profiles
   - CI/CD integration
   - Automated build scripts

5. **Performance Profiler Integration**
   - Built-in performance monitoring
   - Memory profiler
   - Frame time analysis

6. **Extension System**
   - Plugin architecture
   - Community-developed extensions
   - Extension marketplace

7. **Cloud Project Sync**
   - Optional cloud backup
   - Project sharing with team members
   - Version control integration (Git, GitHub)

8. **Learning Center**
   - Interactive tutorials
   - Code samples
   - Video integration

9. **Community Features**
   - Forum integration
   - Discord integration
   - Show & tell gallery

10. **MonoGame Store**
    - Purchase premium assets
    - Support content creators
    - Revenue sharing with MonoGame Foundation

---

## Branding & Visual Identity

### Logo & App Icon
- Modern, minimalist design
- Incorporate MonoGame brand elements
- Scalable vector format (SVG)
- Multiple sizes for different platforms
- Distinct from Unity Hub to avoid confusion

### Splash Screen
- MonoGame Hub logo
- Version number
- Loading indicator
- MonoGame orange accent

### Window Styling
- Custom title bar with MonoGame branding
- Frameless window with rounded corners (platform permitting)
- Smooth animations and transitions
- Consistent with MonoGame brand guidelines

---

## Technical Constraints & Requirements

### Minimum System Requirements

**Windows:**
- OS: Windows 10 version 1809 or higher
- CPU: 1 GHz or faster
- RAM: 2 GB
- Disk: 200 MB free space
- .NET: .NET 10 SDK or higher

**macOS:**
- OS: macOS 12 Monterey or higher
- CPU: Intel or Apple Silicon
- RAM: 2 GB
- Disk: 200 MB free space
- .NET: .NET 10 SDK or higher

**Linux:**
- OS: Ubuntu 20.04, Fedora 36, or equivalent
- CPU: 1 GHz or faster
- RAM: 2 GB
- Disk: 200 MB free space
- .NET: .NET 10 SDK or higher
- Display: X11 or Wayland

### Dependencies
- .NET 10.0 SDK
- Avalonia UI 11.x
- SQLite 3.x
- System.Net.Http for API calls
- Markdown.Avalonia for content rendering

---

## Risk Analysis & Mitigation

### Technical Risks

**Risk:** Cross-platform compatibility issues
- **Likelihood:** Medium
- **Impact:** High
- **Mitigation:** Extensive testing on all platforms, use platform abstractions

**Risk:** NuGet API rate limiting or changes
- **Likelihood:** Low
- **Impact:** Medium
- **Mitigation:** Implement caching, retry logic, graceful degradation

**Risk:** MonoGame website structure changes breaking scraping
- **Likelihood:** Medium
- **Impact:** Low
- **Mitigation:** Use RSS feeds where possible, implement robust parsing with error handling

**Risk:** Performance issues with large project counts
- **Likelihood:** Medium
- **Impact:** Medium
- **Mitigation:** Implement pagination, lazy loading, efficient indexing

### User Experience Risks

**Risk:** Complex UI overwhelming new users
- **Likelihood:** Medium
- **Impact:** Medium
- **Mitigation:** Onboarding wizard, tooltips, contextual help

**Risk:** Inconsistent IDE detection across platforms
- **Likelihood:** High
- **Impact:** Low
- **Mitigation:** Provide manual IDE path configuration fallback

---

## Success Metrics

### Adoption Metrics
- **Downloads:** 10,000+ in first 6 months
- **Active Users:** 5,000+ monthly active users
- **Retention:** 60%+ 30-day retention

### Engagement Metrics
- **Projects Created:** Average 3+ projects per user
- **Templates Installed:** Average 2+ template versions per user
- **Session Duration:** Average 15+ minutes per session

### Quality Metrics
- **Crash Rate:** < 0.1% of sessions
- **Bug Reports:** < 10 critical bugs per release
- **User Satisfaction:** 4+ stars average rating

### Community Metrics
- **GitHub Stars:** 500+ stars
- **Contributors:** 20+ contributors
- **Community Discussions:** Active discussion forum

---

## Conclusion

The MonoGame Hub represents a significant leap forward in the MonoGame developer experience. By providing a unified, cross-platform application for project management, template installation, and community engagement, we aim to lower the barrier to entry for new developers while enhancing productivity for experienced users.

This PRD establishes a solid foundation for development, with clear technical architecture, detailed screen specifications, and phased implementation plan. The focus on modern UI/UX, inspired by Unity Hub but with unique MonoGame branding, ensures the Hub will be a showcase feature for the MonoGame ecosystem.

**Key Differentiators:**
- True cross-platform support (Windows, macOS, Linux)
- Modern .NET architecture with Avalonia UI
- Comprehensive project management features
- Integrated community features (blog, resources)
- Focus on developer productivity and ease of use

**Next Steps:**
1. Review and approve PRD
2. Begin Phase 1 implementation
3. Set up CI/CD pipeline
4. Establish beta testing program
5. Launch community feedback channels

---

**Document Version:** 1.0  
**Last Updated:** December 19, 2024  
**Status:** Draft for Review  
**Author:** GitHub Copilot Agent  
**Approvers:** MonoGame Hub Project Stakeholders
