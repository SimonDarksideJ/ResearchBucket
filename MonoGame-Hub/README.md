# MonoGame Hub (prototype)

This folder contains a cross-platform MonoGame Hub prototype built with **Avalonia UI** and targeting **.NET 10**.

## Build

From `MonoGame-Hub/`:

- `dotnet build MonoGameHub.sln`

## Run

- `dotnet run --project src/MonoGameHub.App/MonoGameHub.App.csproj`

## Splashscreen, announcements, and startup options

### Splashscreen

- On startup, the app shows a short splash screen with a live “loading” status line.
- Minimum splash time is controlled by `MinSplashStartTime` in [src/MonoGameHub.App/App.axaml.cs](src/MonoGameHub.App/App.axaml.cs).
- The number of visible splash log lines is controlled by `SplashLogVisibleLines` in [src/MonoGameHub.App/ViewModels/SplashViewModel.cs](src/MonoGameHub.App/ViewModels/SplashViewModel.cs).

### Skip splashscreen (setting)

- In the Settings tab there is a checkbox: “Skip splashscreen”.
- When enabled, the app will open directly to the main window on next startup.

### New announcements (blog posts)

- The Hub caches the blog post count from the previous run (`LastSeenBlogPostCount` in the app settings file).
- During startup, it queries MonoGame blog posts; if the count changed since the last run, it shows a “New announcement” toast in the main application window (bottom-right):
  - Visible for ~15 seconds, then fades away (see `StartupOptions.InAppAnnouncementToastDuration`).
  - Includes an “X” close button to dismiss immediately.

### Debug/testing option: ClearCache

To reset announcement state for testing:

- `dotnet run --project src/MonoGameHub.App/MonoGameHub.App.csproj -- ClearCache`
- `dotnet run --project src/MonoGameHub.App/MonoGameHub.App.csproj -- --clearCache`

This resets the cached blog-post count so the next startup check can treat announcements as “new” again.

## Development

### Force clearing announcement cache

For local development/testing, you can force the app to clear the announcement cache on every startup (even without passing `ClearCache`).

- Constant: `StartupOptions.AlwaysClearCacheInDev`
- Location: [src/MonoGameHub.App/Services/StartupOptions.cs](src/MonoGameHub.App/Services/StartupOptions.cs)
- Default: `false`

When enabled, it behaves like passing `ClearCache` every time (resets `LastSeenBlogPostCount` to the sentinel value used to force a “new” toast).

## Current capabilities (early prototype)

- UI shell with tabs: Projects, Installs, New Project, Settings, Blog, Resources, Docs/API.
- Project scanning (basic heuristics): detects MonoGame package refs, DesktopGL/WindowsDX, legacy MGCB signals, and version spec strings.
- Dotnet orchestration utilities for `dotnet` commands (used progressively as features are wired up).

### Docs/API (in-app browser)

- Split view: left TOC + filter, right rendered page.
- Two modes:
  - **Docs**: `https://docs.monogame.net/articles/`
  - **API**: `https://docs.monogame.net/api/`
- Offline-first caching:
  - Caches TOCs for both modes and caches rendered pages on disk.
  - When online, it refreshes content; when offline, it falls back to cached content.
- Link behavior:
  - Links to `docs.monogame.net` stay in-app (navigate within the Docs/API pane).
  - External links open in the system browser.

### Folder picking UX

- Folder path fields (Projects, Settings, New Project) include a standard "..." button that opens the OS folder picker.

## Notes

- Template package IDs and detection heuristics evolve; they’re centralized in `MonoGameHub.Core` to keep iteration fast.

## Future

- Analytics
- Automation
