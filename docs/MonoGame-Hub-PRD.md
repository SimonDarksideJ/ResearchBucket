# MonoGame Hub — Product Requirements Document (PRD)

Date: 2025-12-19  
Status: Draft v0.1  
Owner: MonoGame Community (proposal)

## 1) Vision
MonoGame Hub is a cross-platform desktop “project hub” for MonoGame—conceptually similar to Unity Hub—focused on:
- Discovering and organizing MonoGame projects
- Ensuring the correct MonoGame templates/tools are installed
- Creating new projects from templates
- Making projects “ready to run” immediately
- Surfacing official MonoGame news and resources inside the hub

The tone should be modern and friendly, with a MonoGame identity (MonoGame orange as an accent), but not a direct visual copy of Unity Hub.

## 2) Goals
- **Cross-platform** desktop client: Windows, macOS, Linux.
- **Project discovery**: list MonoGame projects from a configurable folder; show metadata (platforms/templates used, content pipeline approach, MonoGame version).
- **Version intelligence**:
  - Detect whether a project references a fixed MonoGame version or wildcard (e.g., `3.8.5-develop.*`).
  - When wildcard is used, resolve to the latest matching version from the “dotnet registry” (NuGet sources).
  - Sync and display available MonoGame versions across stable + preview + develop builds.
- **Installs management**: view installed MonoGame `dotnet new` templates and install/uninstall versions reliably.
- **New project workflow**: create a project by selecting a template + project name; run `dotnet new` to a folder; then run restore steps.
- **Ready-to-open**: after creation, run `dotnet restore` and (when required) `dotnet tool restore`.
- **Settings**:
  - Default projects folder
  - Default NuGet cache folder (updates the environment variable controlling global packages location)
  - Show currently running .NET SDK version
  - Provide OS-specific links to install .NET 9 or .NET 10
  - Preferred IDE (Visual Studio, VS Code, Rider) and open project after creation
- **Content integration**:
  - Blog: list and display posts from https://monogame.net/blog/ with tag filtering and search
  - Resources: list items from https://monogame.net/resources/ with tag filtering and search

## 3) Non-Goals (initial release)
- Account sign-in, cloud sync, collaboration.
- Full project editing (Hub is not an IDE).
- MonoGame runtime installation management (templates/tools only).
- Deep build diagnostics across all target platforms.
- A full “package manager UI” for all NuGet dependencies.

## 4) Target Users
- **New users**: want an easy “Create → Run” path.
- **Hobbyists/indies**: manage multiple experiments and switch template versions.
- **Maintainers**: quickly see which projects depend on which MonoGame/tooling versions.

## 5) Primary User Flows
### 5.1 First run
1. Hub opens to **Projects** tab.
2. If no project root configured: prompt inside page (not a modal requirement) to choose a default projects folder.
3. Hub scans folder and populates project list.

### 5.2 Discover projects
1. User selects a root folder.
2. Hub enumerates projects underneath.
3. Hub displays a card/list entry per project with:
   - Project name
   - Path
   - Detected platforms/templates (DesktopGL/Windows, etc.)
   - Content pipeline approach (legacy MGCB vs new pipeline)
   - Detected MonoGame version + whether wildcard
   - Status badges (e.g., “Needs restore”, “Missing templates”)

### 5.3 Sync versions
1. User visits **Installs** tab.
2. Hub queries configured NuGet sources for MonoGame template package versions.
3. Hub caches results locally with a refresh button.

### 5.4 Install templates
1. User selects a MonoGame version.
2. Hub uninstalls existing MonoGame template versions (as required).
3. Hub installs the selected version.
4. Hub indicates whether the version is **stable** vs **pre-release**.

### 5.5 Create project
1. User visits **New Project**.
2. Enters project name.
3. Selects template (and version if required).
4. Hub runs `dotnet new` to `DefaultProjectsFolder/ProjectName` (or chosen folder).
5. Hub runs:
   - `dotnet restore`
   - `dotnet tool restore` when the generated project uses older MGCB editor templates.
6. Hub offers “Open in IDE” using preference.

## 6) Screens / Information Architecture
### 6.1 Projects
- Folder selector (default project root)
- Project list/grid
- Search/filter within discovered projects (name/path/tag-like platform badges)
- On selection: a details pane/section showing:
  - Detected project type(s)
  - MonoGame version info
  - Content pipeline mode
  - Actions: Open folder, Open in IDE, Run restore, Reveal in file explorer

### 6.2 Installs
- Local state:
  - Which MonoGame templates are installed
  - Which .NET SDK is in use
- Remote state:
  - List of versions available (stable + preview + develop)
- Actions:
  - Sync/refresh available versions
  - Install selected template version
  - Uninstall existing templates (automated as part of install)
- Visual requirements:
  - **Stable releases**: black background row/card
  - **Pre-release (preview/develop)**: highlighted **MonoGame orange** row/card

### 6.3 New Project
- Inputs:
  - Project name
  - Template selection
  - Target location (default to configured projects folder)
- Execution view:
  - Shows step-by-step progress output for `dotnet new`, `dotnet restore`, `dotnet tool restore` (when applicable)
- Completion:
  - Buttons: Open in IDE, Open folder

### 6.4 Settings
- Default projects folder
- NuGet cache folder
  - Sets global packages folder (see “Technical Requirements”)
- Current .NET SDK version (read-only)
- Links: Install .NET 9 / .NET 10 (OS-specific)
- Preferred IDE:
  - Visual Studio (Windows/macOS where applicable)
  - VS Code
  - Rider

### 6.5 Blog
- List of posts with title/date/excerpt
- Tag filter and text search
- Post reader pane embedded in the app

### 6.6 Resources
- List of resource items
- Tag filter and text search
- Resource details pane (open external link)

## 7) Look & Feel Requirements
- Modern, clean, “hub-like” dashboard aesthetic.
- MonoGame orange as a key accent color and for pre-release highlighting.
- Avoid copying Unity Hub’s layout pixel-for-pixel; use similar *concepts* (left navigation, list/detail layouts) while giving MonoGame its own identity.
- Accessibility:
  - Keyboard navigable
  - Reasonable contrast
  - Scalable typography

## 8) Functional Requirements
### 8.1 Project discovery & scanning
- Select a root folder.
- Recursively scan for MonoGame projects.

**Project identification heuristics** (initial version):
- A “MonoGame project” is any solution or project file (`*.sln`, `*.csproj`) that:
  - References MonoGame packages (e.g., `MonoGame.Framework.*`) OR
  - Contains MonoGame template markers typical of official templates.

**Platform/templates detection** (example signals):
- DesktopGL: `MonoGame.Framework.DesktopGL` reference
- WindowsDX: `MonoGame.Framework.WindowsDX` reference
- Content pipeline projects:
  - New pipeline: `MonoGame.Pipeline` package reference (or known pipeline tool pattern)
  - Legacy MGCB MSBuild approach: MSBuild task reference (commonly `MonoGame.Content.Builder.Task`) and content reference items (commonly `MonoGame.Content.Reference`)

Note: exact item names can vary by template vintage; implementation should support multiple patterns and remain extendable.

### 8.2 MonoGame version detection
- Extract MonoGame package versions from `PackageReference` and/or `packages.lock.json` (if present).
- If version is a wildcard (e.g., `3.8.5-develop.*` or `3.8.*`):
  - Resolve to the latest matching version from configured NuGet sources.

### 8.3 Available versions sync
- Query NuGet sources (the “dotnet registry”) for:
  - MonoGame template package(s)
  - MonoGame framework package(s)
- Classify versions:
  - Stable: no prerelease label
  - Pre-release: contains prerelease label (e.g., `-preview`, `-rc`, `-develop`)
- Cache results locally with timestamp.

### 8.4 Template installs management
- List installed `dotnet new` templates related to MonoGame.
- Install flow:
  1. Uninstall existing MonoGame templates (all versions)
  2. Install the requested version
  3. Verify install by listing templates

### 8.5 New project creation
- Run `dotnet new <templateShortName> -n <ProjectName> -o <OutputFolder>`
- Run `dotnet restore`
- Conditionally run `dotnet tool restore` (legacy MGCB editor templates)

### 8.6 Settings
- Default projects folder stored in app settings.
- NuGet cache folder:
  - Configure global packages folder via environment variable.
  - See “Technical Requirements” for exact mechanism.
- IDE preference:
  - After project creation, open in preferred IDE.

### 8.7 Blog and Resources integration
- Blog: ingest and render posts from https://monogame.net/blog/
- Resources: ingest and render items from https://monogame.net/resources/
- Provide:
  - tag filter
  - search

Implementation note: if the sites provide RSS/JSON, prefer that; otherwise use safe HTML parsing and cache results.

## 9) Technical Requirements
### 9.1 Cross-platform constraints
- Must run on Windows, macOS, Linux.
- Must integrate cleanly with .NET tooling (`dotnet` CLI).
- Must support modern .NET (target .NET 10 preferred; .NET 9 acceptable fallback).

### 9.2 Local execution model
The Hub runs external commands and captures output:
- `dotnet --info`
- `dotnet new list` (and/or `dotnet new --list` depending on SDK)
- `dotnet new install <pkg>::<version>` or `dotnet new uninstall <pkg>` (exact subcommands depend on .NET SDK behavior)
- `dotnet restore`
- `dotnet tool restore`

Commands should be executed with:
- Clear streaming output in UI
- Cancellation support
- Robust error handling and actionable messages

### 9.3 NuGet cache folder setting
- Support setting the NuGet global packages folder via environment variable (commonly `NUGET_PACKAGES`).
- Apply changes in a way that affects Hub-invoked dotnet operations. (System-wide persistence may be OS-specific; at minimum apply to Hub process and child processes.)

### 9.4 “Wildcard” version resolution
When a project uses a wildcard version:
- Identify the wildcard “prefix”, e.g.:
  - `3.8.*` → prefix `3.8.`
  - `3.8.5-develop.*` → prefix `3.8.5-develop.`
- Query NuGet versions for the package.
- Filter to versions matching the prefix.
- Choose the greatest semantic version.
- For prerelease wildcards (like `-develop.*`), include prerelease versions.

### 9.5 Project scanning performance
- Scanning should be incremental and cancellable.
- Cache per-project scan results and only rescan when file timestamps change.

### 9.6 Security and privacy
- No telemetry in initial release unless explicitly designed and opt-in.
- Only read files within user-selected folders.
- Do not execute arbitrary scripts found in project folders.

## 10) Data Model (logical)
- **AppSettings**: ProjectsRoot, NuGetPackagesFolder, PreferredIDE, LastVersionSyncAt
- **ProjectRecord**: Name, Path, DetectedPlatforms, UsesLegacyMGCB, UsesNewPipeline, MonoGameVersionSpec, ResolvedMonoGameVersion, NeedsRestore, LastScannedAt
- **TemplateVersionRecord**: Version, Channel (Stable/Preview/Develop), Installed (bool)
- **FeedItem**: Title, Date, Tags, Url, Excerpt, ContentHtml/Markdown

## 11) Error & Edge Cases
- Multiple MonoGame references across projects in a solution.
- Multi-targeted projects.
- Offline behavior:
  - Show cached version lists
  - Mark feed content stale
- Custom NuGet sources.
- Missing `dotnet` or incompatible SDK.
- Projects that include MonoGame indirectly.

## 12) Release Milestones (suggested)
- **M0 (Prototype)**: UI shell + Projects scan + basic detection
- **M1**: Template install/uninstall + New project workflow
- **M2**: Settings + IDE open
- **M3**: Blog + Resources integration
- **M4**: Polish + accessibility + packaging

---

## Appendix A: Initial detection rules (to be refined)
Legacy MGCB (indicative):
- MSBuild task package reference to `MonoGame.Content.Builder.Task` (or similar)
- Content reference item/targets commonly associated with `MonoGame.Content.Reference`

New pipeline (indicative):
- Pipeline tool/package reference patterns, e.g. `MonoGame.Pipeline`

Platform markers (indicative):
- `MonoGame.Framework.DesktopGL`
- `MonoGame.Framework.WindowsDX`
- Others as templates evolve
